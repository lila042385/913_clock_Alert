// v1.00 20260617 23:59
using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace PortableAlarmClock
{
    public static class ThemeManager
    {
        private static bool _isDark;

        public static void Initialize()
        {
            try
            {
                ApplyTheme();
                SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize ThemeManager.", ex);
            }
        }

        private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                // General preference might contain light/dark theme changes
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplyTheme();
                });
            }
        }

        public static void ApplyTheme()
        {
            try
            {
                bool useLightTheme = true;
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue("AppsUseLightTheme");
                        if (value is int intVal)
                        {
                            useLightTheme = intVal == 1;
                        }
                    }
                }

                _isDark = !useLightTheme;

                if (_isDark)
                {
                    // Dark Mode Palette
                    UpdateBrush("WindowBackgroundBrush", Color.FromRgb(32, 32, 32));
                    UpdateBrush("CardBackgroundBrush", Color.FromRgb(45, 45, 45));
                    UpdateBrush("CardBorderBrush", Color.FromRgb(60, 60, 60));
                    UpdateBrush("TextPrimaryBrush", Color.FromRgb(255, 255, 255));
                    UpdateBrush("TextSecondaryBrush", Color.FromRgb(180, 180, 180));
                    UpdateBrush("BorderBrush", Color.FromRgb(60, 60, 60));
                    UpdateBrush("ButtonBackgroundBrush", Color.FromRgb(45, 45, 45));
                    UpdateBrush("ButtonHoverBrush", Color.FromRgb(60, 60, 60));
                    UpdateBrush("ButtonPressedBrush", Color.FromRgb(70, 70, 70));
                    UpdateBrush("ToggleBackgroundBrush", Color.FromRgb(95, 95, 95));
                    UpdateBrush("ToggleThumbBrush", Color.FromRgb(255, 255, 255));
                    UpdateBrush("ListBoxBackgroundBrush", Color.FromRgb(32, 32, 32));
                    UpdateBrush("CardDisabledBrush", Color.FromRgb(40, 40, 40));
                    UpdateBrush("TextDisabledBrush", Color.FromRgb(110, 110, 110));

                    // Accent (Windows 11 Blue - adjusted for Dark)
                    UpdateBrush("AccentBrush", Color.FromRgb(96, 205, 245));
                    UpdateBrush("AccentHoverBrush", Color.FromRgb(111, 214, 250));
                    UpdateBrush("AccentPressedBrush", Color.FromRgb(77, 180, 220));
                }
                else
                {
                    // Light Mode Palette
                    UpdateBrush("WindowBackgroundBrush", Color.FromRgb(243, 243, 243));
                    UpdateBrush("CardBackgroundBrush", Color.FromRgb(255, 255, 255));
                    UpdateBrush("CardBorderBrush", Color.FromRgb(229, 229, 229));
                    UpdateBrush("TextPrimaryBrush", Color.FromRgb(0, 0, 0));
                    UpdateBrush("TextSecondaryBrush", Color.FromRgb(95, 95, 95));
                    UpdateBrush("BorderBrush", Color.FromRgb(229, 229, 229));
                    UpdateBrush("ButtonBackgroundBrush", Color.FromRgb(255, 255, 255));
                    UpdateBrush("ButtonHoverBrush", Color.FromRgb(245, 245, 245));
                    UpdateBrush("ButtonPressedBrush", Color.FromRgb(230, 230, 230));
                    UpdateBrush("ToggleBackgroundBrush", Color.FromRgb(138, 138, 138));
                    UpdateBrush("ToggleThumbBrush", Color.FromRgb(0, 0, 0));
                    UpdateBrush("ListBoxBackgroundBrush", Color.FromRgb(243, 243, 243));
                    UpdateBrush("CardDisabledBrush", Color.FromRgb(248, 248, 248));
                    UpdateBrush("TextDisabledBrush", Color.FromRgb(175, 175, 175));

                    // Accent (Windows 11 Blue)
                    UpdateBrush("AccentBrush", Color.FromRgb(0, 95, 184));
                    UpdateBrush("AccentHoverBrush", Color.FromRgb(25, 114, 196));
                    UpdateBrush("AccentPressedBrush", Color.FromRgb(0, 77, 150));
                }

                Logger.Info($"Applied {(_isDark ? "Dark" : "Light")} theme.");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to apply theme.", ex);
            }
        }

        private static void UpdateBrush(string key, Color color)
        {
            if (Application.Current.Resources.Contains(key))
            {
                if (Application.Current.Resources[key] is SolidColorBrush brush)
                {
                    if (brush.IsFrozen)
                    {
                        Application.Current.Resources[key] = new SolidColorBrush(color);
                    }
                    else
                    {
                        brush.Color = color;
                    }
                }
            }
            else
            {
                Application.Current.Resources[key] = new SolidColorBrush(color);
            }
        }
    }
}
// v1.00 20260617 23:59
