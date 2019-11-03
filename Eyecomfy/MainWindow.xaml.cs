using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using MenuItem = System.Windows.Forms.MenuItem;
using Application = System.Windows.Application;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;

namespace Eyecomfy {
    public partial class MainWindow : Window {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        private const int pageUp_ID = 9000;
        private const int pageDown_ID = 9001;
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint VK_NEXT = 0x22; // page down virtual key code
        private const uint VK_PRIOR = 0x21; // page up virtual key code
        private HwndSource source;

        private readonly DimScreen dimScreen = new DimScreen();
        private readonly NotifyIcon notifyIcon;
        private const int closeDelay = 2000;
        private CancellationTokenSource hideTokenSource = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();

            // set initial position
            Left = SystemParameters.WorkArea.Width - (Width + 5);
            Top = SystemParameters.WorkArea.Height - Height;

            ShowInTaskbar = false;
            Topmost = true;

            notifyIcon = new NotifyIcon() {
                Icon = new System.Drawing.Icon("brightness.ico"),
                Visible = true
            };

            notifyIcon.MouseClick += NotifyIcon_MouseClick;
            CreateNotifyIConContexMenu();

            slider.Value = (double)Brightness; //set initial brightness
            slider.ValueChanged += Slider_ValueChanged;

            Deactivated += MainWindow_Deactivated;

            dimScreen.Show();
            Show();
        }

        public int Brightness {
            get {
                ManagementScope scope = new ManagementScope("root\\WMI");
                SelectQuery query = new SelectQuery("WmiMonitorBrightness");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query)) {
                    using (ManagementObjectCollection objectCollection = searcher.Get()) {
                        foreach (ManagementObject mObj in objectCollection) {
                            var br_obj = mObj.Properties["CurrentBrightness"].Value;
                            int br = 0;
                            int.TryParse(br_obj + "", out br);
                            return br;
                        }
                    }
                }
                return 0;
            }
            set {
                ManagementScope scope = new ManagementScope("root\\WMI");
                SelectQuery query = new SelectQuery("WmiMonitorBrightnessMethods");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query)) {
                   using (ManagementObjectCollection objectCollection = searcher.Get()) {
                       foreach (ManagementObject mObj in objectCollection) {
                           mObj.InvokeMethod("WmiSetBrightness",
                               new Object[] { UInt32.MaxValue, value });
                           break;
                       }
                   }
                }
            }
        }
        public bool RunAtStartup {
            get {
                string productName =
                    (string)RegistryKey.GetValue(System.Windows.Forms.Application.ProductName);

                if (productName.Equals(System.Windows.Forms.Application.ExecutablePath)) {
                    return true;
                }
                else {
                    return false;
                }
            }
            set {
                if (value == true) {
                    RegistryKey.SetValue(System.Windows.Forms.Application.ProductName,
                        System.Windows.Forms.Application.ExecutablePath);
                }
                else {
                    RegistryKey.DeleteValue(System.Windows.Forms.Application.ProductName, false);
                }
            }
        }
        private RegistryKey RegistryKey {
            get {
                return Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            }
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bool sliderValueBelowZero = slider.Value < 0;


            bool useBrightness = !sliderValueBelowZero;
            bool useAlpha = sliderValueBelowZero && slider.Value > -50;

            if (useBrightness) {
                Brightness = Convert.ToInt32(slider.Value);
            }
            else if (useAlpha) {
                double inverse = -slider.Value;
                byte alpha = (byte)inverse;
                alpha = (byte)(alpha * 2); // make alpha tick twise as big
                dimScreen.Background = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
            }
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                toggleFormVisible();
            }
        }
        private void toggleFormVisible()
        {
            if (Visibility == Visibility.Visible) {
                Visibility = Visibility.Collapsed;
            }
            else {
                Visibility = Visibility.Visible;
                Activate();
            }
        }
        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            var thread = new System.Threading.Thread(p => {
                Action action = () => {
                    Visibility = Visibility.Collapsed;
                };
                System.Threading.Thread.Sleep(100);
                this.Dispatcher.Invoke(action);
            });
            thread.Start();
        }
        private void CreateNotifyIConContexMenu()
        {
            var menu = new System.Windows.Forms.ContextMenu();

            menu.MenuItems.AddRange(new MenuItem[] {
                new MenuItem("Exit", (_,__) => { Application.Current.Shutdown(); }),
                new MenuItem("Run At Startup", (sender, _) => {
                    MenuItem sndr = (MenuItem)sender;
                    bool run = sndr.Checked ? false : true;
                    sndr.Checked = run;
                })
            });

            notifyIcon.ContextMenu = menu;
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);

            RegisterHotKey(handle, pageUp_ID, MOD_ALT, VK_PRIOR); // Alt+PageUp
            RegisterHotKey(handle, pageDown_ID, MOD_ALT, VK_NEXT); // Alt+PageDown
        }
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            int vkey = (((int)lParam >> 16) & 0xFFFF);

            switch (msg) {
                case WM_HOTKEY:
                    switch (wParam.ToInt32()) {
                        case pageUp_ID:
                            if (vkey == VK_PRIOR) {
                                Show();
                                slider.Value += slider.LargeChange;
                                hideTokenSource.Cancel();
                                hideTokenSource.Dispose();
                                hideTokenSource = new CancellationTokenSource();
                                closeAfterDelay(closeDelay, hideTokenSource.Token);
                            }
                            handled = true;
                            break;
                        case pageDown_ID:
                            if (vkey == VK_NEXT) {
                                Show();
                                slider.Value -= slider.LargeChange;
                                hideTokenSource.Cancel();
                                hideTokenSource.Dispose();
                                hideTokenSource = new CancellationTokenSource();
                                closeAfterDelay(closeDelay, hideTokenSource.Token);
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private async void closeAfterDelay(int delay, CancellationToken token)
        {
            await Task.Delay(delay);
            if (!token.IsCancellationRequested) {
                Hide();
            }
        }
    }
}
