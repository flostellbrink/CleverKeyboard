using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

#pragma warning disable 649

namespace CleverKeyboard
{
	public static class User32
	{
		public const uint WmInput = 0x00FF;

		private const uint BsfPostmessage = 0x00000010;
		private const uint BsmApplications = 0x00000008;
		private const uint RidHeader = 0x10000005;
		private const uint RidevInputSink = 0x00000100;
		private const uint RidiDeviceName = 0x20000007;
		private const uint RimTypeKeyboard = 1;
		private const uint SpiSetDefaultInputLang = 90;
		private const uint SpifSendChange = 2;
		private const uint WmInputLangChangeRequest = 0x0050;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterRawInputDevices(
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
			RawInputDevice[] pRawInputDevices,
			int uiNumDevices,
			int cbSize
		);

		private static bool RegisterRawInputDevices(params RawInputDevice[] devices)
		{
			return RegisterRawInputDevices(devices, devices.Length, Marshal.SizeOf(typeof(RawInputDevice)));
		}

		public static bool RegisterInputSink(IntPtr handle)
		{
			return RegisterRawInputDevices(new RawInputDevice
				{ usUsagePage = 1, usUsage = 6, dwFlags = RidevInputSink, hwndTarget = handle });
		}

		[DllImport("user32.dll")]
		private static extern int GetRawInputDeviceList(
			[Out] RawInputDeviceList[] pRawInputDeviceList,
			ref uint puiNumDevices,
			int cbSize);

		public static IEnumerable<IntPtr> GetKeyboards()
		{
			uint nDevices = 0;
			{
				var result = GetRawInputDeviceList(null, ref nDevices, Marshal.SizeOf(typeof(RawInputDeviceList)));
				if (result != 0) throw new Exception("Failed to get input device list count.");
			}

			var deviceList = new RawInputDeviceList[nDevices];
			{
				var size = nDevices * (uint) Marshal.SizeOf(typeof(RawInputDeviceList));
				var result = GetRawInputDeviceList(deviceList, ref size, Marshal.SizeOf(typeof(RawInputDeviceList)));
				if (result != nDevices) throw new Exception("Failed to get input devices.");
			}

			return deviceList.Where(device => device.dwType == RimTypeKeyboard).Select(device => device.hDevice);
		}

		[DllImport("user32.dll")]
		private static extern int GetRawInputDeviceInfo(
			IntPtr deviceHandle,
			uint command,
			[Out] StringBuilder data,
			ref uint dataSize);

		public static string GetRawInputDeviceName(IntPtr deviceHandle)
		{
			uint dataSize = 0;
			{
				var result = GetRawInputDeviceInfo(deviceHandle, RidiDeviceName, null, ref dataSize);
				if (result != 0) throw new Exception("Failed to get device name size");
			}

			var buffer = new StringBuilder((int) dataSize);
			{
				var result = GetRawInputDeviceInfo(deviceHandle, RidiDeviceName, buffer, ref dataSize);
				if (result <= 0) throw new Exception("Failed to read device name");
			}

			return buffer.ToString();
		}

		[DllImport("user32.dll", SetLastError = false)]
		private static extern uint GetRawInputData(
			IntPtr hRawInput,
			uint uiCommand,
			IntPtr pData,
			ref int pcbSize,
			int cbSizeHeader
		);

		public static IntPtr GetRawInputDevice(IntPtr hRawInput)
		{
			var size = Marshal.SizeOf(typeof(RawInputHeader));
			var buffer = Marshal.AllocHGlobal(size);
			try
			{
				if (GetRawInputData(hRawInput, RidHeader, buffer, ref size, size) == uint.MaxValue)
					throw new Exception("Failed to get input header");
				var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
				if (header == null) throw new Exception("Failed to parse input header");
				return header.hDevice;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		[DllImport("user32", SetLastError = true)]
		private static extern int BroadcastSystemMessage(
			uint dwFlags,
			ref uint lpdwRecipients,
			uint uiMessage,
			IntPtr wParam,
			IntPtr lParam);

		public static void SetCurrentLayout(IntPtr layoutId)
		{
			var recipients = BsmApplications;
			BroadcastSystemMessage(BsfPostmessage, ref recipients, WmInputLangChangeRequest, IntPtr.Zero, layoutId);
		}

		[DllImport("user32.dll")]
		private static extern uint GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);

		public static IEnumerable<IntPtr> GetKeyboardLayouts()
		{
			var count = (int) GetKeyboardLayoutList(0, null);
			Debug.Assert(count > 0);

			var localeHandles = new IntPtr[count];
			var realCount = (int) GetKeyboardLayoutList(count, localeHandles);
			Debug.Assert(realCount == count);
			return localeHandles;
		}

		[DllImport("user32.dll")]
		private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern IntPtr GetKeyboardLayout(uint thread);

		public static IntPtr GetKeyboardLayout()
		{
			return GetKeyboardLayout((ushort) GetWindowThreadProcessId(GetForegroundWindow(), 0));
		}

		[DllImport("user32.dll")]
		private static extern bool GetKeyboardLayoutNameW([MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer);

		public static string GetKeyboardName(IntPtr keyboardLayout)
		{
			var currentLayout = GetKeyboardLayout();
			if (currentLayout != keyboardLayout) ActivateKeyboardLayout(keyboardLayout, 0);

			var buffer = new StringBuilder();
			GetKeyboardLayoutNameW(buffer);

			if (currentLayout != keyboardLayout) ActivateKeyboardLayout(currentLayout, 0);

			var nameId = buffer.ToString().ToLowerInvariant();
			var regKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\ControlSet001\Control\Keyboard Layouts\{nameId}");
			var name = regKey?.GetValue("Layout Text");

			return name as string;
		}

		[DllImport("user32.dll")]
		private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr[] pvParam, uint fWinIni);

		public static void SetDefaultLayout(IntPtr layoutId)
		{
			SystemParametersInfo(SpiSetDefaultInputLang, 0, new[] { layoutId }, SpifSendChange);
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RawInputDevice
		{
			public ushort usUsagePage;
			public ushort usUsage;
			public uint dwFlags;
			public IntPtr hwndTarget;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RawInputDeviceList
		{
			public readonly IntPtr hDevice;
			public readonly int dwType;
		}

		[StructLayout(LayoutKind.Sequential)]
		private class RawInputHeader
		{
			public uint dwSize;
			public uint dwType;
			public IntPtr hDevice;
			public IntPtr wParam;
		}
	}
}
