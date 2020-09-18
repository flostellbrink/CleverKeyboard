using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace CleverKeyboard.Commands
{
	public class OpenCommand : MarkupExtension, ICommand
	{
		private static OpenCommand Instance { get; } = new OpenCommand();

#pragma warning disable
		public event EventHandler CanExecuteChanged;
#pragma warning restore

		public bool CanExecute(object _)
		{
			return true;
		}

		public void Execute(object _)
		{
			var window = Application.Current.MainWindow;
			if (window == null) return;

			window.WindowState = WindowState.Normal;
			Application.Current.MainWindow?.Focus();
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return Instance;
		}
	}
}
