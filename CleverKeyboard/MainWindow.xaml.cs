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
			if (!User32.RegisterInputSink(handle)) throw new Exception("Failed to register input sink.");

			var source = HwndSource.FromHwnd(handle);
			if (source == null) throw new Exception("Failed to create hwnd source");
			source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
			{
				if (msg == User32.WmInput) ActivateKeyboard(User32.GetRawInputDevice(lParam));
				return IntPtr.Zero;
			});
		}

		/// <summary>
		/// Ensures that the keyboard is tracked as active.
		/// Activates the corresponding layout if its set and different from the current.
		/// </summary>
		private void ActivateKeyboard(IntPtr handle)
		{
			var activeKeyboard = ViewModel.ActiveKeyboards.FirstOrDefault(k => k.Handle == handle);
			if (activeKeyboard == null)
			{
				activeKeyboard = ViewModel.Keyboards.FirstOrDefault(k => k.Handle == handle);
				if (activeKeyboard == null) return;
				ViewModel.ActiveKeyboards.Add(activeKeyboard);
			}

			activeKeyboard.OnChange();
			if (!activeKeyboard.PreferredLayoutHandle.HasValue) return;

			var preferredLayout = activeKeyboard.PreferredLayoutHandle.Value;
			if (preferredLayout == User32.GetKeyboardLayout()) return;

			User32.SetCurrentLayout(preferredLayout);
			User32.SetDefaultLayout(preferredLayout);
		}
	}
}
