#include "pch.h"
#include "StartupTask.h"
#include <stdio.h>
#include <stdarg.h>
#include <stdint.h>
#include <Windows.h>
#include <math.h>

using namespace BackgroundApplication1;

using namespace Platform;
using namespace Windows::ApplicationModel::Background;
using namespace Windows::Devices::Enumeration;
using namespace Windows::Devices::I2c;
using namespace concurrency;

#define ods(...) ods_f(__VA_ARGS__)

static void ods_f(const wchar_t *fmt, ...)
{
	wchar_t buf[512];
	va_list ap;

	va_start(ap, fmt);
	vswprintf_s(buf, fmt, ap);
	OutputDebugString(buf);
	va_end(ap);
}

static auto GetDevice(int addr, I2cBusSpeed speed = I2cBusSpeed::StandardMode, I2cSharingMode sharing = I2cSharingMode::Exclusive)
{
	auto settings = ref new I2cConnectionSettings(addr);
	settings->BusSpeed = speed;
	settings->SharingMode = sharing;
	auto aqs = I2cDevice::GetDeviceSelector();
	auto findAllTask = create_task(DeviceInformation::FindAllAsync(aqs));
	auto devId = findAllTask.get()->GetAt(0)->Id;
	ods(L"got devId: %s\n", devId->Data());

	auto getDeviceTask = create_task(I2cDevice::FromIdAsync(devId, settings));
	auto device = getDeviceTask.get();
	ods(L"got device: %s\n", device->ToString()->Data());
	return device;
}

static void ClearDisplay(I2cDevice^ device)
{
	static const auto zeroBytes = ref new Array<byte>(1 + 192);
	device->Write(zeroBytes);
}

static inline uint16_t rgb565(uint8_t r, uint8_t g, uint8_t b)
{
	return ((r & 31) << 11) | ((g & 63) << 5) | (b & 31);
}

static const char map[64] = {
	0, 0, 1, 1, 1, 1, 0, 0,
	0, 1, 1, 0, 0, 1, 1, 0,
	0, 1, 1, 0, 0, 1, 1, 0,
	0, 1, 1, 0, 0, 1, 1, 0,
	0, 1, 1, 1, 1, 1, 1, 0,
	0, 1, 1, 0, 0, 1, 1, 0,
	0, 1, 1, 0, 0, 1, 1, 0,
	0, 1, 1, 0, 0, 1, 1, 0,
};

static void Draw(I2cDevice^ device, int ticks)
{
	static int dir;
	static int yp;
	static int xp;

	auto data = ref new Array<byte>(1 + 192);

	auto s0 = sinf((float)ticks*1.1234f) + 1.0f;
	auto s1 = sinf((float)ticks*0.9876f) + 1.0f;
	auto s2 = sinf((float)ticks*1.3780f) + 1.0f;

	switch (dir) {
	case 0:
	C0:
		if (xp == 7) {
			dir = 2;
			goto C2;
		} else {
			++xp;
		}
		break;
	case 1:
	C1:
		if (xp == 0) {
			dir = 3;
			goto C3;
		} else {
			--xp;
		}
		break;
	case 2:
	C2:
		if (yp == 7) {
			dir = 1;
			goto C1;
		} else {
			++yp;
		}
		break;
	case 3:
	C3:
		if (yp == 0) {
			dir = 0;
			goto C0;
		} else {
			--yp;
		}
		break;
	}

	int idx = 1 + (yp * 24 + xp);

	//if (++xp == 8)
	//{
	//	xp = 0;
	//	if (++yp == 8)
	//		yp = 0;
	//}

	byte r = (byte)(s0*15.5f);
	byte g = (byte)(s1*31.5f);
	byte b = (byte)(s2*15.5f);

	data[idx + 0] = r;
	data[idx + 8] = g;
	data[idx + 16] = b;

	device->Write(data);
}

static void StartDemo(HANDLE timerDone, I2cDevice^ device)
{
	using namespace Windows::System::Threading;
	using namespace Windows::Foundation;

	ods(L"starting PeriodicTimer...\n");

	TimeSpan period;
	period.Duration = 10000000 / 16;
	int ticks = 0;
	auto tpt = ThreadPoolTimer::CreatePeriodicTimer(ref new TimerElapsedHandler([=](ThreadPoolTimer^ src) mutable {
		ods(L"tick: %d\n", ticks);

		Draw(device, ticks);

		if (++ticks == 1000) {
			SetEvent(timerDone);
			src->Cancel();
		}
	}), period);
}

void StartupTask::Run(IBackgroundTaskInstance^ taskInstance)
{
	HANDLE timerDone = CreateEventEx(nullptr, nullptr, 0, SYNCHRONIZE | EVENT_MODIFY_STATE);

	auto device = GetDevice(0x46);

	StartDemo(timerDone, device);

	ods(L"device: %s\n", device->DeviceId->Data());
	ods(L"writing to led...\n");

	auto data = ref new Array<byte>(1 + 192);
	data[0] = 0x00;
	int i = 1;
	for (int y = 0; y < 8; ++y) {
		int row = y << 3;
		for (int x = 0; x < 8; ++x) {
			char m = map[row + x];
			byte r, g, b;
			if (m) {
				r = 0;
				g = 63;
				b = 0;
			} else {
				r = g = b = 0;
			}

			data[i + 0] = r;
			data[i + 8] = g;
			data[i + 16] = b;
			++i;
		}
		i += 16;
	}

	device->Write(data);
	ods(L"done...\n");

	WaitForSingleObjectEx(timerDone, INFINITE, false);

	ClearDisplay(device);

	//deferral->Complete();
}
