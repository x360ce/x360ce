using System;
using System.ComponentModel;
using System.Diagnostics;

namespace JocysCom.ClassLibrary.Windows
{
	public partial class UacHelper
	{

		public static Process CreateProcess(
			string fileName, string arguments = null,
			bool useFileWorkingFolder = false, bool isElevated = false)
		{
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = true;
			psi.WorkingDirectory = useFileWorkingFolder
				? new System.IO.FileInfo(fileName).DirectoryName
				: Environment.CurrentDirectory;
			psi.FileName = fileName;
			if (arguments != null)
				psi.Arguments = arguments;
			psi.CreateNoWindow = true;
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			if (isElevated)
				psi.Verb = "runas";
			var process = new Process();
			// Must enable Exited event for both sync and async scenarios.
			process.EnableRaisingEvents = true;
			process.StartInfo = psi;
			return process;
		}

		/// <summary>
		/// Start program mode.
		/// </summary>
		/// <param name="fileName"></param>
		public static int RunProcess(
			string fileName, string arguments = null,
			bool useFileWorkingFolder = false, bool isElevated = false
			)
		{
			int exitCode = -1;
			if (string.IsNullOrWhiteSpace(fileName))
				throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateProcess(fileName, arguments, useFileWorkingFolder, isElevated))
			{
				try
				{
					process.Start();
				}
				catch (Win32Exception)
				{
					// The user refused to allow privileges elevation
					// or other error happened. Do nothing and return...
					return exitCode;
				}
				process.WaitForExit();
				exitCode = process.ExitCode;
			}
			return exitCode;
		}
		public static void RunProcessAsync(
			string fileName, string arguments = null,
			bool useFileWorkingFolder = false, bool isElevated = false,
			EventHandler exitedEventHandler = null
		)
		{
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("Executable file name must be specified");
			using (Process process = CreateProcess(fileName, arguments, useFileWorkingFolder, isElevated: isElevated))
			{
				if (exitedEventHandler != null)
					process.Exited += exitedEventHandler;
				try
				{
					process.Start();
				}
				catch (Win32Exception)
				{
					// The user refused to allow privileges elevation
					// or other error happend. Do nothing and return...
				}
			}
		}

		/// <summary>
		/// Executable file.
		/// </summary>
		public static string CurrentProcessFileName
		{
			get
			{
				if (string.IsNullOrEmpty(_UpdateExeFileFullName))
					using (var process = Process.GetCurrentProcess())
						_UpdateExeFileFullName = process.MainModule?.FileName;
				return _UpdateExeFileFullName;
			}
		}
		static string _UpdateExeFileFullName;

		/// <summary>
		/// Restart curent app in elevated mode.
		/// </summary>
		public static void RunElevated()
		{
			//if (IsElevated)
			//	throw new ApplicationException("Elevated already");
			RunProcessAsync(CurrentProcessFileName, null, false, true);
			//Close this instance because we have an elevated instance
			System.Windows.Application.Current.Shutdown();
		}

	}
}
