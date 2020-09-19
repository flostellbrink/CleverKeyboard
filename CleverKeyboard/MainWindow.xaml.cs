using System;
using System.Linq;
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
			if (!ViewModel.FirstRun) Hide();

			InitializeComponent();
			RegisterInputSink();

			Closing += (sender, args) =>
			{
				Hide();
				args.Cancel = true;
			};
		}

		private MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

		/// <summary>
		/// Registers a raw input device to listen to all inputs.
		/// </summary>
		private void RegisterInputSink()
		{
			var handle = new WindowInteropHelper(this).EnsureHandle();
			if (!User32.RegisterInputSink(handle)) throw new Exception("Failed to register input sink.");

			var source = HwndSource.FromHwnd(handle);
			if (source == null) throw new Exception("Failed to create hwnd source");
			source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
			{
				if (msg == User32.WmInput) ActivateKeyboard(User32.GetRawInputDevice(lParam));
				return IntPtr.Zero;
			});
		}

		private Keyboard KeyboardByHandle(IntPtr handle) =>
			ViewModel.ActiveKeyboards.FirstOrDefault(k => k.Handle == handle);

		private Keyboard KeyboardByName(IntPtr handle, out string name)
		{
			var keyboardName = name = User32.GetRawInputDeviceName(handle);
			var keyboard = ViewModel.ActiveKeyboards.FirstOrDefault(k => k.Name == keyboardName);
			if (keyboard != null) keyboard.Handle = handle;
			return keyboard;
		}

		private Keyboard EnsureKeyboard(IntPtr handle, string name)
		{
			var keyboard = new Keyboard { Handle = handle, Name = name };
			ViewModel.ActiveKeyboards.Add(keyboard);
			return keyboard;
		}

		/// <summary>
		/// Ensures that the keyboard is tracked as active.
		/// Activates the corresponding layout if its set and different from the current.
		/// </summary>
		private void ActivateKeyboard(IntPtr handle)
		{
			var activeKeyboard =
				KeyboardByHandle(handle) ??
				KeyboardByName(handle, out var name) ??
				EnsureKeyboard(handle, name);

			activeKeyboard.OnChange(nameof(Keyboard.Name));
			if (!activeKeyboard.PreferredLayout.Handle.HasValue) return;

			var preferredLayout = activeKeyboard.PreferredLayout.Handle.Value;
			if (preferredLayout == User32.GetKeyboardLayout()) return;

			User32.SetCurrentLayout(preferredLayout);
			User32.SetDefaultLayout(preferredLayout);
		}
	}
}
