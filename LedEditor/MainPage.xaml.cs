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
            Task.Run(async () =>
            {
                device = await GetDeviceAsync().ConfigureAwait(false);
            }).Wait(5000);
            if (device == null)
            {
                throw new Exception("failed to get device");
            }

            // Clear display
            var data = new byte[1 + 192];
            device.Write(data);
        }

        public void SetPixel(int x, int y, byte val = 15)
        {
            int i = 1 + y * 24 + x;
            buffer[i + 0] = val;
            buffer[i + 8] = val;
            buffer[i + 16] = val;
        }

        public void Flush()
        {
            device.Write(buffer);
        }
    }

    public sealed partial class MainPage : Page
    {
        private LedMatrix ledMatrix;
        private bool[] ledStatus = new bool[8 * 8];
        private Brush defBrush;
        private Brush greenBrush;

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
                ledMatrix.SetPixel(x, y);
                ledStatus[i] = true;
                btn.Background = greenBrush;
            }
            else
            {
                ledMatrix.SetPixel(x, y, 0);
                ledStatus[i] = false;
                btn.Background = defBrush;
            }
            
            ledMatrix.Flush();
        }
    }
}
