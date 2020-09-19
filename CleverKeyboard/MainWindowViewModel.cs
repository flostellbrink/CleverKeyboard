using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global

namespace CleverKeyboard
{
	public class MainWindowViewModel
	{
		public static Layout KeepLayout => new Layout { Description = "Keep current layout" };

		public MainWindowViewModel()
		{
			Layouts = User32.GetKeyboardLayouts()
				.Select(handle => new Layout { Handle = handle, Name = User32.GetKeyboardName(handle) })
				.Prepend(KeepLayout)
				.ToList();

			try
			{
				var configJson = File.ReadAllText($"{AssemblyName}Config.json");
				ActiveKeyboards = JsonConvert.DeserializeObject<BindingList<Keyboard>>(configJson);
			}
			catch
			{
				ActiveKeyboards = new BindingList<Keyboard>();
				FirstRun = true;
			}

			ActiveKeyboards.ListChanged += (sender, args) => File.WriteAllText(
				$"{AssemblyName}Config.json",
				JsonConvert.SerializeObject(ActiveKeyboards, Formatting.Indented));
		}

		private RegistryKey RunKey => Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
		private string AssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
		private string ExeLocation => System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

		/// <summary>Indicates whether the application starts automatically.</summary>
		public bool AutoStart {
			get => RunKey.GetValue(AssemblyName) as string == ExeLocation;
			set
			{
				if (value) RunKey.SetValue(AssemblyName, ExeLocation);
				else RunKey.DeleteValue(AssemblyName);
			}
		}

		public bool FirstRun { get; set; }

		/// <summary>List of available keyboard layouts.</summary>
		public List<Layout> Layouts { get; }

		/// <summary>List of keyboards that have been used.</summary>
		public BindingList<Keyboard> ActiveKeyboards { get; }
	}

	public class Keyboard : INotifyPropertyChanged
	{
		/// <summary>Windows internal keyboard handle.</summary>
		[JsonIgnore]
		public IntPtr Handle { get; set; }

		/// <summary>Windows internal keyboard name.</summary>
		public string Name { get; set; }

		private Layout _preferredLayout = MainWindowViewModel.KeepLayout;

		public Layout PreferredLayout
		{
			get => _preferredLayout;
			set
			{
				_preferredLayout = value;
				OnChange(nameof(PreferredLayout));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnChange(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
	}

	public class Layout
	{
		/// <summary>Windows internal layout handle.</summary>
		public IntPtr? Handle { get; set; }

		/// <summary>Windows internal layout name.</summary>
		public string Name { get; set; }

		/// <summary>Description of what happens if this layout is selected.</summary>
		public string Description { get; set; }

		protected bool Equals(Layout other)
		{
			return Nullable.Equals(Handle, other.Handle);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((Layout) obj);
		}

		public override int GetHashCode()
		{
			return Handle?.GetHashCode() ?? 0;
		}

		public override string ToString()
		{
			return Description ?? $"Switch to {Name}";
		}
	}
}
