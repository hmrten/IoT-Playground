using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
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

namespace SenseHat
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const byte DeviceAddress = 0x46;
        private I2cDevice device;
        private byte[] zeroBytes = new byte[1 + 192];
        private byte[] buf = new byte[1 + 192]; // buf[0] = address, 192 = 8x8 x 3 bytes per pixel

        private static async Task<I2cDevice> GetDevice()
        {
            var aqs = I2cDevice.GetDeviceSelector();
            var infos = await DeviceInformation.FindAllAsync(aqs);
            var settings = new I2cConnectionSettings(DeviceAddress)
            {
                BusSpeed = I2cBusSpeed.StandardMode
            };
            return await I2cDevice.FromIdAsync(infos[0].Id, settings);
        }

        private void SetupDisplay()
        {
            Task.Run(async () =>
            {
                Debug.WriteLine("getting device...");
                device = await GetDevice().ConfigureAwait(false);
            }).Wait();
        }

        private void ClearDisplay()
        {
            device.Write(zeroBytes);
        }

        private void Draw()
        {
            buf[0] = 0x00;
            int i = 1;
            for (int y = 0; y < 8; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    byte r = 0; // 5 bits
                    byte g = 0; // 6 bits
                    byte b = 31; // 5 bits

                    buf[i + 0] = r;
                    buf[i + 8] = g;
                    buf[i + 16] = b;

                    ++i;
                }
                i += 16;
            }

            device.Write(buf);
        }

        public MainPage()
        {
            this.InitializeComponent();

            try
            {
                SetupDisplay();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            Debug.WriteLine("device: {0}", device.DeviceId);

            Draw();

            ClearDisplay();

            device.Dispose();
        }
    }
}
