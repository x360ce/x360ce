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
			// Initialize non-UI service first.
			Global._LocalService = new Service.LocalService();
			Global._LocalService.Start();
			// Initialize main window.
			var w = new MainWindow();
			Global._MainWindow = w;
			MainWindow = w;
			// Initialize Tray Icon which will manage main window.
			Global._TrayManager = new Service.TrayManager(w);
			Global._TrayManager.OnExitClick += _TrayManager_OnExitClick;
			Global._TrayManager.OnWindowSizeChanged += _TrayManager_OnWindowSizeChanged;
			Global._TrayManager.InitMinimizeAndTopMost();
			// Finally show window.
			w.Show();
		}

		private void _TrayManager_OnWindowSizeChanged(object sender, System.EventArgs e)
		{
			// Form GUI update is very heavy on CPU.
			// Enable form GUI update only if form is not minimized.
			var enableUpdates = MainWindow.WindowState != WindowState.Minimized && !Program.IsClosing;
			Global._MainWindow.EnableFormUpdates(enableUpdates);
		}

		private void _TrayManager_OnExitClick(object sender, System.EventArgs e)
		{
			// Remove tray icon first.
			Global._TrayManager.Dispose();
			Current.Shutdown();
		}
	}
}
