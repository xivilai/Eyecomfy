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

namespace BrightnessSlider {
    public partial class MainWindow : Window {
        public MainWindow()
        {
            InitializeComponent();

            // set initial position
            Left = SystemParameters.WorkArea.Width - (Width + 5);
            Top = SystemParameters.WorkArea.Height - Height;

            ShowInTaskbar = false;
            Topmost = true;

            //notifyIcon1.MouseClick += NotifyIcon1_MouseClick;
            //Deactivate += Form1_Deactivate;

            //CreateNotifyIConContexMenu();

            //set initial brightness
            slider.Value = (double)Brightness;

            slider.ValueChanged += Slider_ValueChanged;
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
