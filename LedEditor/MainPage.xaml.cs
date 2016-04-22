using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LedEditor
{
    public class LedMatrix
    {
        private I2cDevice device;
        private byte[] buffer = new byte[1 + 192];

        private async Task<I2cDevice> GetDeviceAsync()
        {
            var aqs = I2cDevice.GetDeviceSelector();
            var infos = await DeviceInformation.FindAllAsync(aqs);
            var settings = new I2cConnectionSettings(0x46)
            {
                BusSpeed = I2cBusSpeed.StandardMode
            };
            return await I2cDevice.FromIdAsync(infos[0].Id, settings);
        }

        public void Init()
        {
            try
            {
                Task.Run(async () =>
                {
                    device = await GetDeviceAsync().ConfigureAwait(false);
                }).Wait(5000);
            }
            catch (Exception e)
            {

            }
            //if (device == null)
            //{
            //    throw new Exception("failed to get device");
            //}

            // Clear display
            var data = new byte[1 + 192];
            device?.Write(data);
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            int i = 1 + y * 24 + x;
            buffer[i + 0] = r;
            buffer[i + 8] = g;
            buffer[i + 16] = b;
        }

        public void Flush()
        {
            device?.Write(buffer);
        }
    }

    public sealed partial class MainPage : Page
    {
        private LedMatrix ledMatrix;
        private bool[] ledStatus = new bool[8 * 8];
        private Brush defBrush;
        private Brush greenBrush;
        private byte r5 = 31, g6 = 63, b5 = 31;

        private void InitButtons()
        {
            for (int i = 0; i < 64; ++i)
            {
                int y = i >> 3;
                int x = i & 7;
                string btnName = "btn" + y + x;
                var btn = FindName(btnName) as Button;
                btn.Content = i;
                btn.Click += btn_Click;
            }

            defBrush = (FindName("btn00") as Button).Background;
            greenBrush = new SolidColorBrush(new Windows.UI.Color { A=255, G = 128 });
        }

        public MainPage()
        {
            this.InitializeComponent();

            ledMatrix = new LedMatrix();
            ledMatrix.Init();

            InitButtons();
        }

        private void btn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int i = (int)btn.Content;
            int y = i >> 3;
            int x = i & 7;

            if (!ledStatus[i])
            {
                ledMatrix.SetPixel(x, y, r5, g6, b5);
                ledStatus[i] = true;
                btn.Background = greenBrush;
            }
            else
            {
                ledMatrix.SetPixel(x, y, 0, 0, 0);
                ledStatus[i] = false;
                btn.Background = defBrush;
            }
            
            ledMatrix.Flush();
        }

        private void UpdateColorPreview()
        {
            var colorPreview = FindName("colorPreview") as Rectangle;
            if (colorPreview != null)
            {
                byte r = (byte)((r5 * 255) >> 5);
                byte g = (byte)((g6 * 255) >> 6);
                byte b = (byte)((b5 * 255) >> 5);
                colorPreview.Fill = new SolidColorBrush(new Windows.UI.Color { R = r, G = g, B = b, A = 255 });
            }
        }

        private void sliderRed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            r5 = (byte)e.NewValue;
            UpdateColorPreview();
        }

        private void sliderGreen_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            g6 = (byte)e.NewValue;
            UpdateColorPreview();
        }

        private void sliderBlue_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            b5 = (byte)e.NewValue;
            UpdateColorPreview();
        }
    }
}
