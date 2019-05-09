using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

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
