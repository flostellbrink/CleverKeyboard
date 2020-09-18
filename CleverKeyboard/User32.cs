using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace CleverKeyboard
{
	public static class User32
	{
		private const uint BsfPostmessage = 0x00000010;
		private const uint BsmApplications = 0x00000008;
		public const uint RidevInputSink = 0x00000100;
		private const uint RidiDeviceName = 0x20000007;
		public const uint RidHeader = 0x10000005;
		public const uint RimTypeKeyboard = 1;
		private const uint SpifSendChange = 2;
		private const uint SpiSetDefaultInputLang = 90;
		public const uint WmInput = 0x00FF;
		private const uint WmInputLangChangeRequest = 0x0050;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterRawInputDevices(
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
			RawInputDevice[] pRawInputDevices,
			int uiNumDevices,
			int cbSize
		);

		public static bool RegisterRawInputDevices(params RawInputDevice[] rawInputDevices)
		{
			return RegisterRawInputDevices(
				rawInputDevices,
				rawInputDevices.Length,
				Marshal.SizeOf(typeof(RawInputDevice))
			);
		}

		[DllImport("user32.dll")]
		private static extern int GetRawInputDeviceList([Out] RawInputDeviceList[] pRawInputDeviceList,
			ref uint puiNumDevices,
			int cbSize);

		public static RawInputDeviceList[] GetRawInputDeviceList()
		{
			uint nDevices = 0;

			var res = GetRawInputDeviceList(null, ref nDevices, Marshal.SizeOf(typeof(RawInputDeviceList)));
			Debug.Assert(res == 0);

			var deviceList = new RawInputDeviceList[nDevices];

			var size = nDevices * (uint) Marshal.SizeOf(typeof(RawInputDeviceList));
			res = GetRawInputDeviceList(deviceList, ref size, Marshal.SizeOf(typeof(RawInputDeviceList)));
			Debug.Assert(res == nDevices);
			return deviceList;
		}

		[DllImport("user32.dll")]
		private static extern int GetRawInputDeviceInfo(IntPtr deviceHandle,
			uint command,
			[Out] StringBuilder data,
			ref uint dataSize);

		public static string GetRawInputDeviceName(IntPtr deviceHandle)
		{
			uint dataSize = 0;
			var res = GetRawInputDeviceInfo(deviceHandle, RidiDeviceName, null, ref dataSize);
			Debug.Assert(res == 0);
			Debug.Assert(dataSize > 0);

			var buffer = new StringBuilder((int) dataSize);
			res = GetRawInputDeviceInfo(deviceHandle, RidiDeviceName, buffer, ref dataSize);
			Debug.Assert(res > 0);

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

		public static uint GetRawInputData(IntPtr hRawInput, uint uiCommand, out RawInputHeader data)
		{
			var size = Marshal.SizeOf(typeof(RawInputHeader));
			var buffer = Marshal.AllocHGlobal(size);
			try
			{
				var result = GetRawInputData(
					hRawInput,
					uiCommand,
					buffer,
					ref size,
					size
				);

				data = new RawInputHeader();
				Marshal.PtrToStructure(buffer, data);
				return result;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		[DllImport("user32", SetLastError = true)]
		private static extern int BroadcastSystemMessage(uint dwFlags,
			ref uint lpdwRecipients,
			uint uiMessage,
			IntPtr wParam,
			IntPtr lParam);

		public static void SetCurrentLayout(IntPtr layoutId) {
			var recipients = BsmApplications;
			BroadcastSystemMessage(BsfPostmessage, ref recipients, WmInputLangChangeRequest, IntPtr.Zero, layoutId);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

		[DllImport("user32.dll")]
		private static extern uint GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);

		[DllImport("user32.dll")]
		private static extern bool GetKeyboardLayoutNameW([MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern IntPtr GetKeyboardLayout(uint thread);

		public static IntPtr[] GetKeyboardLayoutList()
		{
			var count = (int) GetKeyboardLayoutList(0, null);
			Debug.Assert(count > 0);

			var localeHandles = new IntPtr[count];
			var realCount = (int) GetKeyboardLayoutList(count, localeHandles);
			Debug.Assert(realCount == count);
			return localeHandles;
		}

		public static string GetKeyboardLayoutName(IntPtr keyboardLayout)
		{
			return $"{GetLanguageName(keyboardLayout)} / {GetKeyboardName(keyboardLayout)}";
		}

		private static string GetLanguageName(IntPtr keyboardLayout)
		{
			var langId = (ushort) keyboardLayout.ToInt32();

			var langName = CultureInfo.GetCultureInfo(langId).DisplayName;

			return langName;
		}

		private static string GetKeyboardName(IntPtr keyboardLayout)
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

		public static IntPtr GetKeyboardLayout()
		{
			var winThreadProcId = GetWindowThreadProcessId(GetForegroundWindow(), 0);
			return GetKeyboardLayout((ushort) winThreadProcId);
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr LoadKeyboardLayout([MarshalAs(UnmanagedType.LPTStr)] string pwszKLID, uint flags);

		public static IntPtr LoadKeyboardLayout(ushort layout, uint flags)
		{
			return LoadKeyboardLayout(string.Format("{0:X04}{0:X04}", layout), flags);
		}

		[DllImport("user32.dll")]
		private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr[] pvParam, uint fWinIni);

		public static void SetDefaultLayout(IntPtr layoutId) {
			SystemParametersInfo(SpiSetDefaultInputLang, 0, new[] { layoutId }, SpifSendChange);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RawInputDevice
		{
			private readonly ushort usUsagePage;
			private readonly ushort usUsage;
			private readonly uint dwFlags;
			private readonly IntPtr hwndTarget;

			public RawInputDevice(ushort usUsagePage, ushort usUsage, uint dwFlags, IntPtr hwndTarget)
			{
				this.usUsagePage = usUsagePage;
				this.usUsage = usUsage;
				this.dwFlags = dwFlags;
				this.hwndTarget = hwndTarget;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RawInputDeviceList
		{
			public IntPtr hDevice;
			public int dwType;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class RawInputHeader
		{
			public uint dwSize;
			public uint dwType;
			public IntPtr hDevice;
			public IntPtr wParam;
		}
	}
}
