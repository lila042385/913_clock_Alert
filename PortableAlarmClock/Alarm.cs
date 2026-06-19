// v1.02 20260620 00:52 INotifyPropertyChanged implementation for UI binding update
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PortableAlarmClock
{
    public class Alarm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Guid _id = Guid.NewGuid();
        private int _hour;
        private int _minute;
        private string _title = string.Empty;
        private List<DayOfWeek> _weekdays = new List<DayOfWeek>();
        private bool _isEnabled = true;
        private string _soundName = "Default";
        private int _snoozeMinutes = 10;
        private int _sortOrder;
        private DateTime? _lastTriggered;
        private DateTime _createdAt = DateTime.Now;
        private DateTime _updatedAt = DateTime.Now;

        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int Hour
        {
            get => _hour;
            set
            {
                if (_hour != value)
                {
                    _hour = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TimeString));
                }
            }
        }

        public int Minute
        {
            get => _minute;
            set
            {
                if (_minute != value)
                {
                    _minute = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TimeString));
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        // Days of week for repeating. If empty, it's a one-time alarm.
        // C# DayOfWeek: 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        public List<DayOfWeek> Weekdays
        {
            get => _weekdays;
            set
            {
                _weekdays = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WeekdaysString));
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SoundName
        {
            get => _soundName;
            set { _soundName = value; OnPropertyChanged(); }
        }

        public int SnoozeMinutes
        {
            get => _snoozeMinutes;
            set { _snoozeMinutes = value; OnPropertyChanged(); }
        }

        public int SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(); }
        }

        public DateTime? LastTriggered
        {
            get => _lastTriggered;
            set { _lastTriggered = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public DateTime? SnoozeUntil { get; set; }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(); }
        }

        // Visual helper for weekdays description (e.g. "Mon, Tue, Wed" or "Once")
        // Since we need to follow Japanese style as requested
        public string WeekdaysString
        {
            get
            {
                if (Weekdays == null || Weekdays.Count == 0)
                {
                    return "1回限り";
                }
                if (Weekdays.Count == 7)
                {
                    return "毎日";
                }

                // Sort by Mon..Sun (standard in Windows Alarm)
                var sorted = new List<DayOfWeek>(Weekdays);
                sorted.Sort((a, b) =>
                {
                    int valA = a == DayOfWeek.Sunday ? 7 : (int)a;
                    int valB = b == DayOfWeek.Sunday ? 7 : (int)b;
                    return valA.CompareTo(valB);
                });

                var names = new List<string>();
                foreach (var day in sorted)
                {
                    names.Add(GetDayShortName(day));
                }
                return string.Join("、", names);
            }
        }

        public string TimeString => $"{Hour:D2}:{Minute:D2}";

        private static string GetDayShortName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Sunday => "日",
                DayOfWeek.Monday => "月",
                DayOfWeek.Tuesday => "火",
                DayOfWeek.Wednesday => "水",
                DayOfWeek.Thursday => "木",
                DayOfWeek.Friday => "金",
                DayOfWeek.Saturday => "土",
                _ => string.Empty
            };
        }
    }
}
// v1.02 20260620 00:52 INotifyPropertyChanged implementation for UI binding update
// v1.01 20260617 23:55
