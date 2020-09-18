using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace CleverKeyboard
{
	public class MainWindowViewModel
	{
		public MainWindowViewModel()
		{
			Keyboards = User32.GetRawInputDeviceList()
				.Where(device => device.dwType == User32.RimTypeKeyboard)
				.Select(device => new Keyboard
				{
					Handle = device.hDevice,
					Name = User32.GetRawInputDeviceName(device.hDevice)
				})
				.ToList();
			ActiveKeyboards = new ObservableCollection<Keyboard>(Keyboards.Where(keyboard => keyboard.Activated));

			Layouts = User32.GetKeyboardLayoutList()
				.Select(handle => new Layout{Handle = handle, Name = User32.GetKeyboardLayoutName(handle)})
				.ToList();
			Layouts.Insert(0, new Layout{Name = "Keep current layout"});
		}

		/// <summary>List of available keyboard layouts.</summary>
		public List<Layout> Layouts { get; }

		/// <summary>List of keyboards that are connected to the machine.</summary>
		public List<Keyboard> Keyboards { get; }

		public ObservableCollection<Keyboard> ActiveKeyboards { get; }

		public void ActivateKeyboard(IntPtr handle)
		{
			var keyboard = Keyboards.FirstOrDefault(k => k.Handle == handle);
			if (keyboard == null) return;

			SetLayout(keyboard);
			if (keyboard.Activated) return;

			keyboard.Activated = true;
			keyboard.OnChanged(nameof(Keyboard.Activated));
			ActiveKeyboards.Add(keyboard);
		}

		private void SetLayout(Keyboard keyboard)
		{
			if (!keyboard.PreferredLayoutHandle.HasValue) return;

			var preferredLayout = keyboard.PreferredLayoutHandle.Value;
			var currentLayout = User32.GetKeyboardLayout();
			if (preferredLayout == currentLayout) return;

			User32.SetCurrentLayout(preferredLayout);
			User32.SetDefaultLayout(preferredLayout);
		}
	}

	public class Keyboard : INotifyPropertyChanged
	{
		/// <summary>Windows internal keyboard handle.</summary>
		public IntPtr Handle { get; set; }

		/// <summary>Windows internal keyboard name.</summary>
		public string Name { get; set; }

		/// <summary>Indicates whether the keyboard has been used.</summary>
		public bool Activated { get; set; }

		/// <summary>Handle of the layout to be used with this keyboard.</summary>
		public IntPtr? PreferredLayoutHandle { get; set; }

		public Layout PreferredLayout
		{
			set => PreferredLayoutHandle = value.Handle;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnChanged(string property)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}
	}

	public class Layout
	{
		/// <summary>Windows internal layout handle.</summary>
		public IntPtr? Handle { get; set; }

		/// <summary>Windows internal layout name.</summary>
		public string Name { get; set; }

		public override string ToString() => Name.Split("/").Last().Trim();
	}
}
