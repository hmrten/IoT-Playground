using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ButtonInput
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int PIN_BUTTON = 6;

        private GpioController gpio;
        private GpioPin pin;
        private int hits;

        public MainPage()
        {
            this.InitializeComponent();

            gpio = GpioController.GetDefault();

            pin = gpio.OpenPin(PIN_BUTTON);

            // When button is connected to a PIN -> GND configure mode to InputPullUp
            pin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            
            // How often you can press the button
            pin.DebounceTimeout = TimeSpan.FromMilliseconds(1);

            // Callback to run when value changes
            pin.ValueChanged += Button_Changed;
        }

        private async void Button_Changed(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // When button closes the circuit
                if (args.Edge.CompareTo(GpioPinEdge.FallingEdge) == 0)
                {
                    ++hits;
                    textBlock.Text = "Hits: " + hits;
                }
            });
        }
    }
}
