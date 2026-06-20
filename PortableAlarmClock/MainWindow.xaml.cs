// v1.06 20260620 10:20
// 履歴: 左メニューの設定タブへの遷移処理（SettingsViewのインスタンス生成・割当）を追加
// v1.05 20260620 00:52 - Custom titlebar handlers + balloon notification
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PortableAlarmClock
{
    public partial class MainWindow : Window
    {
        private readonly AlarmManager _alarmManager;
        private readonly DispatcherTimer _schedulerTimer;
        private SystemTrayIcon? _trayIcon;
        private bool _isExplicitClose;

        // Tab views (lazily created)
        private AlarmView? _alarmView;
        private TimerView? _timerView;
        private StopwatchView? _stopwatchView;
        private SettingsView? _settingsView;

        public MainWindow()
        {
            InitializeComponent();

            // Resolve global AlarmManager
            _alarmManager = App.AlarmManagerInstance ?? new AlarmManager(AppDomain.CurrentDomain.BaseDirectory);

            // Subscribe to alarm triggers
            _alarmManager.AlarmTriggered += AlarmManager_AlarmTriggered;

            // Start 1-second check scheduler
            _schedulerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _schedulerTimer.Tick += SchedulerTimer_Tick;
            _schedulerTimer.Start();

            // Initialize tray icon
            _trayIcon = new SystemTrayIcon(this);
            _trayIcon.DoubleClicked += TrayIcon_DoubleClicked;
            _trayIcon.OpenMenuSelected += TrayIcon_DoubleClicked;
            _trayIcon.ExitMenuSelected += TrayIcon_ExitMenuSelected;
            _trayIcon.Create();

            // Subscribe to PowerModeChanged for sleep resume
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Navigate to initial view (Alarm)
            NavigateTo("Alarm");

            // Notify user of loading issues if any
            if (_alarmManager.LoadedWithError)
            {
                MessageBox.Show(
                    $"\u8a2d\u5b9a\u30d5\u30a1\u30a4\u30eb\u306e\u8aad\u307f\u8fbc\u307f\u4e2d\u306b\u30a8\u30e9\u30fc\u304c\u767a\u751f\u3057\u305f\u305f\u3081\u3001\u521d\u671f\u72b6\u614b\u3067\u8d77\u52d5\u3057\u307e\u3057\u305f\u3002{Environment.NewLine}\u30a8\u30e9\u30fc: {_alarmManager.LastErrorMessage}{Environment.NewLine}(\u7834\u640d\u30d5\u30a1\u30a4\u30eb\u306f\u30d0\u30c3\u30af\u30a2\u30c3\u30d7\u3055\u308c\u307e\u3057\u305f)",
                    "\u8a2d\u5b9a\u8aad\u8fbc\u30a8\u30e9\u30fc", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #region Custom Titlebar Handlers

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TrayButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            Logger.Info("Window hidden via tray button (minimized to system tray).");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "\u30a2\u30d7\u30ea\u3092\u7d42\u4e86\u3059\u308b\u3068\u3001\u8a2d\u5b9a\u3057\u305f\u30a2\u30e9\u30fc\u30e0\u306f\u9cf4\u308a\u307e\u305b\u3093\u3002\u7d42\u4e86\u3057\u307e\u3059\u304b\uff1f",
                "\u30dd\u30fc\u30bf\u30d6\u30eb \u30a2\u30e9\u30fc\u30e0 \u30af\u30ed\u30c3\u30af",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _isExplicitClose = true;
                this.Close();
            }
        }

        #endregion

        #region Navigation

        private void NavButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string viewName)
        {
            switch (viewName)
            {
                case "Alarm":
                    _alarmView ??= new AlarmView();
                    ContentArea.Content = _alarmView;
                    break;
                case "Timer":
                    _timerView ??= new TimerView();
                    ContentArea.Content = _timerView;
                    break;
                case "Stopwatch":
                    _stopwatchView ??= new StopwatchView();
                    ContentArea.Content = _stopwatchView;
                    break;
                case "Settings":
                    _settingsView ??= new SettingsView();
                    ContentArea.Content = _settingsView;
                    break;
            }
        }

        #endregion

        #region Alarm Scheduler & Trigger

        private void SchedulerTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _alarmManager.CheckAlarms();
            }
            catch (Exception ex)
            {
                Logger.Error("Exception in scheduler timer tick.", ex);
            }

            // Safety: ensure timer keeps running
            if (!_schedulerTimer.IsEnabled)
            {
                Logger.Info("Scheduler timer was stopped unexpectedly. Restarting.");
                _schedulerTimer.Start();
            }
        }

        private void AlarmManager_AlarmTriggered(Alarm alarm)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Logger.Info($"Opening AlarmAlertWindow for: {alarm.Id}");

                    // Show Windows balloon notification
                    string notifTitle = string.IsNullOrWhiteSpace(alarm.Title) ? "\u30a2\u30e9\u30fc\u30e0" : alarm.Title;
                    _trayIcon?.ShowBalloonNotification(notifTitle, $"{alarm.TimeString} - \u30a2\u30e9\u30fc\u30e0\u6642\u523b\u3067\u3059");

                    var alertWindow = new AlarmAlertWindow(alarm)
                    {
                        Owner = this
                    };

                    alertWindow.Show();
                    alertWindow.Closed += (s, e) =>
                    {
                        if (alertWindow.IsSnoozed)
                        {
                            alarm.SnoozeUntil = DateTime.Now.AddMinutes(alarm.SnoozeMinutes);
                            Logger.Info($"Alarm {alarm.Id} snoozed until {alarm.SnoozeUntil}");
                        }
                        else if (alertWindow.IsDismissed)
                        {
                            alarm.SnoozeUntil = null;
                            if (alarm.Weekdays == null || alarm.Weekdays.Count == 0)
                            {
                                _alarmManager.ToggleAlarm(alarm.Id, false);
                            }
                            else
                            {
                                _alarmManager.SaveAlarms();
                            }
                            Logger.Info($"Alarm {alarm.Id} dismissed.");
                        }
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to trigger alarm window for: {alarm.Id}", ex);
                }
            });
        }

        #endregion

        #region System Events

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                Logger.Info("System resumed from sleep. Performing instant alarm check.");
                try
                {
                    _alarmManager.CheckAlarms();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to check alarms on sleep resume.", ex);
                }
            }
        }

        #endregion

        #region Tray Icon

        private void TrayIcon_DoubleClicked()
        {
            Dispatcher.Invoke(() =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            });
        }

        private void TrayIcon_ExitMenuSelected()
        {
            Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(
                    "\u30a2\u30d7\u30ea\u3092\u7d42\u4e86\u3059\u308b\u3068\u3001\u8a2d\u5b9a\u3057\u305f\u30a2\u30e9\u30fc\u30e0\u306f\u9cf4\u308a\u307e\u305b\u3093\u3002\u7d42\u4e86\u3057\u307e\u3059\u304b\uff1f",
                    "\u30dd\u30fc\u30bf\u30d6\u30eb \u30a2\u30e9\u30fc\u30e0 \u30af\u30ed\u30c3\u30af",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _isExplicitClose = true;
                    this.Close();
                }
            });
        }

        #endregion

        #region Window Lifecycle

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitClose)
            {
                // Minimize to tray instead of closing
                e.Cancel = true;
                this.Hide();
                Logger.Info("Window hidden (minimized to system tray).");
            }
            else
            {
                _schedulerTimer.Stop();
                SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                _trayIcon?.Dispose();
                Logger.Info("App closed explicitly.");
            }
        }

        #endregion
    }
}
// v1.06 20260620 10:20
// v1.04 20260619 08:24 - Shell with tab navigation