using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Demo
{
    //public class HiResClock
    //{
    //    private Int64 freq = 0;
    //    private double invFreq = 0.0;
    //    private Int64 startTicks;

    //    [DllImport("coredll.dll")]
    //    private static extern int QueryPerformanceFrequency(ref Int64 freq);
    //    [DllImport("coredll.dll")]
    //    private static extern int QueryPerformanceCounter(ref Int64 ticks);

    //    public HiResClock()
    //    {
    //        QueryPerformanceFrequency(ref freq);
    //        invFreq = 1.0 / (double)freq;
    //    }

    //    public Int64 Freq { get { return freq; } }
    //    public double InvFreq { get { return invFreq; } }

    //    public Int64 Ticks
    //    {
    //        get
    //        {
    //            Int64 ticks = 0;
    //            QueryPerformanceCounter(ref ticks);
    //            return ticks;
    //        }
    //    }
    //}

    public sealed partial class MainPage : Page
    {
        const int pitch = 320 * 4;

        private DispatcherTimer timer;
        private WriteableBitmap screen;
        private byte[] screenPixels;
        //private Stream screenStream;
        private Random rnd;
        private Stopwatch clock = new Stopwatch();
        private int reportTimeout = 25;

        public MainPage()
        {
            this.InitializeComponent();

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            timer.Tick += Timer_Tick;

            screen = new WriteableBitmap(320, 200);
            screenPixels = new byte[pitch * 200];
            //screenStream = screen.PixelBuffer.AsStream();
            rnd = new Random();
            screenImage.Source = screen;

            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            //var startTicks = clock.Ticks;
            clock.Reset();
            clock.Start();

            using (var stream = screen.PixelBuffer.AsStream())
            {
                for (int y = 0; y < 200; ++y)
                {
                    int row = y * pitch;
                    for (int x = 0; x < pitch; x += 4)
                    {
                        int c = rnd.Next();
                        byte r = (byte)((c >> 16) & 255);
                        byte g = (byte)((c >> 8) & 255); ;
                        byte b = (byte)(c & 255); ;

                        screenPixels[row + x + 0] = b;
                        screenPixels[row + x + 1] = g;
                        screenPixels[row + x + 2] = r;
                        screenPixels[row + x + 3] = 0x00;
                    }
                }
                stream.Write(screenPixels, 0, screenPixels.Length);
                screen.Invalidate();
                screenImage.Source = screen;
            }
            clock.Stop();
            var elapsedMs = clock.ElapsedMilliseconds;
            if (--reportTimeout == 0)
            {
                Debug.WriteLine("{0} ms", elapsedMs);
                reportTimeout = 25;
            }
        }
    }
}
