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

    // Custom NativeWindow to intercept WM_MOUSEWHEEL events
    public class TrayIconNativeWindow : NativeWindow
    {
        public event MouseEventHandler MouseWheelEvent;

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg == WM_MOUSEWHEEL)
            {
                int delta = (short)((int)m.WParam >> 16);
                MouseEventArgs args = new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta);
                MouseWheelEvent?.Invoke(this, args);
            }
            base.WndProc(ref m);
        }
    }

    public class Measure
    {
        private API _api;
        private NotifyIcon _trayIcon;
        private TrayIconNativeWindow _nativeWindow; // For WM_MOUSEWHEEL events
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

        // Dispose only the native window and the icon, but keep the tray icon if possible
        public void Dispose()
        {
            if (_nativeWindow != null)
            {
                _nativeWindow.ReleaseHandle();
                _nativeWindow = null;
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
                API.Log(0, "Tray icon is disabled via configuration. No icon will be shown.");
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
                    API.Log(1, $"Error loading icon from path '{_iconPath}': {ex.Message}");
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

            // Hook into the underlying native window of the tray icon for WM_MOUSEWHEEL events (only attach once)
            AttachNativeWindow();
        }

        // Attach mouse event handlers if not already attached
        private void AttachMouseEventHandlers()
        {
            // Avoid attaching multiple times
            _trayIcon.MouseClick -= TrayIcon_MouseClick;
            _trayIcon.MouseDoubleClick -= TrayIcon_MouseDoubleClick;

            _trayIcon.MouseClick += TrayIcon_MouseClick;
            _trayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left && !string.IsNullOrEmpty(_leftClickAction))
            {
                _api.Execute(_leftClickAction);
            }
            else if (args.Button == MouseButtons.Middle && !string.IsNullOrEmpty(_middleClickAction))
            {
                _api.Execute(_middleClickAction);
            }
            else if (args.Button == MouseButtons.Right && _trayIcon.ContextMenuStrip == null && !string.IsNullOrEmpty(_rightClickAction))
            {
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

        // Attach native window for mouse wheel events if not already attached
        private void AttachNativeWindow()
        {
            if (_nativeWindow != null)
                return;

            try
            {
                FieldInfo windowField = typeof(NotifyIcon).GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
                if (windowField != null)
                {
                    NativeWindow window = windowField.GetValue(_trayIcon) as NativeWindow;
                    if (window != null)
                    {
                        _nativeWindow = new TrayIconNativeWindow();
                        _nativeWindow.AssignHandle(window.Handle);
                        _nativeWindow.MouseWheelEvent += (sender, e) =>
                        {
                            if (e.Delta > 0 && !string.IsNullOrEmpty(_mouseWheelUpAction))
                            {
                                _api.Execute(_mouseWheelUpAction);
                            }
                            else if (e.Delta < 0 && !string.IsNullOrEmpty(_mouseWheelDownAction))
                            {
                                _api.Execute(_mouseWheelDownAction);
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                API.Log(1, $"Error attaching to tray icon native window for mouse wheel events: {ex.Message}");
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
