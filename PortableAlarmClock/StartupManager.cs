// v1.00 20260620 10:10
// 履歴: 新規作成。レジストリ(HKCU)を使用したWindows起動時自動実行の登録・解除マネージャー
using System;
using Microsoft.Win32;

namespace PortableAlarmClock
{
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "PortableAlarmClock";

        /// <summary>
        /// 現在自動起動が有効になっているかどうかを判定します。
        /// </summary>
        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key != null)
                    {
                        return key.GetValue(AppName) != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("自動起動状態の読み込みに失敗しました。", ex);
            }
            return false;
        }

        /// <summary>
        /// 自動起動の登録または解除を行います。
        /// </summary>
        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            // 自身の実行可能ファイルパスを取得
                            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            // 常駐起動するために --minimized 引数を追加して登録
                            key.SetValue(AppName, $"\"{exePath}\" --minimized");
                            Logger.Info("スタートアップに登録しました。");
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                            Logger.Info("スタートアップから削除しました。");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"自動起動設定の変更に失敗しました (enable: {enable})。", ex);
                throw;
            }
        }
    }
}
// v1.00 20260620 10:10
