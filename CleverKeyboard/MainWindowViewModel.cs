using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

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

		private RegistryKey RunKey => Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
		private string AssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
		private string AssemblyLocation => Assembly.GetExecutingAssembly().Location;

		/// <summary>Indicates whether the application starts automatically.</summary>
		public bool AutoStart {
			get => RunKey.GetValue(AssemblyName) as string == AssemblyLocation;
			set
			{
				if (value) RunKey.SetValue(AssemblyName, AssemblyLocation);
				else RunKey.DeleteValue(AssemblyName);
			}
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
