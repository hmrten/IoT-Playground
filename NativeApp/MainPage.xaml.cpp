//
// MainPage.xaml.cpp
// Implementation of the MainPage class.
//

#include "pch.h"
#include "MainPage.xaml.h"
#include <wrl.h>
#include <robuffer.h>
#include <stdarg.h>

using namespace NativeApp;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::System;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Windows::UI::Xaml::Navigation;
using namespace Windows::Storage::Streams;
using namespace Microsoft::WRL;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

extern "C" int test();

static uint32_t rnd()
{
	static uint32_t seed = 7;
	seed ^= seed << 13;
	seed ^= seed >> 17;
	seed ^= seed << 5;
	return seed;
}

static byte *pixels = nullptr;

static void ods(const char *fmt, ...)
{
	char buf[512];
	va_list ap;
	
	va_start(ap, fmt);
	vsprintf_s(buf, fmt, ap);
	OutputDebugStringA(buf);
	va_end(ap);
}

MainPage::MainPage()
{
	InitializeComponent();

	QueryPerformanceFrequency(&qpcFreq);

	auto cw = Window::Current->CoreWindow;
	cw->KeyDown += ref new Windows::Foundation::TypedEventHandler<Windows::UI::Core::CoreWindow ^, Windows::UI::Core::KeyEventArgs ^>(this, &NativeApp::MainPage::OnKeyDown);

	screen = ref new WriteableBitmap((int)imgScreen->Width, (int)imgScreen->Height);
	timer = ref new DispatcherTimer();
	TimeSpan ts;
	ts.Duration = 10000000LL / TICKRATE;
	timer->Interval = ts;
	timer->Tick += ref new Windows::Foundation::EventHandler<Platform::Object ^>(this, &NativeApp::MainPage::OnTick);

	ComPtr<IBufferByteAccess> bytes;
	reinterpret_cast<IInspectable*>(screen->PixelBuffer)->QueryInterface(IID_PPV_ARGS(&bytes));
	bytes->Buffer(&pixels);

	imgScreen->Source = screen;

	timer->Start();

	int x = test();
	ods("test: %d", x);
}


void NativeApp::MainPage::OnKeyDown(Windows::UI::Core::CoreWindow ^sender, Windows::UI::Core::KeyEventArgs ^args)
{
	if (args->VirtualKey == VirtualKey::Escape) {
		Application::Current->Exit();
	}
}

void NativeApp::MainPage::OnTick(Platform::Object ^sender, Platform::Object ^args)
{
	static int interval = TICKRATE;

	const int width = screen->PixelWidth;
	const int height = screen->PixelHeight;

	LARGE_INTEGER t0, t;

	QueryPerformanceCounter(&t0);

	for (int y = 0; y < height; ++y) {
		uint32_t *p = (uint32_t *)pixels + y * width;
		for (int x = 0; x < width; ++x) {
			p[x] = rnd();
		}
	}
	screen->Invalidate();
	QueryPerformanceCounter(&t);
	if (--interval == 0) {
		int ms = (int)(((t.QuadPart - t0.QuadPart) * 1000LL) / qpcFreq.QuadPart);
		ods("%d ms\n", ms);
		interval = TICKRATE;
	}
}
