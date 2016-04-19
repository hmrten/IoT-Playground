#include "pch.h"
#include "StartupTask.h"
#include <stdio.h>
#include <stdarg.h>
#include <stdint.h>

using namespace BackgroundApplication1;

using namespace Platform;
using namespace Windows::ApplicationModel::Background;
using namespace Windows::Devices::Enumeration;
using namespace Windows::Devices::I2c;
using namespace concurrency;

static void ods(const wchar_t *fmt, ...)
{
	wchar_t buf[512];
	va_list ap;

	va_start(ap, fmt);
	vswprintf_s(buf, fmt, ap);
	OutputDebugString(buf);
	va_end(ap);
}

static auto GetDeviceAsync(int addr, I2cBusSpeed speed = I2cBusSpeed::StandardMode, I2cSharingMode sharing = I2cSharingMode::Exclusive)
{
	auto aqs = I2cDevice::GetDeviceSelector();
	auto infoTask = create_task(DeviceInformation::FindAllAsync(aqs));
	auto deviceTask = infoTask.then([](DeviceInformationCollection^ dic) {
		return dic->GetAt(0)->Id;
	}).then([=](String ^devId) {
		ods(L"got devId: %s\n", devId->Data());
		auto settings = ref new I2cConnectionSettings(addr);
		settings->BusSpeed = speed;
		settings->SharingMode = sharing;
		return create_task(I2cDevice::FromIdAsync(devId, settings)).get();
	});
	return deviceTask;
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
	1, 1, 1, 1, 1, 1, 1, 1,
	1, 0, 0, 0, 0, 0, 0, 1,
	1, 0, 0, 0, 0, 0, 0, 1,
	1, 0, 0, 1, 1, 0, 0, 1,
	1, 0, 0, 1, 1, 0, 0, 1,
	1, 0, 0, 0, 0, 0, 0, 1,
	1, 0, 0, 0, 0, 0, 0, 1,
	1, 1, 1, 1, 1, 1, 1, 1,
};

void StartupTask::Run(IBackgroundTaskInstance^ taskInstance)
{
	auto deferral = taskInstance->GetDeferral();

	auto device = GetDeviceAsync(0x46).get();

	ods(L"device: %s\n", device->DeviceId);
	ods(L"writing to led...\n");

	auto data = ref new Array<byte>(1 + 192);
	data[0] = 0x00;
	int i = 1;
	for (int y = 0; y < 8; ++y)
	{
		int row = y << 3;
		for (int x = 0; x < 8; ++x)
		{
			char m = map[row + x];
			//char m = 1;
			byte r, g, b;
			if (m)
			{
				r = 0;
				g = 0;
				b = 31;
			}			else			{				r = g = b = 0;			}

			data[i + 0] = r;
			data[i + 8] = g;
			data[i + 16] = b;
			++i;
		}
		i += 16;
	}

	device->Write(data);
	ods(L"done...\n");

	ClearDisplay(device);

	deferral->Complete();
}
