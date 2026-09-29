using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.Win32;
using System;
using System.Linq;

namespace x360ce.App.Issues
{
	/// <summary>
	/// Detect the Visual C++ v14x runtime, and explain what its installer answered.
	/// </summary>
	/// <remarks>
	/// Uses the component key Microsoft documents for redistributable detection:
	/// https://learn.microsoft.com/cpp/windows/redistributing-visual-cpp-files
	/// Matching display names in Add/Remove Programs is unreliable, because the name
	/// carries a year label that changes with every release and is localized.
	/// All 14.x runtimes are backwards compatible, so any of them is enough.
	/// </remarks>
	public static class CppRuntimeDetector
	{

		const string RuntimeKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes";

		/// <summary>Microsoft's page naming the latest supported Visual C++ v14 Redistributable.</summary>
		public const string LatestUrl = "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist";

		/// <summary>Microsoft's guide for a Visual C++ Redistributable that will not install.</summary>
		public const string TroubleshootUrl = "https://learn.microsoft.com/cpp/windows/troubleshoot-vc-redistributable-installation-issues";

		/// <summary>What the Issues tab says about the runtime, given what its installer last answered.</summary>
		/// <param name="exitCode">The installer's last exit code; null when it has not run.</param>
		/// <param name="installed">Whether the runtime is detected now.</param>
		/// <param name="program">The runtime's name as the user sees it.</param>
		/// <param name="description">Text for the Issues tab; null when there is nothing to say.</param>
		/// <param name="troubleshoot">True when "More..." opens <see cref="TroubleshootUrl"/>.</param>
		/// <returns>The severity to show.</returns>
		/// <remarks>
		/// A code arrives either as Windows Installer returns it (1638) or wrapped as a Windows error
		/// result (0x80070666), the form the installer's own error window shows, and both read the same.
		///
		/// "The same or a newer version is already registered" while the runtime is not detected is
		/// Moderate, not Critical. Installing again cannot change it, and a Critical issue stops the checks
		/// after it and keeps the controller pages closed.
		/// </remarks>
		public static IssueSeverity ExplainInstall(int? exitCode, bool installed, string program, out string description, out bool troubleshoot)
		{
			description = null;
			troubleshoot = false;
			if (installed)
				return IssueSeverity.None;
			if (!exitCode.HasValue)
			{
				description = "Install " + program;
				return IssueSeverity.Critical;
			}
			var code = exitCode.Value;
			// A Windows error wrapped as a result (0x8007xxxx) carries the error in its low 16 bits.
			if ((code & unchecked((int)0xFFFF0000)) == unchecked((int)0x80070000))
				code &= 0xFFFF;
			switch (code)
			{
				case -1:
					description = "The " + program + " installer did not start, because administrator permission was refused. Install again and allow it.";
					return IssueSeverity.Critical;
				case 0:
					description = "The " + program + " installer finished, but the runtime is still not found. Restart Windows, then check again.";
					return IssueSeverity.Critical;
				case 1223:
				case 1602:
					description = "Installing " + program + " was cancelled.";
					return IssueSeverity.Critical;
				case 1618:
					description = "Another installation is running. Wait for it to finish, then install " + program + " again.";
					return IssueSeverity.Critical;
				case 1638:
					description = "The same or a newer " + program + " is already registered, but it is not found. " +
						"Remove every Microsoft Visual C++ v14 Redistributable in Windows Settings > Apps, restart Windows, " +
						"then install again. More... opens Microsoft's troubleshooting guide.";
					troubleshoot = true;
					return IssueSeverity.Moderate;
				case 3010:
					description = "Restart Windows to finish installing " + program + ".";
					return IssueSeverity.Critical;
				default:
					// A result that is not a Windows error is read in hexadecimal, the form Microsoft's guide uses.
					var shown = code < 0 ? "0x" + code.ToString("X8") : code.ToString();
					description = string.Format("Installing {0} failed with code {1}. More... opens Microsoft's troubleshooting guide.", program, shown);
					troubleshoot = true;
					return IssueSeverity.Critical;
			}
		}

		/// <summary>Whether the installer did its work: 0, or 3010 when Windows must restart to finish it.</summary>
		public static bool Succeeded(int? exitCode)
		{
			return exitCode == 0 || exitCode == 3010;
		}

		/// <summary>Installs without asking anything, shows its progress, and leaves the restart to the user.</summary>
		public const string InstallArguments = "/install /passive /norestart";

		/// <summary>Downloads a Visual C++ v14 installer, runs it as Administrator and waits for it.</summary>
		/// <param name="uri">Microsoft's permalink to the installer.</param>
		/// <param name="infoPage">Page offered when the download fails.</param>
		/// <returns>The installer's exit code; -1 when administrator permission was refused; null when the download failed.</returns>
		/// <remarks>
		/// Waits for the installer, so it runs only from an issue's fix, which is on a worker thread. The
		/// virtual bus keeps its first answer about the runtime, so after an install that succeeded it is
		/// told to ask again.
		/// </remarks>
		public static int? Install(Uri uri, Uri infoPage)
		{
			var localPath = System.IO.Path.Combine(x360ce.Engine.EngineHelper.AppDataPath, "Temp", uri.Segments.Last());
			var exitCode = IssueHelper.DownloadAndInstall(uri, localPath, infoPage, true, InstallArguments);
			if (Succeeded(exitCode))
				Nefarius.ViGEm.Client.ViGEmClient.ForgetRuntimeInstalled();
			return exitCode;
		}

		/// <summary>Installed runtime version, or null when not installed.</summary>
		/// <param name="x64">True for the 64-bit runtime, false for the 32-bit runtime.</param>
		public static Version GetInstalledVersion(bool x64)
		{
			var view = x64 ? RegistryView.Registry64 : RegistryView.Registry32;
			var name = x64 ? "x64" : "x86";
			try
			{
				using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
				using (var key = baseKey.OpenSubKey(RuntimeKey + "\\" + name))
				{
					if (key == null)
						return null;
					if (GetInt(key, "Installed") != 1)
						return null;
					var major = GetInt(key, "Major");
					// Runtimes before 14.0 are a different, incompatible product line.
					if (major < 14)
						return null;
					return new Version(major, GetInt(key, "Minor"), GetInt(key, "Bld"), GetInt(key, "Rbld"));
				}
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return null;
			}
		}

		static int GetInt(RegistryKey key, string name)
		{
			var value = key.GetValue(name);
			if (value == null)
				return 0;
			try
			{
				return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				return 0;
			}
		}

	}
}
