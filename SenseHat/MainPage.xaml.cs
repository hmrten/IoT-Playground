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
    // Temperature & Humidity sensor
    public class HTS221 : IDisposable
    {
        // I2C Address
        public const byte C_Addr = 0x5F;
        public const byte C_RegId = 0x0F;
        public const byte C_Id = 0xBC;

        // Registers
        public const byte C_WhoAmI = 0x0F;
        public const byte C_AvConf = 0x10;
        public const byte C_Ctrl1 = 0x20;
        public const byte C_Ctrl2 = 0x21;
        public const byte C_Ctrl3 = 0x22;
        public const byte C_Status = 0x27;
        public const byte C_HumidityOutL = 0x28;
        public const byte C_HumidityOutH = 0x29;
        public const byte C_TempOutL = 0x2A;
        public const byte C_TempOutH = 0x2B;
        public const byte C_H0H2 = 0x30;
        public const byte C_H1H2 = 0x31;
        public const byte C_T0C8 = 0x32;
        public const byte C_T1C8 = 0x33;
        public const byte C_T1T0 = 0x35;
        public const byte C_H0T0Out = 0x36;
        public const byte C_H1T0Out = 0x3A;
        public const byte C_T0Out = 0x3C;
        public const byte C_T1Out = 0x3E;

        private I2cDevice device;
        private Func<Int16, float> convertTemperature;
        private Func<Int16, float> convertHumidity;

        private byte Read8(byte addr, string msg)
        {
            try
            {
                byte[] wb = { addr };
                var rb = new byte[1];
                device.WriteRead(wb, rb);
                return rb[0];
            }
            catch (Exception e)
            {
                throw new Exception(msg, e);
            }
        }

        private UInt16 Read16LE(byte addr, string msg)
        {
            try
            {
                byte[] wb = { addr };
                byte[] rb = new byte[2];
                device.WriteRead(wb, rb);
                return (UInt16)(((UInt16)rb[1] << 8) | (UInt16)rb[0]);
            }
            catch (Exception e)
            {
                throw new Exception(msg, e);
            }
        }

        private async Task GetDevice()
        {
            var aqs = I2cDevice.GetDeviceSelector();
            var infos = await DeviceInformation.FindAllAsync(aqs);
            var settings = new I2cConnectionSettings(C_Addr)
            {
                BusSpeed = I2cBusSpeed.FastMode
            };
            device = await I2cDevice.FromIdAsync(infos[0].Id, settings);
        }

        private Func<Int16, float> GetTemperatureConverter()
        {
            byte rawMsb = Read8(C_T1T0 + 0x80, "failed to read T1T0");

            byte t0Lsb = Read8(C_T0C8 + 0x80, "failed to read T0C8");
            byte t1Lsb = Read8(C_T1C8 + 0x80, "failed to read T1C8");

            UInt16 t0c8 = (UInt16)(((UInt16)(rawMsb & 0x03) << 8) | (UInt16)t0Lsb);
            UInt16 t1c8 = (UInt16)(((UInt16)(rawMsb & 0x0C) << 6) | (UInt16)t1Lsb);

            float t0 = t0c8 / 8.0f;
            float t1 = t1c8 / 8.0f;

            Int16 t0out = (Int16)Read16LE(C_T0Out + 0x80, "failed to read T0Out");
            Int16 t1out = (Int16)Read16LE(C_T1Out + 0x80, "failed to read T1Out");

            float m = (t1 - t0) / (t1out - t0out);
            float b = t0 - (m * t0out);

            return t => t * m + b;
        }

        private Func<Int16, float> GetHumidityConverter()
        {
            byte h0h2 = Read8(C_H0H2 + 0x80, "failed to read H0H2");
            byte h1h2 = Read8(C_H1H2 + 0x80, "failed to read H1H2");

            float h0 = h0h2 * 0.5f;
            float h1 = h1h2 * 0.5f;

            Int16 h0t0out = (Int16)Read16LE(C_H0T0Out + 0x80, "failed to read H0T0Out");
            Int16 h1t0out = (Int16)Read16LE(C_H1T0Out + 0x80, "failed to read H1T0Out");

            float m = (h1 - h0) / (h0t0out - h1t0out);
            float b = h0 - (m * h0t0out);

            return t => t * m + b;
        }

        public float ReadTemperature()
        {
            var status = Read8(C_Status, "failed to read Status");
            if ((status & 1) == 1)
            {
                var rawData = (Int16)Read16LE(C_TempOutL + 0x80, "failed to read TempOutL");
                var t = convertTemperature(rawData);
                return t;
            }
            return 0.0f;
        }

        public float ReadHumidity()
        {
            var status = Read8(C_Status, "failed to read Status");
            if ((status & 2) == 2)
            {
                var rawData = (Int16)Read16LE(C_HumidityOutL + 0x80, "failed to read HumidityOutL");
                var t = convertHumidity(rawData);
                return t;
            }
            return 0.0f;
        }

        public void Init()
        {
            Task.Run(async () =>
            {
                await GetDevice().ConfigureAwait(false);
            }).Wait();
            if (device == null)
            {
                throw new Exception("failed to get device");
            }

            byte[] data = new byte[2];
            data[0] = C_Ctrl1;
            data[1] = 0x87;
            device.Write(data);

            data[0] = C_AvConf;
            data[1] = 0x1B;
            device.Write(data);

            convertTemperature = GetTemperatureConverter();
            convertHumidity = GetHumidityConverter();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                device.Dispose();
                disposed = true;
            }
        }
    }

    public class LedMatrix : IDisposable
    {
        private const byte C_Addr = 0x46;

        private I2cDevice device;
        private byte[] zeroBytes = new byte[1 + 192];

        private static async Task<I2cDevice> GetDevice()
        {
            var aqs = I2cDevice.GetDeviceSelector();
            var infos = await DeviceInformation.FindAllAsync(aqs);
            var settings = new I2cConnectionSettings(C_Addr)
            {
                BusSpeed = I2cBusSpeed.StandardMode
            };
            return await I2cDevice.FromIdAsync(infos[0].Id, settings);
        }

        public void Init()
        {
            Task.Run(async () =>
            {
                Debug.WriteLine("getting device...");
                device = await GetDevice().ConfigureAwait(false);
            }).Wait();
        }

        public void Clear()
        {
            device.Write(zeroBytes);
        }

        public void Draw()
        {
            byte[] buf = new byte[1 + 192]; // buf[0] = address, 192 = 8x8 x 3 bytes per pixel
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

        //public void SetPixel(int x, int y)
        //{
        //    y * 24
        //}

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                device.Dispose();
                disposed = true;
            }
        }
    }

    public class SensorReader
    {
        private Queue<Tuple<float, float>> readings;
        private int capacity;
        private HTS221 hts221;
        private DispatcherTimer timer;
        private Action<SensorReader, float, float> onReading;

        public IEnumerable<Tuple<float, float>> Data
        {
            get
            {
                return readings;
            }
        }

        public IEnumerable<float> TemperatureData
        {
            get
            {
                return readings.Select(r => r.Item1);
            }
        }

        public IEnumerable<float> HumidityData
        {
            get
            {
                return readings.Select(r => r.Item2);
            }
        }

        public SensorReader(int ms, int capacity = 100, Action<SensorReader, float, float> onReading = null)
        {
            readings = new Queue<Tuple<float, float>>(capacity);
            this.capacity = capacity;

            hts221 = new HTS221();
            hts221.Init();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(ms);
            timer.Tick += Timer_Tick;
            timer.Start();

            this.onReading = onReading;
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void Timer_Tick(object sender, object e)
        {
            float c = hts221.ReadTemperature();
            float h = hts221.ReadHumidity();

            var pair = new Tuple<float, float>(c, h);

            if (readings.Count < capacity)
            {
                readings.Enqueue(pair);
            }
            else
            {
                readings.Dequeue();
                readings.Enqueue(pair);
            }

            onReading?.Invoke(this, c, h);
        }
    }

    public sealed partial class MainPage : Page
    {
        private SensorReader sensorReader;

        private void Demo_LedMatrix()
        {
            using (var led = new LedMatrix())
            {
                led.Init();
                led.Draw();
                led.Clear();
            }
        }

        private void Demo_HTS221()
        {
            using (var hts221 = new HTS221())
            {
                hts221.Init();
                float c = hts221.ReadTemperature();
                float h = hts221.ReadHumidity();
                Debug.WriteLine("temp: {0} celcius\nhumidity: {1} %", c, h);
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            sensorReader = new SensorReader(100, 10, (sr, c, h) =>
            {
                Debug.WriteLine("reading: <{0}, {1}>", c, h);
                listBox.ItemsSource = sr.Data.Select(x => String.Format("Temp: {0:F2} C, Humidity: {1:F2} %", x.Item1, x.Item2));
            });

            //Demo_HTS221();
            //Demo_LedMatrix();
        }
    }
}
