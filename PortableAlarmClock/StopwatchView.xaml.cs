// v1.00 20260619 08:24
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PortableAlarmClock
{
    public partial class StopwatchView : UserControl
    {
        private enum StopwatchState { Stopped, Running }

        private StopwatchState _state = StopwatchState.Stopped;
        private readonly System.Diagnostics.Stopwatch _stopwatch = new();
        private readonly DispatcherTimer _displayTimer;
        private readonly ObservableCollection<LapRecord> _laps = new();
        private TimeSpan _lastLapTime = TimeSpan.Zero;

        public StopwatchView()
        {
            InitializeComponent();

            LapsListBox.ItemsSource = _laps;

            _displayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _displayTimer.Tick += DisplayTimer_Tick;

            UpdateUI();
        }

        #region Stopwatch Logic

        private void StartStopwatch()
        {
            _stopwatch.Start();
            _displayTimer.Start();
            _state = StopwatchState.Running;

            Logger.Info("Stopwatch started.");
            UpdateUI();
        }

        private void StopStopwatch()
        {
            _stopwatch.Stop();
            _displayTimer.Stop();
            _state = StopwatchState.Stopped;

            // Update display one final time to show precise stopped time
            UpdateElapsedDisplay();

            Logger.Info($"Stopwatch stopped at {_stopwatch.Elapsed}");
            UpdateUI();
        }

        private void RecordLap()
        {
            var totalElapsed = _stopwatch.Elapsed;
            var lapTime = totalElapsed - _lastLapTime;
            _lastLapTime = totalElapsed;

            var lap = new LapRecord
            {
                LapNumber = $"Lap {_laps.Count + 1}",
                LapTime = lapTime,
                TotalTime = totalElapsed
            };

            _laps.Insert(0, lap); // newest lap at top
            Logger.Info($"Lap {_laps.Count}: {lapTime} (Total: {totalElapsed})");
        }

        private void ResetStopwatch()
        {
            _stopwatch.Reset();
            _displayTimer.Stop();
            _state = StopwatchState.Stopped;
            _lastLapTime = TimeSpan.Zero;
            _laps.Clear();

            Logger.Info("Stopwatch reset.");
            UpdateElapsedDisplay();
            UpdateUI();
        }

        #endregion

        #region UI Update

        private void DisplayTimer_Tick(object? sender, EventArgs e)
        {
            UpdateElapsedDisplay();
        }

        private void UpdateElapsedDisplay()
        {
            var elapsed = _stopwatch.Elapsed;
            ElapsedText.Text = FormatTimeSpan(elapsed);
        }

        private void UpdateUI()
        {
            switch (_state)
            {
                case StopwatchState.Stopped:
                    StartStopIcon.Text = "\uE768"; // Play

                    if (_stopwatch.ElapsedMilliseconds > 0)
                    {
                        // Paused state - show reset button
                        LapResetButton.IsEnabled = true;
                        LapResetIcon.Text = "\uE72C"; // Refresh/Reset
                        LapResetIcon.ToolTip = "Reset";
                    }
                    else
                    {
                        // Initial state
                        LapResetButton.IsEnabled = false;
                        LapResetIcon.Text = "\uEA3A"; // Flag/Lap
                        LapResetIcon.ToolTip = "Lap";
                    }
                    break;

                case StopwatchState.Running:
                    StartStopIcon.Text = "\uE769"; // Pause
                    LapResetButton.IsEnabled = true;
                    LapResetIcon.Text = "\uEA3A"; // Flag/Lap
                    LapResetIcon.ToolTip = "Lap";
                    break;
            }
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds / 10:D2}";
        }

        #endregion

        #region Event Handlers

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_state)
            {
                case StopwatchState.Stopped:
                    StartStopwatch();
                    break;
                case StopwatchState.Running:
                    StopStopwatch();
                    break;
            }
        }

        private void LapResetButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_state)
            {
                case StopwatchState.Running:
                    RecordLap();
                    break;
                case StopwatchState.Stopped:
                    if (_stopwatch.ElapsedMilliseconds > 0)
                    {
                        ResetStopwatch();
                    }
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a single lap record for the stopwatch.
    /// </summary>
    public class LapRecord
    {
        public string LapNumber { get; set; } = string.Empty;
        public TimeSpan LapTime { get; set; }
        public TimeSpan TotalTime { get; set; }

        public string LapTimeString => FormatTimeSpan(LapTime);
        public string TotalTimeString => FormatTimeSpan(TotalTime);

        private static string FormatTimeSpan(TimeSpan ts)
        {
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds / 10:D2}";
        }
    }
}
// v1.00 20260619 08:24
