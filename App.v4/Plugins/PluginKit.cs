using System;
using System.IO;

namespace x360ce.App.Plugins
{
	/// <summary>Writes a folder to start a plugin in: the types a plugin implements, the build settings and script, a sample and the help.</summary>
	/// <remarks>
	/// Everything comes from inside x360ce.exe, so one command gives a person or an AI agent all a plugin needs. In the folder,
	/// Build-Plugins.ps1 builds the sample, TestController\TestController.cs; a copy of its folder, renamed and changed, is a
	/// new plugin.
	/// </remarks>
	public static class PluginKit
	{
		/// <summary>The files written, by their path in the folder, and the embedded resource each one is.</summary>
		static readonly string[,] Files =
		{
			{ "x360ce.Plugins.dll", "x360ce.Plugins.dll" },
			{ "Directory.Build.props", "Documents.PluginKit.Directory.Build.props" },
			{ "Build-Plugins.ps1", "Documents.PluginKit.Build-Plugins.ps1" },
			{ @"TestController\TestController.cs", "Documents.PluginKit.TestController.cs" },
			{ "README.md", AppHelper.HelpPluginsResource },
		};

		/// <summary>Writes the kit into a folder, made when missing.</summary>
		/// <returns>0 when every file was written; 1 when one could not be.</returns>
		public static int Write(string folder, TextWriter output, TextWriter error)
		{
			if (string.IsNullOrWhiteSpace(folder))
			{
				error.WriteLine("Name the folder to write the plugin kit into: x360ce.exe /PluginKit=<folder>");
				return 1;
			}
			folder = Path.GetFullPath(folder);
			try
			{
				Directory.CreateDirectory(folder);
				for (var i = 0; i < Files.GetLength(0); i++)
				{
					using (var stream = Engine.EngineHelper.GetResourceStream(Files[i, 1]))
					{
						if (stream == null)
						{
							error.WriteLine("This x360ce carries no " + Files[i, 0] + ".");
							return 1;
						}
						var path = Path.Combine(folder, Files[i, 0]);
						Directory.CreateDirectory(Path.GetDirectoryName(path));
						using (var file = File.Create(path))
							stream.CopyTo(file);
					}
				}
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				error.WriteLine("The plugin kit could not be written to " + folder + ": " + ex.Message);
				return 1;
			}
			output.WriteLine("Plugin kit written to " + folder + ".");
			output.WriteLine("Build its plugins there with: .\\Build-Plugins.ps1");
			output.WriteLine("Then copy a plugin's folder, such as bin\\MyDevice, into " + Path.Combine(Engine.EngineHelper.AppDataPath, PluginManager.FolderName) + ".");
			return 0;
		}
	}
}
