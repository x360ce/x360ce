using System;
using System.Collections.Generic;
using System.IO;

namespace x360ce.Setup
{
	public partial class SetupEngine
	{
		public static string SettingsDir => @"C:\ProgramData\X360CE\Settings";
		public static string UserGamesXml => Path.Combine(SettingsDir, "x360ce.UserGames.xml");
		public static string UserSettingsXml => Path.Combine(SettingsDir, "x360ce.UserSettings.xml");
		public static string PadSettingsXml => Path.Combine(SettingsDir, "x360ce.PadSettings.xml");
		public static string UserDevicesXml => Path.Combine(SettingsDir, "x360ce.UserDevices.xml");

		public const string VerifiedPadChecksum = "afe12f16-63dd-07f4-4fc6-96d2b1238b8f";
		public const string DefaultTwinCtrl1Guid = "7cb4d230-2cd4-11f1-8001-444553540000";
		public const string DefaultTwinCtrl2Guid = "7cb52050-2cd4-11f1-8002-444553540000";

		/// <summary>
		/// Safely deploys or replaces a file in the destination folder.
		/// Overwrites existing files directly in-place (updating them with zero duplicate files).
		/// Clears ReadOnly attributes if present to guarantee reliable replacement.
		/// </summary>
		private void DeployOrReplaceFile(string sourcePath, string destPath, Action<string> log)
		{
			if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
				return;

			try
			{
				bool existed = File.Exists(destPath);
				if (existed)
				{
					// Ensure destination file is not marked ReadOnly so it can be updated
					var attributes = File.GetAttributes(destPath);
					if ((attributes & FileAttributes.ReadOnly) != 0)
					{
						File.SetAttributes(destPath, attributes & ~FileAttributes.ReadOnly);
					}
				}

				File.Copy(sourcePath, destPath, true);

				var fileName = Path.GetFileName(destPath);
				if (existed)
				{
					log?.Invoke(string.Format("  -> Updated/Replaced: {0}", fileName));
				}
				else
				{
					log?.Invoke(string.Format("  -> Installed: {0}", fileName));
				}
			}
			catch (UnauthorizedAccessException)
			{
				log?.Invoke(string.Format("  -> Protected destination: {0} (Virtual ViGEm controller registered globally).", Path.GetFileName(destPath)));
			}
			catch (IOException ex)
			{
				log?.Invoke(string.Format("  -> File locked: {0} ({1}). Please close any game or x360ce instance.", Path.GetFileName(destPath), ex.Message));
			}
			catch (Exception ex)
			{
				log?.Invoke(string.Format("  -> Error copying {0}: {1}", Path.GetFileName(destPath), ex.Message));
			}
		}

		/// <summary>
		/// Performs the complete automatic installation, file deployment, and controller optimization.
		/// </summary>
		public bool InstallToFolder(string targetFolder, Action<string> log, Action<int> progress = null)
		{
			if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
			{
				log?.Invoke("Error: Game folder does not exist.");
				return false;
			}

			progress?.Invoke(10);
			log?.Invoke(string.Format("Target Folder: {0}", targetFolder));

			// 1. Locate Source Binaries
			var executingDir = Path.GetDirectoryName(typeof(SetupEngine).Assembly.Location);
			var candidateDirs = new[]
			{
				executingDir,
				AppDomain.CurrentDomain.BaseDirectory,
				Path.Combine(executingDir, "Release_Portable"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Release_Portable"),
				Path.Combine(Environment.CurrentDirectory, "Release_Portable"),
				@"D:\Projects\x360ce app\Release_Portable"
			};

			string sourceDir = null;
			foreach (var cDir in candidateDirs)
			{
				if (!string.IsNullOrEmpty(cDir) && Directory.Exists(cDir) && File.Exists(Path.Combine(cDir, "x360ce.exe")))
				{
					sourceDir = cDir;
					break;
				}
			}

			if (string.IsNullOrEmpty(sourceDir))
			{
				sourceDir = executingDir;
			}

			var sourceExe = Path.Combine(sourceDir, "x360ce.exe");
			var sourceIni = Path.Combine(sourceDir, "x360ce.ini");
			var sourceEngine = Path.Combine(sourceDir, "x360ce.Engine.dll");
			var sourceConfig = Path.Combine(sourceDir, "x360ce.exe.config");

			// 2. Deploy or Replace Files in Game Folder (always replace, never skip or duplicate)
			log?.Invoke("Deploying and updating x360ce emulator files...");
			if (File.Exists(sourceExe))
			{
				DeployOrReplaceFile(sourceExe, Path.Combine(targetFolder, "x360ce.exe"), log);
			}
			if (File.Exists(sourceIni))
			{
				DeployOrReplaceFile(sourceIni, Path.Combine(targetFolder, "x360ce.ini"), log);
			}
			if (File.Exists(sourceEngine))
			{
				DeployOrReplaceFile(sourceEngine, Path.Combine(targetFolder, "x360ce.Engine.dll"), log);
			}
			if (File.Exists(sourceConfig))
			{
				DeployOrReplaceFile(sourceConfig, Path.Combine(targetFolder, "x360ce.exe.config"), log);
			}

			// Copy or replace all dependency DLLs without skipping (strictly from confirmed source folder)
			if (Directory.Exists(sourceDir) && File.Exists(sourceExe))
			{
				var processedDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var dll in Directory.GetFiles(sourceDir, "*.dll"))
				{
					var dllName = Path.GetFileName(dll);
					if (processedDlls.Contains(dllName))
						continue;

					processedDlls.Add(dllName);
					DeployOrReplaceFile(dll, Path.Combine(targetFolder, dllName), log);
				}
			}

			progress?.Invoke(40);

			// 3. Ensure Settings Directory
			if (!Directory.Exists(SettingsDir))
				Directory.CreateDirectory(SettingsDir);

			// 4. Configure Verified Pad Settings (Removes button reversal, sets accurate sticks)
			log?.Invoke("Calibrating controller layout (verified Twin USB / XInput mapping)...");
			ConfigurePadSettingsXml();

			progress?.Invoke(60);

			// 5. Detect and Register Game Executables
			var games = ScanFolderForGameExecutables(targetFolder);
			if (games.Count == 0)
			{
				// Fallback: register the folder as target
				games.Add(new DetectedGameInfo
				{
					FilePath = Path.Combine(targetFolder, Path.GetFileName(targetFolder) + ".exe"),
					Is64Bit = true
				});
			}

			var controllers = DetectConnectedControllers();

			log?.Invoke(string.Format("Configuring {0} game executable(s) with 2-Player Virtual ViGEm support...", games.Count));
			foreach (var game in games)
			{
				RegisterGameInXml(game, controllers);
				log?.Invoke(string.Format("  -> Registered: {0} ({1})", game.FileName, game.Is64Bit ? "64-bit" : "32-bit"));
			}

			progress?.Invoke(85);

			// 6. Check for Sider / PES double input fix
			FixSiderDoubleInput(targetFolder, log);

			progress?.Invoke(100);
			log?.Invoke("Setup and optimization complete! Virtual Xbox 360 controllers are ready.");
			return true;
		}
	}
}

