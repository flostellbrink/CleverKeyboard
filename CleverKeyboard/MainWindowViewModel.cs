using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global

namespace CleverKeyboard
{
	public class MainWindowViewModel
	{
		public MainWindowViewModel()
		{
			Keyboards = User32.GetKeyboards()
				.Select(handle => new Keyboard { Handle = handle, Name = User32.GetRawInputDeviceName(handle) })
				.ToList();

			ActiveKeyboards = new ObservableCollection<Keyboard>();

			Layouts = User32.GetKeyboardLayouts()
				.Select(handle => new Layout { Handle = handle, Name = User32.GetKeyboardName(handle) })
				.Prepend(new Layout { Description = "Keep current layout" })
				.ToList();
		}

		/// <summary>List of available keyboard layouts.</summary>
		public List<Layout> Layouts { get; }

		/// <summary>List of keyboards that are connected to the machine.</summary>
		public List<Keyboard> Keyboards { get; }

		/// <summary>List of keyboards that have been used.</summary>
		public ObservableCollection<Keyboard> ActiveKeyboards { get; }
	}

	public class Keyboard : INotifyPropertyChanged
	{
		/// <summary>Windows internal keyboard handle.</summary>
		public IntPtr Handle { get; set; }

		/// <summary>Windows internal keyboard name.</summary>
		public string Name { get; set; }

		/// <summary>Handle of the layout to be used with this keyboard.</summary>
		public IntPtr? PreferredLayoutHandle { get; set; }

		public Layout PreferredLayout
		{
			set => PreferredLayoutHandle = value.Handle;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnChange() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
	}

	public class Layout
	{
		/// <summary>Windows internal layout handle.</summary>
		public IntPtr? Handle { get; set; }

		/// <summary>Windows internal layout name.</summary>
		public string Name { get; set; }

		/// <summary>Description of what happens if this layout is selected.</summary>
		public string Description { get; set; }

		public override string ToString()
		{
			return Description ?? $"Switch to {Name}";
		}
	}
}
