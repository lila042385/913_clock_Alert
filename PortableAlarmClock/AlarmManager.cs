// v1.03 20260620 09:46
// 履歴: アラーム編集時にUIが即時更新されない不具合を、インスタンス差し替え方式に変更することで修正
// v1.02 20260617 23:59
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PortableAlarmClock
{
    public class AlarmManager
    {
        private static readonly object _lock = new object();
        private readonly string _dataDir;
        private readonly string _jsonPath;
        private readonly string _backupPath;

        public ObservableCollection<Alarm> Alarms { get; private set; } = new ObservableCollection<Alarm>();
        
        // Flags to indicate loading issues to the UI
        public bool LoadedWithError { get; private set; }
        public string LastErrorMessage { get; private set; } = string.Empty;

        // Alarm Trigger event
        public event Action<Alarm>? AlarmTriggered;

        // Keep track of the last time we checked the alarms for scheduling & sleep resume detection
        private DateTime _lastCheckTime = DateTime.Now;

        public AlarmManager(string baseDir)
        {
            _dataDir = Path.Combine(baseDir, "AlarmClockData");
            _jsonPath = Path.Combine(_dataDir, "alarms.json");
            _backupPath = Path.Combine(_dataDir, "alarms_broken_backup.json");
            _lastCheckTime = DateTime.Now;
        }

        public void LoadAlarms()
        {
            lock (_lock)
            {
                LoadedWithError = false;
                LastErrorMessage = string.Empty;
                Alarms.Clear();

                if (!File.Exists(_jsonPath))
                {
                    Logger.Info("No alarms.json found. Initializing with empty list.");
                    return;
                }

                try
                {
                    string json = File.ReadAllText(_jsonPath, Encoding.UTF8);
                    var list = JsonSerializer.Deserialize<List<Alarm>>(json);
                    if (list != null)
                    {
                        // Sort by SortOrder
                        var sorted = list.OrderBy(a => a.SortOrder).ToList();
                        foreach (var alarm in sorted)
                        {
                            Alarms.Add(alarm);
                        }
                        Logger.Info($"Loaded {Alarms.Count} alarms successfully.");
                    }
                }
                catch (Exception ex)
                {
                    LoadedWithError = true;
                    LastErrorMessage = ex.Message;
                    Logger.Error("Failed to load alarms.json. Creating backup and starting empty.", ex);

                    // Backup the broken file
                    try
                    {
                        if (File.Exists(_jsonPath))
                        {
                            if (File.Exists(_backupPath))
                            {
                                File.Delete(_backupPath);
                            }
                            File.Move(_jsonPath, _backupPath);
                            Logger.Info("Backed up broken alarms.json successfully.");
                        }
                    }
                    catch (Exception backupEx)
                    {
                        Logger.Error("Failed to backup broken alarms.json", backupEx);
                    }
                }
            }
        }

        public void SaveAlarms()
        {
            lock (_lock)
            {
                try
                {
                    if (!Directory.Exists(_dataDir))
                    {
                        Directory.CreateDirectory(_dataDir);
                    }

                    // Assign SortOrder based on current position in collection
                    for (int i = 0; i < Alarms.Count; i++)
                    {
                        Alarms[i].SortOrder = i;
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(Alarms.ToList(), options);

                    // Secure saving: Write to temp file first, then replace
                    string tempPath = Path.Combine(_dataDir, "alarms.tmp");
                    File.WriteAllText(tempPath, json, Encoding.UTF8);

                    if (File.Exists(_jsonPath))
                    {
                        File.Replace(tempPath, _jsonPath, null);
                    }
                    else
                    {
                        File.Move(tempPath, _jsonPath);
                    }

                    Logger.Info("Alarms saved successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to save alarms.", ex);
                    throw; // Re-throw to handle in UI or App.xaml.cs if needed
                }
            }
        }

        public void AddAlarm(Alarm alarm)
        {
            lock (_lock)
            {
                alarm.SortOrder = Alarms.Count > 0 ? Alarms.Max(a => a.SortOrder) + 1 : 0;
                Alarms.Add(alarm);
                SaveAlarms();
                Logger.Info($"Added new alarm: {alarm.Id} ({alarm.TimeString})");
            }
        }

        public void RemoveAlarm(Alarm alarm)
        {
            lock (_lock)
            {
                if (Alarms.Remove(alarm))
                {
                    SaveAlarms();
                    Logger.Info($"Removed alarm: {alarm.Id}");
                }
            }
        }

        public void UpdateAlarm(Alarm alarm)
        {
            lock (_lock)
            {
                var existing = Alarms.FirstOrDefault(a => a.Id == alarm.Id);
                if (existing != null)
                {
                    int index = Alarms.IndexOf(existing);
                    if (index >= 0)
                    {
                        Alarms[index] = alarm;
                    }
                    SaveAlarms();
                    Logger.Info($"Updated alarm: {alarm.Id}");
                }
            }
        }

        public void ToggleAlarm(Guid id, bool isEnabled)
        {
            lock (_lock)
            {
                var alarm = Alarms.FirstOrDefault(a => a.Id == id);
                if (alarm != null)
                {
                    alarm.IsEnabled = isEnabled;
                    alarm.UpdatedAt = DateTime.Now;
                    
                    // Reset LastTriggered so it can fire at the next scheduled time
                    if (isEnabled)
                    {
                        alarm.LastTriggered = null;
                    }
                    
                    SaveAlarms();
                    Logger.Info($"Toggled alarm: {id} to {isEnabled}");
                }
            }
        }

        public void CheckAlarms()
        {
            DateTime now = DateTime.Now;
            
            // Check if there is a gap (which typically means sleep resume or time changed)
            // If the gap is larger than 10 seconds, we treat it as a special jump
            var gap = now - _lastCheckTime;
            if (gap.TotalSeconds > 10)
            {
                Logger.Info($"Time gap detected: {gap.TotalSeconds:F1} seconds. Performing scheduler recovery.");
                HandleTimeGap(_lastCheckTime, now);
                _lastCheckTime = now;
                return;
            }

            lock (_lock)
            {
                foreach (var alarm in Alarms)
                {
                    if (!alarm.IsEnabled) continue;

                    // 1. Check Snooze
                    if (alarm.SnoozeUntil.HasValue)
                    {
                        if (now >= alarm.SnoozeUntil.Value)
                        {
                            Logger.Info($"Snooze alarm triggered for {alarm.Id} ({alarm.TimeString})");
                            alarm.SnoozeUntil = null; // Reset snooze
                            TriggerAlarm(alarm);
                        }
                        continue;
                    }

                    // 2. Normal check
                    if (alarm.Hour == now.Hour && alarm.Minute == now.Minute)
                    {
                        // Check if we should trigger today
                        bool shouldTrigger = false;
                        if (alarm.Weekdays == null || alarm.Weekdays.Count == 0)
                        {
                            // One-time alarm
                            shouldTrigger = true;
                        }
                        else if (alarm.Weekdays.Contains(now.DayOfWeek))
                        {
                            // Repeating alarm on today's day of week
                            shouldTrigger = true;
                        }

                        if (shouldTrigger)
                        {
                            // Avoid double trigger in the same minute
                            bool alreadyTriggered = alarm.LastTriggered.HasValue && 
                                                   alarm.LastTriggered.Value.Date == now.Date &&
                                                   alarm.LastTriggered.Value.Hour == now.Hour &&
                                                   alarm.LastTriggered.Value.Minute == now.Minute;

                            if (!alreadyTriggered)
                            {
                                Logger.Info($"Alarm scheduled time reached for {alarm.Id} ({alarm.TimeString})");
                                alarm.LastTriggered = now;
                                TriggerAlarm(alarm);
                            }
                        }
                    }
                }
            }

            _lastCheckTime = now;
        }

        private void TriggerAlarm(Alarm alarm)
        {
            AlarmTriggered?.Invoke(alarm);
        }

        public void HandleTimeGap(DateTime lastCheck, DateTime now)
        {
            lock (_lock)
            {
                foreach (var alarm in Alarms)
                {
                    if (!alarm.IsEnabled) continue;
                    if (alarm.SnoozeUntil.HasValue) continue;

                    // Check all minutes between lastCheck and now
                    DateTime check = lastCheck.AddMinutes(1);
                    check = new DateTime(check.Year, check.Month, check.Day, check.Hour, check.Minute, 0);
                    DateTime limit = now;

                    while (check <= limit)
                    {
                        if (alarm.Hour == check.Hour && alarm.Minute == check.Minute)
                        {
                            bool shouldTrigger = false;
                            if (alarm.Weekdays == null || alarm.Weekdays.Count == 0)
                            {
                                shouldTrigger = true;
                            }
                            else if (alarm.Weekdays.Contains(check.DayOfWeek))
                            {
                                shouldTrigger = true;
                            }

                            if (shouldTrigger)
                            {
                                bool alreadyTriggered = alarm.LastTriggered.HasValue && 
                                                       alarm.LastTriggered.Value.Date == check.Date &&
                                                       alarm.LastTriggered.Value.Hour == check.Hour &&
                                                       alarm.LastTriggered.Value.Minute == check.Minute;

                                if (!alreadyTriggered)
                                {
                                    double diffMinutes = (now - check).TotalMinutes;
                                    if (diffMinutes <= 10.0)
                                    {
                                        Logger.Info($"Delay-triggering alarm {alarm.Id} ({alarm.TimeString}) from missed time {check:yyyy-MM-dd HH:mm:ss} (delay: {diffMinutes:F1} mins)");
                                        alarm.LastTriggered = now;
                                        TriggerAlarm(alarm);
                                    }
                                    else
                                    {
                                        Logger.Info($"Skipping missed alarm {alarm.Id} ({alarm.TimeString}) from {check:yyyy-MM-dd HH:mm:ss} (exceeded 10 min threshold: {diffMinutes:F1} mins)");
                                        alarm.LastTriggered = check;
                                    }
                                }
                            }
                        }
                        check = check.AddMinutes(1);
                    }
                }
            }
        }
    }
}
// v1.03 20260620 09:46
