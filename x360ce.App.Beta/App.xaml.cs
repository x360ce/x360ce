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
		}

		/// <summary>
		/// Get shared resource.
		/// </summary>
		public static object GetResource(string name)
			=> Current.Resources[name];

	}
}
