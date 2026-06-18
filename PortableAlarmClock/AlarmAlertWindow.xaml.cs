// v1.00 20260617 23:50
using System;
using System.Windows;
using System.Windows.Threading;

namespace PortableAlarmClock
{
    public partial class AlarmAlertWindow : Window
    {
        public Alarm Alarm { get; }
        public bool IsSnoozed { get; private set; }
        public bool IsDismissed { get; private set; }

        private readonly DispatcherTimer _topmostTimer;

        public AlarmAlertWindow(Alarm alarm)
        {
            InitializeComponent();
            Alarm = alarm;

            TimeTextBlock.Text = alarm.TimeString;
            TitleTextBlock.Text = string.IsNullOrWhiteSpace(alarm.Title) ? "アラーム" : alarm.Title;

            // Enforce window stays on top
            _topmostTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _topmostTimer.Tick += TopmostTimer_Tick;
            _topmostTimer.Start();

            // Start playing alarm sound
            AlarmSoundPlayer.Play();

            Closed += AlarmAlertWindow_Closed;
        }

        private void TopmostTimer_Tick(object? sender, EventArgs e)
        {
            // Regularly keep the window on top of others
            try
            {
                if (!Topmost)
                {
                    Topmost = true;
                }
                Activate();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to keep AlarmAlertWindow topmost.", ex);
            }
        }

        private void SnoozeButton_Click(object sender, RoutedEventArgs e)
        {
            IsSnoozed = true;
            Close();
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            IsDismissed = true;
            Close();
        }

        private void AlarmAlertWindow_Closed(object? sender, EventArgs e)
        {
            _topmostTimer.Stop();
            AlarmSoundPlayer.Stop();
        }
    }
}
// v1.00 20260617 23:50
