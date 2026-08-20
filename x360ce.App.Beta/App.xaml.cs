using System.Windows;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(e.Exception);
			e.Handled = true;
		}
	}
}
