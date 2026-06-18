// v1.00 20260619 08:24
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PortableAlarmClock
{
    public partial class TimerView : UserControl
    {
        private enum TimerState { Idle, Running, Paused, Finished }

        private TimerState _state = TimerState.Idle;
        private readonly DispatcherTimer _timer;
        private int _setHours;
        private int _setMinutes;
        private int _setSeconds;
        private TimeSpan _totalDuration;
        private TimeSpan _remaining;
        private DateTime _lastTickTime;

        public TimerView()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += Timer_Tick;

            UpdateUI();
        }

        #region Timer Logic

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            var elapsed = now - _lastTickTime;
            _lastTickTime = now;

            _remaining -= elapsed;

            if (_remaining <= TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
                _timer.Stop();
                _state = TimerState.Finished;

                // Play alarm sound
                try
                {
                    AlarmSoundPlayer.Play();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to play timer completion sound.", ex);
                }

                // Show completion notification
                MessageBox.Show(
                    Window.GetWindow(this),
                    "Timer completed!",
                    "Timer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Stop sound after dismiss
                try
                {
                    AlarmSoundPlayer.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to stop timer completion sound.", ex);
                }

                _state = TimerState.Idle;
                UpdateUI();
                return;
            }

            UpdateCountdownDisplay();
            UpdateProgressArc();
        }

        private void StartTimer()
        {
            int totalSeconds = _setHours * 3600 + _setMinutes * 60 + _setSeconds;
            if (totalSeconds <= 0) return;

            _totalDuration = TimeSpan.FromSeconds(totalSeconds);
            _remaining = _totalDuration;
            _lastTickTime = DateTime.Now;
            _state = TimerState.Running;
            _timer.Start();

            Logger.Info($"Timer started: {_totalDuration}");
            UpdateUI();
        }

        private void ResumeTimer()
        {
            _lastTickTime = DateTime.Now;
            _state = TimerState.Running;
            _timer.Start();

            Logger.Info("Timer resumed.");
            UpdateUI();
        }

        private void PauseTimer()
        {
            _timer.Stop();
            _state = TimerState.Paused;

            Logger.Info($"Timer paused. Remaining: {_remaining}");
            UpdateUI();
        }

        private void ResetTimer()
        {
            _timer.Stop();
            _state = TimerState.Idle;
            _remaining = TimeSpan.Zero;

            Logger.Info("Timer reset.");
            UpdateUI();
        }

        #endregion

        #region UI Update

        private void UpdateUI()
        {
            switch (_state)
            {
                case TimerState.Idle:
                    TimeSettingPanel.Visibility = Visibility.Visible;
                    CountdownText.Visibility = Visibility.Collapsed;
                    ProgressArc.Data = null;
                    ResetButton.Visibility = Visibility.Collapsed;
                    PlaceholderRight.Visibility = Visibility.Collapsed;
                    StartPauseIcon.Text = "\uE768"; // Play
                    HourText.Text = _setHours.ToString("D2");
                    MinuteText.Text = _setMinutes.ToString("D2");
                    SecondText.Text = _setSeconds.ToString("D2");
                    break;

                case TimerState.Running:
                    TimeSettingPanel.Visibility = Visibility.Collapsed;
                    CountdownText.Visibility = Visibility.Visible;
                    ResetButton.Visibility = Visibility.Visible;
                    PlaceholderRight.Visibility = Visibility.Visible;
                    StartPauseIcon.Text = "\uE769"; // Pause
                    UpdateCountdownDisplay();
                    UpdateProgressArc();
                    break;

                case TimerState.Paused:
                    TimeSettingPanel.Visibility = Visibility.Collapsed;
                    CountdownText.Visibility = Visibility.Visible;
                    ResetButton.Visibility = Visibility.Visible;
                    PlaceholderRight.Visibility = Visibility.Visible;
                    StartPauseIcon.Text = "\uE768"; // Play
                    UpdateCountdownDisplay();
                    UpdateProgressArc();
                    break;
            }
        }

        private void UpdateCountdownDisplay()
        {
            int totalSec = (int)Math.Ceiling(_remaining.TotalSeconds);
            int h = totalSec / 3600;
            int m = (totalSec % 3600) / 60;
            int s = totalSec % 60;
            CountdownText.Text = $"{h:D2}:{m:D2}:{s:D2}";
        }

        private void UpdateProgressArc()
        {
            if (_totalDuration.TotalSeconds <= 0) return;

            double fraction = _remaining.TotalSeconds / _totalDuration.TotalSeconds;
            double angle = fraction * 360.0;

            double cx = 140, cy = 140, r = 137;
            double startAngleRad = -Math.PI / 2;
            double endAngleRad = startAngleRad + (angle * Math.PI / 180.0);

            double x1 = cx + r * Math.Cos(startAngleRad);
            double y1 = cy + r * Math.Sin(startAngleRad);
            double x2 = cx + r * Math.Cos(endAngleRad);
            double y2 = cy + r * Math.Sin(endAngleRad);

            int largeArc = angle > 180 ? 1 : 0;

            if (angle >= 359.99)
            {
                // Full circle - draw two half arcs
                double xMid = cx + r * Math.Cos(startAngleRad + Math.PI);
                double yMid = cy + r * Math.Sin(startAngleRad + Math.PI);
                ProgressArc.Data = Geometry.Parse(
                    $"M {x1:F2},{y1:F2} A {r:F2},{r:F2} 0 1 1 {xMid:F2},{yMid:F2} A {r:F2},{r:F2} 0 1 1 {x1:F2},{y1:F2}");
            }
            else if (angle > 0.01)
            {
                ProgressArc.Data = Geometry.Parse(
                    $"M {x1:F2},{y1:F2} A {r:F2},{r:F2} 0 {largeArc} 1 {x2:F2},{y2:F2}");
            }
            else
            {
                ProgressArc.Data = null;
            }
        }

        #endregion

        #region Event Handlers

        private void StartPauseButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_state)
            {
                case TimerState.Idle:
                    StartTimer();
                    break;
                case TimerState.Running:
                    PauseTimer();
                    break;
                case TimerState.Paused:
                    ResumeTimer();
                    break;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetTimer();
        }

        // Hour spinner
        private void HourUpButton_Click(object sender, RoutedEventArgs e)
        {
            _setHours = (_setHours + 1) % 24;
            HourText.Text = _setHours.ToString("D2");
        }

        private void HourDownButton_Click(object sender, RoutedEventArgs e)
        {
            _setHours = (_setHours - 1 + 24) % 24;
            HourText.Text = _setHours.ToString("D2");
        }

        private void HourText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _setHours = ((_setHours + (e.Delta > 0 ? 1 : -1)) + 24) % 24;
            HourText.Text = _setHours.ToString("D2");
            e.Handled = true;
        }

        // Minute spinner
        private void MinuteUpButton_Click(object sender, RoutedEventArgs e)
        {
            _setMinutes = (_setMinutes + 1) % 60;
            MinuteText.Text = _setMinutes.ToString("D2");
        }

        private void MinuteDownButton_Click(object sender, RoutedEventArgs e)
        {
            _setMinutes = (_setMinutes - 1 + 60) % 60;
            MinuteText.Text = _setMinutes.ToString("D2");
        }

        private void MinuteText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _setMinutes = ((_setMinutes + (e.Delta > 0 ? 1 : -1)) + 60) % 60;
            MinuteText.Text = _setMinutes.ToString("D2");
            e.Handled = true;
        }

        // Second spinner
        private void SecondUpButton_Click(object sender, RoutedEventArgs e)
        {
            _setSeconds = (_setSeconds + 1) % 60;
            SecondText.Text = _setSeconds.ToString("D2");
        }

        private void SecondDownButton_Click(object sender, RoutedEventArgs e)
        {
            _setSeconds = (_setSeconds - 1 + 60) % 60;
            SecondText.Text = _setSeconds.ToString("D2");
        }

        private void SecondText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _setSeconds = ((_setSeconds + (e.Delta > 0 ? 1 : -1)) + 60) % 60;
            SecondText.Text = _setSeconds.ToString("D2");
            e.Handled = true;
        }

        #endregion
    }
}
// v1.00 20260619 08:24
