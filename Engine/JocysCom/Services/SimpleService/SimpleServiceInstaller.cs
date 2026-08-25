using System.ComponentModel;

namespace JocysCom.ClassLibrary.Services.SimpleService
{
	[RunInstaller(true)]
	public partial class SimpleServiceInstaller : System.Configuration.Install.Installer
	{
		public SimpleServiceInstaller()
		{
			InitializeComponent();
		}

	}
}
