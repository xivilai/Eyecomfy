using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Eyecomfy {
    /// <summary>
    /// Interaction logic for DimScreen.xaml
    /// </summary>
    public partial class DimScreen : Window {
        public DimScreen()
        {
            InitializeComponent();
        }

        [DllImport("gdi32.dll")]
        private unsafe static extern bool SetDeviceGammaRamp(Int32 hdc, ref RAMP lpRamp);
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);
        private static RAMP s_ramp = new RAMP();
        public static void SetGamma(int gamma)
        {
            s_ramp.Red = new ushort[256];
            s_ramp.Green = new ushort[256];
            s_ramp.Blue = new ushort[256];

            for (int i = 1; i < 256; i++) {
                // gamma is a value between 3 and 44
                s_ramp.Red[i] = s_ramp.Green[i] = s_ramp.Blue[i] = (ushort)(Math.Min(65535,
                Math.Max(0, Math.Pow((i + 1) / 256.0, gamma * 0.1) * 65535 + 0.5)));
            }

            SetDeviceGammaRamp(GetDC(IntPtr.Zero).ToInt32(), ref s_ramp);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct RAMP {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]

        public UInt16[] Red;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]

        public UInt16[] Green;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]

        public UInt16[] Blue;
    }
}
