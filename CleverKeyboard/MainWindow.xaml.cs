﻿using System;
using System.Windows;
using System.Windows.Interop;

namespace CleverKeyboard
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			DataContext = ViewModel;
			InitializeComponent();
			RegisterInputSink();
		}

		private MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

		/// <summary>
		/// Registers a raw input device to listen to all inputs.
		/// </summary>
		private void RegisterInputSink()
		{
			var handle = new WindowInteropHelper(this).EnsureHandle();
			var device = new User32.RAWINPUTDEVICE(1, 6, User32.RIDEV_INPUTSINK, handle);
			if (!User32.RegisterRawInputDevices(device))
				throw new Exception("Failed to register input sink.");

			HwndSource.FromHwnd(handle)?.AddHook(WndProc);

			IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				if (msg != User32.WM_INPUT) return IntPtr.Zero;

				var result = User32.GetRawInputData(lParam, User32.RID_HEADER, out var header);
				if (result == uint.MaxValue) throw new Exception("Could not read input data.");

				ViewModel.MakeKeyboardActive(header.hDevice);

				return IntPtr.Zero;
			}
		}
	}
}