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
				.Where(device => device.dwType == User32.RIM_TYPEKEYBOARD)
				.Select(device => new Keyboard
				{
					Handle = device.hDevice,
					Name = User32.GetRawInputDeviceName(device.hDevice)
				})
				.ToList();
			ActiveKeyboards = new ObservableCollection<Keyboard>(Keyboards.Where(keyboard => keyboard.Active));
		}

		/// <summary>List of available keyboard layouts.</summary>
		public List<string> Layouts { get; } = new List<string>
		{
			"Keep current layout",
			"Switch to Australian"
		};

		/// <summary>List of keyboards that are connected to the machine.</summary>
		public List<Keyboard> Keyboards { get; }

		public ObservableCollection<Keyboard> ActiveKeyboards { get; }

		public void MakeKeyboardActive(IntPtr handle)
		{
			var keyboard = Keyboards.FirstOrDefault(k => k.Handle == handle);
			if (keyboard?.Active != false) return;

			keyboard.Active = true;
			keyboard.OnChanged(nameof(Keyboard.Active));
			ActiveKeyboards.Add(keyboard);
		}
	}

	public class Keyboard : INotifyPropertyChanged
	{
		/// <summary>Windows internal keyboard handle.</summary>
		public IntPtr Handle { get; set; }

		/// <summary>Windows internal keyboard name.</summary>
		public string Name { get; set; }

		/// <summary>Indicates whether the keyboard has been used.</summary>
		public bool Active { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnChanged(string property)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}
	}
}
