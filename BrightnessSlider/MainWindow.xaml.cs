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

namespace BrightnessSlider {
    public partial class MainWindow : Window {
        private readonly NotifyIcon notifyIcon;
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
                Visible = true,
                BalloonTipText = "Hi there, i am your brightness pal!",
                BalloonTipTitle = "Brightness slider:",
                BalloonTipIcon = ToolTipIcon.Info,
            };
            
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
            CreateNotifyIConContexMenu();
            notifyIcon.ShowBalloonTip(2000);
            
            slider.Value = (double)Brightness; //set initial brightness
            slider.ValueChanged += Slider_ValueChanged;

            Deactivated += MainWindow_Deactivated;
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

        private RegistryKey RegistryKey {
            get {
                return Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
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

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Brightness = Convert.ToInt32(slider.Value);
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
    }
}
