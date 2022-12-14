using System.Windows;

namespace x360ce.Tests
{
	/// <summary>
	/// Interaction logic for TestApplication.xaml
	/// </summary>
	public partial class MemoryLeakApp : Application
	{
		public MemoryLeakApp()
		{
			// Load all resources.
			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
		}
	}
}
