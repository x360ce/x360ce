using System;
using System.Windows;
using Application = System.Windows.Application;

#if NETCOREAPP
namespace x360ce.Net60Test
#else
namespace x360ce.Net48Test
#endif
{
	internal class CustomWindow: Window
	{
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			//Application.Current.Dispatcher.InvokeShutdown();
		}
	}
}
