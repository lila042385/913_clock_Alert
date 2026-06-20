// v1.00 20260620 10:15
// 履歴: 新規作成。設定画面のイベント処理。自動起動トグルのチェック状態をレジストリと同期
using System;
using System.Windows;
using System.Windows.Controls;

namespace PortableAlarmClock
{
    public partial class SettingsView : UserControl
    {
        private bool _isInitializing = true;

        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isInitializing = true;
                // レジストリから自動起動の登録状態を取得してトグルに反映
                StartupToggle.IsChecked = StartupManager.IsStartupEnabled();
            }
            catch (Exception ex)
            {
                Logger.Error("設定の初期化に失敗しました。", ex);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void StartupToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            try
            {
                StartupManager.SetStartup(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自動起動の設定に失敗しました。\nエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                // 失敗した場合はトグルの状態を元に戻す
                _isInitializing = true;
                StartupToggle.IsChecked = false;
                _isInitializing = false;
            }
        }

        private void StartupToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            try
            {
                StartupManager.SetStartup(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自動起動の解除に失敗しました。\nエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                // 失敗した場合はトグルの状態を元に戻す
                _isInitializing = true;
                StartupToggle.IsChecked = true;
                _isInitializing = false;
            }
        }
    }
}
// v1.00 20260620 10:15
