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
			var w = new MainWindow();
			MainWindow = w;
			x360ce.App.MainWindow.Current = w;
			w.Show();
		}
	}
}
