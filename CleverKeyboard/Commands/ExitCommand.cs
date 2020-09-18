using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace CleverKeyboard.Commands
{
	public class ExitCommand : MarkupExtension, ICommand
	{
		private static ExitCommand Instance { get; } = new ExitCommand();

#pragma warning disable
		public event EventHandler CanExecuteChanged;
#pragma warning restore

		public bool CanExecute(object _)
		{
			return true;
		}

		public void Execute(object _)
		{
			Application.Current.Shutdown();
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return Instance;
		}
	}
}
