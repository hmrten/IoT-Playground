using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Gpio;
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

namespace ButtonLED
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int PIN_LED = 5;

        private GpioController gpio;
        private GpioPin pin;
        private GpioPinValue pinValue;

        private void UpdateButtonText()
        {
            if (pinValue == GpioPinValue.High)
                button.Content = "LED ON";
            else
                button.Content = "LED OFF";
        }

        public MainPage()
        {
            this.InitializeComponent();

            gpio = GpioController.GetDefault();

            pin = gpio.OpenPin(PIN_LED);

            // Set pin to a known state (high == led off)
            // and drive mode to output
            pin.Write(GpioPinValue.High);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            pinValue = GpioPinValue.High;
            UpdateButtonText();
        }

        // Toggle pin state between low and high
        private void button_Click(object sender, RoutedEventArgs e)
        {
            pinValue = pinValue == GpioPinValue.High ? GpioPinValue.Low : GpioPinValue.High;
            pin.Write(pinValue);
            UpdateButtonText();
        }
    }
}
