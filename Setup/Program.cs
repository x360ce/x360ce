using System;
using System.Windows.Forms;

namespace x360ce.Setup
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			string targetFolder = null;
			bool silent = false;

			if (args != null && args.Length > 0)
			{
				foreach (var arg in args)
				{
					if (string.Equals(arg, "/silent", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(arg, "-silent", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(arg, "/s", StringComparison.OrdinalIgnoreCase))
					{
						silent = true;
					}
					else if (!arg.StartsWith("/") && !arg.StartsWith("-"))
					{
						targetFolder = arg.Trim('"', '\'');
					}
				}
			}

			if (silent && !string.IsNullOrEmpty(targetFolder))
			{
				try
				{
					var engine = new SetupEngine();
					engine.InstallToFolder(targetFolder, msg => { });
				}
				catch { }
				return;
			}

			Application.Run(new MainSetupForm(targetFolder));
		}
	}
}
