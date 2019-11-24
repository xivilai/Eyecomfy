using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Win32Utils;

namespace Eyecomfy {
    public partial class DimScreen : Window {

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private static int SW_SHOWNA = 8;

        DispatcherTimer alwaysOnTopTimer = new DispatcherTimer();
        public DimScreen()
        {
            InitializeComponent();

            alwaysOnTopTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            alwaysOnTopTimer.Tick += AlwaysOnTopTimer_Tick;
            alwaysOnTopTimer.Start();
        }

        private void AlwaysOnTopTimer_Tick(object sender, EventArgs e)
        {
            var foregroundWindowHandle = GetForegroundWindow();
            IntPtr thisWindowHandle = new WindowInteropHelper(this).Handle;

            bool thisWindowIsTopmost = thisWindowHandle.ToInt32() == foregroundWindowHandle.ToInt32();

            if (!thisWindowIsTopmost) {
                ShowWindow(thisWindowHandle, SW_SHOWNA);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);

            // Hide app from taskbar (ShowInTaskbar property does not work for "Alt+Tab" menu)
            SetWindowLong(hwnd, GWL_EX_STYLE, (GetWindowLong(hwnd, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        }
    }
}
