using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rainmeter;

namespace SkinTray
{
    // Custom color table for dark context menus.
    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 60);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 45);
        public override Color MenuBorder => Color.FromArgb(70, 70, 70);
        public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 45);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 45);
        public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 45);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(70, 70, 70);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(70, 70, 70);
    }

    // Custom renderer for dark context menus.
    public class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        public DarkToolStripRenderer() : base(new DarkColorTable()) { }
    }

    // Low-level mouse hook for capturing mouse wheel events over tray icon
    public class MouseHook
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, [Out] out RECT iconLocation);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NOTIFYICONIDENTIFIER
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public Guid guidItem;
        }

        public event EventHandler<int> MouseWheel;
        private IntPtr _trayWindowHandle;
        private uint _trayIconID;
        private API _api;

        public MouseHook(IntPtr trayWindowHandle, uint trayIconID, API api)
        {
            _trayWindowHandle = trayWindowHandle;
            _trayIconID = trayIconID;
            _api = api;
            _proc = HookCallback;
        }

        public void Install()
        {
            if (_hookID == IntPtr.Zero)
            {
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                    if (_hookID == IntPtr.Zero && _api != null)
                    {
                        int error = Marshal.GetLastWin32Error();
                        _api.Log(API.LogType.Error, $"Failed to install mouse hook. Error code: {error}");
                    }
                }
            }
        }

        public void Uninstall()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                try
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                    // Check if mouse is over tray icon
                    NOTIFYICONIDENTIFIER nii = new NOTIFYICONIDENTIFIER
                    {
                        cbSize = Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER)),
                        hWnd = _trayWindowHandle,
                        uID = _trayIconID,
                        guidItem = Guid.Empty
                    };

                    RECT rect;
                    int result = Shell_NotifyIconGetRect(ref nii, out rect);
                    if (result == 0) // S_OK
                    {
                        if (hookStruct.pt.x >= rect.left && hookStruct.pt.x <= rect.right &&
                            hookStruct.pt.y >= rect.top && hookStruct.pt.y <= rect.bottom)
                        {
                            short delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                            MouseWheel?.Invoke(this, delta);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_api != null)
                        _api.Log(API.LogType.Error, $"Error in mouse hook callback: {ex.Message}");
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }

    public class Measure
    {
        private API _api;
        private NotifyIcon _trayIcon;
        private MouseHook _mouseHook; // For WM_MOUSEWHEEL events
        private string _iconPath;
        private string _toolTipText;
        private string _leftClickAction;
        private string _rightClickAction;
        private string _middleClickAction;
        private string _doubleClickAction;
        private string _mouseWheelUpAction;
        private string _mouseWheelDownAction;
        private int _darkContext;  // 1 for dark mode context menu

        public Measure()
        {
            // Initialize the tray icon only once
            _trayIcon = new NotifyIcon
            {
                Visible = true
            };
        }

        // Dispose the mouse hook and the tray icon
        public void Dispose()
        {
            if (_mouseHook != null)
            {
                _mouseHook.Uninstall();
                _mouseHook = null;
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        /// <summary>
        /// Updates the icon and other properties without recreating the NotifyIcon.
        /// </summary>
        public void Reload(API api, ref double maxValue)
        {
            _api = api;

            // Check if the skin has disabled the tray icon
            int disabled = _api.ReadInt("Disabled", 0);
            if (disabled == 1)
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                }
                return;
            }
            else
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = true;
                }
            }

            // Read configuration settings from Rainmeter
            string newIconPath = _api.ReadString("Icon", "");
            _toolTipText = _api.ReadString("ToolTipText", "Tray Icon Plugin");
            _leftClickAction = _api.ReadString("LeftMouseUpAction", "");
            _rightClickAction = _api.ReadString("RightMouseUpAction", "");
            _middleClickAction = _api.ReadString("MiddleMouseUpAction", "");
            _doubleClickAction = _api.ReadString("DoubleClickAction", "");
            _mouseWheelUpAction = _api.ReadString("MouseWheelUpAction", "");
            _mouseWheelDownAction = _api.ReadString("MouseWheelDownAction", "");
            _darkContext = _api.ReadInt("DarkContext", 0);

            // Update tooltip text regardless of icon change
            _trayIcon.Text = _toolTipText;

            // Only update the icon if the path has changed to avoid unnecessary refresh
            if (!string.IsNullOrEmpty(newIconPath) && newIconPath != _iconPath)
            {
                _iconPath = newIconPath;
                try
                {
                    // Dispose of the previous icon if one exists
                    if (_trayIcon.Icon != null)
                    {
                        _trayIcon.Icon.Dispose();
                    }
                    _trayIcon.Icon = new System.Drawing.Icon(_iconPath);
                }
                catch (Exception ex)
                {
                    _api.Log(API.LogType.Error, $"Error loading icon from path '{_iconPath}': {ex.Message}");
                    // Optionally, set a default icon here.
                }
            }

            // Set up the context menu if defined in the configuration
            int menuCount = _api.ReadInt("ContextMenuItemCount", 0);
            if (menuCount > 0)
            {
                // Create a new context menu and attach it.
                ContextMenuStrip cms = new ContextMenuStrip();
                // If dark mode is enabled, set a custom renderer.
                if (_darkContext == 1)
                {
                    cms.Renderer = new DarkToolStripRenderer();
                    cms.BackColor = Color.FromArgb(45, 45, 45);
                    cms.ForeColor = Color.White;
                }
                for (int i = 1; i <= menuCount; i++)
                {
                    string itemText = _api.ReadString("ContextMenuItem" + i, "");
                    string itemAction = _api.ReadString("ContextMenuAction" + i, "");
                    if (!string.IsNullOrEmpty(itemText) && !string.IsNullOrEmpty(itemAction))
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(itemText);
                        // Adjust item colors if in dark mode
                        if (_darkContext == 1)
                        {
                            item.BackColor = Color.FromArgb(45, 45, 45);
                            item.ForeColor = Color.White;
                        }
                        item.Click += (sender, args) => { _api.Execute(itemAction); };
                        cms.Items.Add(item);
                    }
                }
                _trayIcon.ContextMenuStrip = cms;
            }
            else
            {
                // Clear any existing context menu if none is defined.
                _trayIcon.ContextMenuStrip = null;
            }

            // Attach or update mouse event handlers (only attach once)
            AttachMouseEventHandlers();

            // Install low-level mouse hook for mouse wheel events (only attach once)
            InstallMouseHook();
        }

        // Attach mouse event handlers if not already attached
        private void AttachMouseEventHandlers()
        {
            // Avoid attaching multiple times
            _trayIcon.MouseUp -= TrayIcon_MouseUp;
            _trayIcon.MouseDoubleClick -= TrayIcon_MouseDoubleClick;

            _trayIcon.MouseUp += TrayIcon_MouseUp;
            _trayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;
        }

        private void TrayIcon_MouseUp(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left && !string.IsNullOrEmpty(_leftClickAction))
            {
                _api.Execute(_leftClickAction);
            }
            else if (args.Button == MouseButtons.Middle && !string.IsNullOrEmpty(_middleClickAction))
            {
                _api.Execute(_middleClickAction);
            }
            else if (args.Button == MouseButtons.Right && !string.IsNullOrEmpty(_rightClickAction))
            {
                // Execute right-click action regardless of context menu presence
                // The context menu will still show if defined (handled by NotifyIcon)
                _api.Execute(_rightClickAction);
            }
        }

        private void TrayIcon_MouseDoubleClick(object sender, MouseEventArgs args)
        {
            if (!string.IsNullOrEmpty(_doubleClickAction))
            {
                _api.Execute(_doubleClickAction);
            }
        }

        // Install low-level mouse hook for mouse wheel events if not already installed
        private void InstallMouseHook()
        {
            if (_mouseHook != null)
                return;

            try
            {
                // Get the window handle from the NotifyIcon
                FieldInfo windowField = typeof(NotifyIcon).GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
                if (windowField != null)
                {
                    NativeWindow window = windowField.GetValue(_trayIcon) as NativeWindow;
                    if (window != null && window.Handle != IntPtr.Zero)
                    {
                        _mouseHook = new MouseHook(window.Handle, 1, _api);
                        _mouseHook.MouseWheel += (sender, delta) =>
                        {
                            if (delta > 0 && !string.IsNullOrEmpty(_mouseWheelUpAction))
                            {
                                _api.Execute(_mouseWheelUpAction);
                            }
                            else if (delta < 0 && !string.IsNullOrEmpty(_mouseWheelDownAction))
                            {
                                _api.Execute(_mouseWheelDownAction);
                            }
                        };
                        _mouseHook.Install();
                    }
                }
            }
            catch (Exception ex)
            {
                _api.Log(API.LogType.Error, $"Error installing mouse hook for mouse wheel events: {ex.Message}");
            }
        }

        // Update method (called periodically to refresh the skin)
        public double Update()
        {
            return 0.0;
        }
    }

    public static class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Dispose();
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            return 0.0;
        }
    }
}
