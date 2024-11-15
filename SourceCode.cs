using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rainmeter;

namespace SkinTray
{
    public class Measure
    {
        private API _api;
        private NotifyIcon _trayIcon;
        private string _iconPath;
        private string _toolTipText;
        private string _leftClickAction;
        private string _rightClickAction;

        public Measure()
        {
            _trayIcon = new NotifyIcon();
        }

        // Reload the plugin (this is called when the skin is loaded or reloaded)
        public void Reload(API api, ref double maxValue)
        {
            _api = api;

            // Get the settings from Rainmeter config file
            _iconPath = _api.ReadString("Icon", "");  // Path to the icon file
            _toolTipText = _api.ReadString("ToolTipText", "Tray Icon Plugin");
            _leftClickAction = _api.ReadString("LeftMouseUpAction", "");
            _rightClickAction = _api.ReadString("RightMouseUpAction", "");

            // Initialize the tray icon
            _trayIcon.Icon = new System.Drawing.Icon(_iconPath);  // Set the tray icon
            _trayIcon.Visible = true;
            _trayIcon.Text = _toolTipText;  // Tooltip when hovering the tray icon

            // Define the actions for left and right mouse clicks
            _trayIcon.MouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Left)
                {
                    if (!string.IsNullOrEmpty(_leftClickAction))
                    {
                        _api.Execute(_leftClickAction);  // Execute the left click action
                    }
                }
                else if (args.Button == MouseButtons.Right)
                {
                    if (!string.IsNullOrEmpty(_rightClickAction))
                    {
                        _api.Execute(_rightClickAction);  // Execute the right click action
                    }
                }
            };

            // Optionally, add a context menu to the tray icon (if you want right-click options)
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show ToolTip", null, (sender, e) => MessageBox.Show(_toolTipText));
            _trayIcon.ContextMenuStrip = contextMenu;
        }

        // Update method (called periodically to refresh the skin)
        public double Update()
        {
            return 0.0;  // Returning 0 as this is a tray icon plugin with no specific numeric value
        }
    }

    public static class Plugin
    {
        // Initialize the plugin
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        // Finalize the plugin
        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();
        }

        // Reload the plugin (called when skin is reloaded)
        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new API(rm), ref maxValue);
        }

        // Update method (called periodically)
        [DllExport]
        public static double Update(IntPtr data)
        {
            return 0.0;
        }
    }
}
