//
// MainPage.xaml.h
// Declaration of the MainPage class.
//

#pragma once

#include "MainPage.g.h"

namespace NativeApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public ref class MainPage sealed
	{
		static const int TICKRATE = 25;

		Windows::UI::Xaml::Media::Imaging::WriteableBitmap^ screen;
		Windows::UI::Xaml::DispatcherTimer^ timer;
		LARGE_INTEGER qpcFreq;

	public:
		MainPage();

		void OnKeyDown(Windows::UI::Core::CoreWindow ^sender, Windows::UI::Core::KeyEventArgs ^args);
		void OnTick(Platform::Object ^sender, Platform::Object ^args);
	};
}
