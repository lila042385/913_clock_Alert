// v1.02 20260619 08:20 - Custom clock icon
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.IO;

namespace PortableAlarmClock
{
    public class SystemTrayIcon : IDisposable
    {
        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;

        private const int WM_USER = 0x0400;
        public const int WM_TRAYICON = WM_USER + 256;

        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        // Context Menu Flags
        private const uint TPM_LEFTALIGN = 0x0000;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint MF_STRING = 0x00000000;

        private const int IDI_APPLICATION = 32512;

        // LoadImage constants
        private const int IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;
        private const uint LR_DEFAULTSIZE = 0x00000040;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool Shell_NotifyIcon(int dwMessage, [In] ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string name, int type, int cx, int cy, uint fuLoad);

        private readonly Window _window;
        private IntPtr _hWnd;
        private IntPtr _hIcon;
        private bool _isCreated;
        private readonly int _uid = 1001;

        public event Action? DoubleClicked;
        public event Action? OpenMenuSelected;
        public event Action? ExitMenuSelected;

        public SystemTrayIcon(Window window)
        {
            _window = window;
            
            // Get window handle
            var helper = new WindowInteropHelper(_window);
            _hWnd = helper.EnsureHandle();

            // Load custom clock icon from embedded resource
            _hIcon = LoadClockIcon();
            if (_hIcon == IntPtr.Zero)
            {
                // Fallback to default application icon
                _hIcon = LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
                Logger.Info("Using fallback system icon for tray.");
            }

            // Hook window messages to receive tray notifications
            HwndSource source = HwndSource.FromHwnd(_hWnd);
            source.AddHook(WndProc);
        }

        private IntPtr LoadClockIcon()
        {
            try
            {
                // Try loading from file next to executable
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(baseDir, "clock_icon.ico");
                if (File.Exists(icoPath))
                {
                    IntPtr hIcon = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
                    if (hIcon != IntPtr.Zero)
                    {
                        Logger.Info("Loaded clock icon from file.");
                        return hIcon;
                    }
                }

                // Try loading from embedded resource by saving to temp file
                var uri = new Uri("pack://application:,,,/clock_icon.ico", UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(uri);
                if (streamInfo != null)
                {
                    string tempIcoPath = Path.Combine(Path.GetTempPath(), "PortableAlarmClock_icon.ico");
                    using (var fs = File.Create(tempIcoPath))
                    {
                        streamInfo.Stream.CopyTo(fs);
                    }
                    IntPtr hIcon = LoadImage(IntPtr.Zero, tempIcoPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
                    if (hIcon != IntPtr.Zero)
                    {
                        Logger.Info("Loaded clock icon from embedded resource.");
                        return hIcon;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load clock icon.", ex);
            }
            return IntPtr.Zero;
        }

        public void Create()
        {
            if (_isCreated) return;

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = _uid,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "Portable Alarm Clock"
            };

            _isCreated = Shell_NotifyIcon(NIM_ADD, ref nid);
            if (!_isCreated)
            {
                Logger.Error("Failed to create system tray icon via Shell_NotifyIcon.");
            }
            else
            {
                Logger.Info("System tray icon created successfully.");
            }
        }

        public void Remove()
        {
            if (!_isCreated) return;

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = _uid
            };

            Shell_NotifyIcon(NIM_DELETE, ref nid);
            _isCreated = false;
            Logger.Info("System tray icon removed.");
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TRAYICON)
            {
                int eventId = (int)lParam;
                if (eventId == WM_LBUTTONDBLCLK)
                {
                    DoubleClicked?.Invoke();
                    handled = true;
                }
                else if (eventId == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ShowContextMenu()
        {
            IntPtr hMenu = IntPtr.Zero;
            try
            {
                // Must bring our window to foreground to close context menu correctly when clicked outside
                SetForegroundWindow(_hWnd);

                hMenu = CreatePopupMenu();
                if (hMenu == IntPtr.Zero) return;

                // Add Menu items (ID 1 for Open, ID 2 for Exit)
                AppendMenu(hMenu, MF_STRING, (IntPtr)1, "開く");
                AppendMenu(hMenu, MF_STRING, (IntPtr)2, "終了");

                if (GetCursorPos(out POINT pt))
                {
                    int selectedId = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_LEFTALIGN, pt.X, pt.Y, 0, _hWnd, IntPtr.Zero);
                    if (selectedId == 1)
                    {
                        OpenMenuSelected?.Invoke();
                    }
                    else if (selectedId == 2)
                    {
                        ExitMenuSelected?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to display tray context menu.", ex);
            }
            finally
            {
                if (hMenu != IntPtr.Zero)
                {
                    DestroyMenu(hMenu);
                }
            }
        }

        public void Dispose()
        {
            Remove();
        }
    }
}
// v1.02 20260619 08:20 - Custom clock icon
