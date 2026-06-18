// v1.00 20260619 08:24
using System;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PortableAlarmClock
{
    public partial class AlarmView : UserControl
    {
        private readonly AlarmManager _alarmManager;

        // Drag & Drop reordering fields
        private bool _isDragging;
        private Point _dragStartPoint;
        private Alarm? _draggedAlarm;
        private ListBoxItem? _draggedItemContainer;
        private ScrollViewer? _listScrollViewer;

        public AlarmView()
        {
            InitializeComponent();

            _alarmManager = App.AlarmManagerInstance ?? new AlarmManager(AppDomain.CurrentDomain.BaseDirectory);
            AlarmsListBox.ItemsSource = _alarmManager.Alarms;

            Loaded += AlarmView_Loaded;
        }

        private void AlarmView_Loaded(object sender, RoutedEventArgs e)
        {
            _listScrollViewer = FindVisualChild<ScrollViewer>(AlarmsListBox);
        }

        #region UI Handlers (Add, Edit, Delete)

        private void AddAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AlarmEditDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.ResultAlarm != null)
            {
                _alarmManager.AddAlarm(dialog.ResultAlarm);
            }
        }

        private void CardGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var grid = sender as Grid;
                var alarm = grid?.DataContext as Alarm;
                if (alarm != null)
                {
                    EditAlarm(alarm);
                }
            }
        }

        private void EditAlarm(Alarm alarm)
        {
            var dialog = new AlarmEditDialog(alarm)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.ResultAlarm != null)
            {
                if (dialog.IsDeleted)
                {
                    _alarmManager.RemoveAlarm(alarm);
                }
                else
                {
                    _alarmManager.UpdateAlarm(dialog.ResultAlarm);
                }
            }
        }

        private void QuickDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                var alarm = btn?.DataContext as Alarm;
                if (alarm != null)
                {
                    var result = MessageBox.Show(
                        $"{alarm.TimeString} {alarm.Title}\n\nThis alarm will be deleted.",
                        "Confirm Delete",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                    {
                        _alarmManager.RemoveAlarm(alarm);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to delete alarm.", ex);
            }
            e.Handled = true;
        }

        private void AlarmToggle_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var alarm = cb?.DataContext as Alarm;
            if (alarm != null)
            {
                _alarmManager.ToggleAlarm(alarm.Id, true);
            }
        }

        private void AlarmToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var alarm = cb?.DataContext as Alarm;
            if (alarm != null)
            {
                _alarmManager.ToggleAlarm(alarm.Id, false);
            }
        }

        #endregion

        #region Drag and Drop Reordering

        private void AlarmsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != AlarmsListBox)
            {
                if (obj is ToggleButton || obj is Button || obj is CheckBox)
                {
                    return;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }

            _dragStartPoint = e.GetPosition(AlarmsListBox);

            _draggedItemContainer = GetListBoxItemFromPoint(AlarmsListBox, _dragStartPoint);
            if (_draggedItemContainer != null)
            {
                _draggedAlarm = _draggedItemContainer.DataContext as Alarm;
            }
        }

        private void AlarmsListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedAlarm == null || _draggedItemContainer == null)
            {
                return;
            }

            Point currentPoint = e.GetPosition(AlarmsListBox);
            Vector diff = _dragStartPoint - currentPoint;

            if (!_isDragging && (Math.Abs(diff.X) >= 8 || Math.Abs(diff.Y) >= 8))
            {
                _isDragging = true;
                _draggedItemContainer.Opacity = 0.5;
            }

            if (_isDragging)
            {
                ListBoxItem? hoveredItemContainer = GetListBoxItemFromPoint(AlarmsListBox, currentPoint);
                if (hoveredItemContainer != null && hoveredItemContainer != _draggedItemContainer)
                {
                    Alarm? targetAlarm = hoveredItemContainer.DataContext as Alarm;
                    if (targetAlarm != null)
                    {
                        int oldIdx = _alarmManager.Alarms.IndexOf(_draggedAlarm);
                        int newIdx = _alarmManager.Alarms.IndexOf(targetAlarm);

                        if (oldIdx >= 0 && newIdx >= 0 && oldIdx != newIdx)
                        {
                            _alarmManager.Alarms.Move(oldIdx, newIdx);
                        }
                    }
                }

                if (_listScrollViewer != null)
                {
                    double containerHeight = AlarmsListBox.ActualHeight;
                    double y = e.GetPosition(AlarmsListBox).Y;
                    double scrollThreshold = 30;

                    if (y < scrollThreshold)
                    {
                        _listScrollViewer.ScrollToVerticalOffset(_listScrollViewer.VerticalOffset - 2);
                    }
                    else if (y > containerHeight - scrollThreshold)
                    {
                        _listScrollViewer.ScrollToVerticalOffset(_listScrollViewer.VerticalOffset + 2);
                    }
                }
            }
        }

        private void AlarmsListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CleanUpDragState(saveChanges: true);
        }

        private void CleanUpDragState(bool saveChanges)
        {
            if (_draggedItemContainer != null)
            {
                _draggedItemContainer.Opacity = 1.0;
            }

            if (_isDragging && saveChanges && _draggedAlarm != null)
            {
                try
                {
                    _alarmManager.SaveAlarms();
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to save alarms order after drag reorder.", ex);
                }
            }

            _isDragging = false;
            _draggedAlarm = null;
            _draggedItemContainer = null;
        }

        private ListBoxItem? GetListBoxItemFromPoint(ListBox listBox, Point point)
        {
            HitTestResult hitTest = VisualTreeHelper.HitTest(listBox, point);
            if (hitTest == null) return null;

            DependencyObject obj = hitTest.VisualHit;
            while (obj != null && obj != listBox)
            {
                if (obj is ListBoxItem item)
                {
                    return item;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        #endregion

        #region Keyboard Reordering

        private void AlarmsListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (AlarmsListBox.SelectedItem is Alarm selectedAlarm)
            {
                bool altPressed = Keyboard.Modifiers == ModifierKeys.Alt;
                if (altPressed && (e.Key == Key.Up || e.Key == Key.Down))
                {
                    int oldIdx = _alarmManager.Alarms.IndexOf(selectedAlarm);
                    int newIdx = oldIdx + (e.Key == Key.Up ? -1 : 1);

                    if (oldIdx >= 0 && newIdx >= 0 && newIdx < _alarmManager.Alarms.Count)
                    {
                        _alarmManager.Alarms.Move(oldIdx, newIdx);
                        AlarmsListBox.SelectedIndex = newIdx;

                        try
                        {
                            _alarmManager.SaveAlarms();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to save alarms order after keyboard reorder.", ex);
                        }
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    EditAlarm(selectedAlarm);
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Helper Functions

        private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t)
                {
                    return t;
                }

                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        #endregion
    }
}
// v1.00 20260619 08:24
