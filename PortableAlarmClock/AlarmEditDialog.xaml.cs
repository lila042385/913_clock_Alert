// v1.00 20260617 23:35
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace PortableAlarmClock
{
    public partial class AlarmEditDialog : Window
    {
        public Alarm? ResultAlarm { get; private set; }
        public bool IsDeleted { get; private set; }

        private readonly Alarm? _originalAlarm;
        private readonly bool _isEditMode;

        public AlarmEditDialog(Alarm? alarm = null)
        {
            InitializeComponent();
            
            _originalAlarm = alarm;
            _isEditMode = alarm != null;

            InitializeComboBoxes();
            LoadAlarmData();
            
            // Allow dragging the borderless window
            MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }

        private void InitializeComboBoxes()
        {
            // Hour ComboBox (00 - 23)
            for (int i = 0; i < 24; i++)
            {
                HourComboBox.Items.Add($"{i:D2}");
            }

            // Minute ComboBox (00 - 59)
            for (int i = 0; i < 60; i++)
            {
                MinuteComboBox.Items.Add($"{i:D2}");
            }

            // Sound Options (Currently using a few simulated options)
            SoundComboBox.Items.Add("電子音 (デフォルト)");
            SoundComboBox.Items.Add("チャイム");
            SoundComboBox.Items.Add("メロディ");
            SoundComboBox.SelectedIndex = 0;

            // Snooze Options
            SnoozeComboBox.Items.Add(new KeyValuePair<int, string>(5, "5分"));
            SnoozeComboBox.Items.Add(new KeyValuePair<int, string>(10, "10分"));
            SnoozeComboBox.Items.Add(new KeyValuePair<int, string>(15, "15分"));
            SnoozeComboBox.Items.Add(new KeyValuePair<int, string>(20, "20分"));
            SnoozeComboBox.Items.Add(new KeyValuePair<int, string>(30, "30分"));
            SnoozeComboBox.DisplayMemberPath = "Value";
            SnoozeComboBox.SelectedValuePath = "Key";
            SnoozeComboBox.SelectedValue = 10; // Default to 10 minutes
        }

        private void LoadAlarmData()
        {
            if (_isEditMode && _originalAlarm != null)
            {
                TitleTextBlock.Text = "アラームの編集";
                DeleteButton.Visibility = Visibility.Visible;

                HourComboBox.SelectedItem = $"{_originalAlarm.Hour:D2}";
                MinuteComboBox.SelectedItem = $"{_originalAlarm.Minute:D2}";
                TitleTextBox.Text = _originalAlarm.Title;

                // Load Weekdays
                var weekdays = _originalAlarm.Weekdays;
                SunToggle.IsChecked = weekdays.Contains(DayOfWeek.Sunday);
                MonToggle.IsChecked = weekdays.Contains(DayOfWeek.Monday);
                TueToggle.IsChecked = weekdays.Contains(DayOfWeek.Tuesday);
                WedToggle.IsChecked = weekdays.Contains(DayOfWeek.Wednesday);
                ThuToggle.IsChecked = weekdays.Contains(DayOfWeek.Thursday);
                FriToggle.IsChecked = weekdays.Contains(DayOfWeek.Friday);
                SatToggle.IsChecked = weekdays.Contains(DayOfWeek.Saturday);

                // Load Sound
                if (SoundComboBox.Items.Contains(_originalAlarm.SoundName))
                {
                    SoundComboBox.SelectedItem = _originalAlarm.SoundName;
                }
                else
                {
                    SoundComboBox.SelectedIndex = 0;
                }

                // Load Snooze
                SnoozeComboBox.SelectedValue = _originalAlarm.SnoozeMinutes;
            }
            else
            {
                TitleTextBlock.Text = "アラームの追加";
                DeleteButton.Visibility = Visibility.Collapsed;

                // Set initial time to next hour
                DateTime initTime = DateTime.Now.AddHours(1);
                HourComboBox.SelectedItem = $"{initTime.Hour:D2}";
                MinuteComboBox.SelectedItem = "00";

                TitleTextBox.Text = "アラーム";
                
                // No weekdays checked by default
                SunToggle.IsChecked = false;
                MonToggle.IsChecked = false;
                TueToggle.IsChecked = false;
                WedToggle.IsChecked = false;
                ThuToggle.IsChecked = false;
                FriToggle.IsChecked = false;
                SatToggle.IsChecked = false;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int hour = Convert.ToInt32(HourComboBox.SelectedItem ?? "08");
            int minute = Convert.ToInt32(MinuteComboBox.SelectedItem ?? "00");
            string title = string.IsNullOrWhiteSpace(TitleTextBox.Text) ? "アラーム" : TitleTextBox.Text.Trim();
            
            var weekdays = new List<DayOfWeek>();
            if (SunToggle.IsChecked == true) weekdays.Add(DayOfWeek.Sunday);
            if (MonToggle.IsChecked == true) weekdays.Add(DayOfWeek.Monday);
            if (TueToggle.IsChecked == true) weekdays.Add(DayOfWeek.Tuesday);
            if (WedToggle.IsChecked == true) weekdays.Add(DayOfWeek.Wednesday);
            if (ThuToggle.IsChecked == true) weekdays.Add(DayOfWeek.Thursday);
            if (FriToggle.IsChecked == true) weekdays.Add(DayOfWeek.Friday);
            if (SatToggle.IsChecked == true) weekdays.Add(DayOfWeek.Saturday);

            string soundName = SoundComboBox.SelectedItem?.ToString() ?? "電子音 (デフォルト)";
            int snoozeMinutes = (int)(SnoozeComboBox.SelectedValue ?? 10);

            if (_isEditMode && _originalAlarm != null)
            {
                ResultAlarm = new Alarm
                {
                    Id = _originalAlarm.Id,
                    Hour = hour,
                    Minute = minute,
                    Title = title,
                    Weekdays = weekdays,
                    IsEnabled = _originalAlarm.IsEnabled, // Keep current state
                    SoundName = soundName,
                    SnoozeMinutes = snoozeMinutes,
                    SortOrder = _originalAlarm.SortOrder,
                    LastTriggered = _originalAlarm.LastTriggered,
                    CreatedAt = _originalAlarm.CreatedAt,
                    UpdatedAt = DateTime.Now
                };
            }
            else
            {
                ResultAlarm = new Alarm
                {
                    Id = Guid.NewGuid(),
                    Hour = hour,
                    Minute = minute,
                    Title = title,
                    Weekdays = weekdays,
                    IsEnabled = true, // Enable by default
                    SoundName = soundName,
                    SnoozeMinutes = snoozeMinutes,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditMode && _originalAlarm != null)
            {
                ResultAlarm = _originalAlarm;
                IsDeleted = true;
                DialogResult = true;
                Close();
            }
        }
    }
}
// v1.00 20260617 23:35
