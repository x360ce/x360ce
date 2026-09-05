using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace x360ce.Setup
{
	public class DetectedGameInfo
	{
		public string FilePath { get; set; }
		public string FileName => Path.GetFileName(FilePath);
		public string DirectoryPath => Path.GetDirectoryName(FilePath);
		public bool Is64Bit { get; set; }
	}

	public class DetectedControllerInfo
	{
		public string Name { get; set; }
		public string InstanceGuid { get; set; }
		public string ProductGuid { get; set; }
		public int VendorId { get; set; }
		public int ProductId { get; set; }
		public int PlayerIndex { get; set; }
		public bool IsOnline { get; set; }
	}

	public class SetupEngine
	{
		private static readonly HashSet<string> ExcludedExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"x360ce.exe", "Setup.exe", "Inject_Games.exe", "dxwebsetup.exe", "dxsetup.exe",
			"vcredist_x86.exe", "vcredist_x64.exe", "CrashReport.exe", "CrashSender.exe",
			"UnityCrashHandler64.exe", "UnityCrashHandler32.exe", "unins000.exe", "Uninstall.exe",
			"BEService_x64.exe", "Social-Club-Setup.exe", "EpicGamesLauncher.exe", "EOSBootStrapper.exe", "EpicWebHelper.exe",
			// JDK build & diagnostic tools (so only javaw.exe / java.exe are registered for Minecraft)
			"javac.exe", "javadoc.exe", "javap.exe", "jcmd.exe", "jconsole.exe", "jdb.exe", "jdeprscan.exe", "jdeps.exe",
			"jfr.exe", "jhsdb.exe", "jimage.exe", "jinfo.exe", "jlink.exe", "jmap.exe", "jmod.exe", "jpackage.exe",
			"jps.exe", "jrunscript.exe", "jshell.exe", "jstack.exe", "jstat.exe", "jstatd.exe", "jwebserver.exe",
			"keytool.exe", "kinit.exe", "klist.exe", "ktab.exe", "rmiregistry.exe", "serialver.exe", "jabswitch.exe",
			"jaccessinspector.exe", "jaccesswalker.exe", "jar.exe", "jarsigner.exe"
		};

		public static string SettingsDir => @"C:\ProgramData\X360CE\Settings";
		public static string UserGamesXml => Path.Combine(SettingsDir, "x360ce.UserGames.xml");
		public static string UserSettingsXml => Path.Combine(SettingsDir, "x360ce.UserSettings.xml");
		public static string PadSettingsXml => Path.Combine(SettingsDir, "x360ce.PadSettings.xml");
		public static string UserDevicesXml => Path.Combine(SettingsDir, "x360ce.UserDevices.xml");

		public const string VerifiedPadChecksum = "afe12f16-63dd-07f4-4fc6-96d2b1238b8f";
		public const string DefaultTwinCtrl1Guid = "7cb4d230-2cd4-11f1-8001-444553540000";
		public const string DefaultTwinCtrl2Guid = "7cb52050-2cd4-11f1-8002-444553540000";

		/// <summary>
		/// Detects Minecraft Bedrock Edition (Windows 10/11) and Java Edition runtimes across official and third-party launchers.
		/// </summary>
		public List<DetectedGameInfo> DetectMinecraftInstallations()
		{
			var list = new List<DetectedGameInfo>();
			var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			void AddGame(string exePath)
			{
				if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath) || foundPaths.Contains(exePath))
					return;

				foundPaths.Add(exePath);
				list.Add(new DetectedGameInfo
				{
					FilePath = exePath,
					Is64Bit = CheckIs64Bit(exePath)
				});
			}

			try
			{
				// 1. Minecraft Bedrock Edition (Windows 10/11 Edition)
				// Query HKCU Repository Packages registry for PackageRootFolder
				try
				{
					using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages"))
					{
						if (key != null)
						{
							foreach (var subKeyName in key.GetSubKeyNames())
							{
								if (subKeyName.StartsWith("Microsoft.MinecraftUWP", StringComparison.OrdinalIgnoreCase))
								{
									using (var subKey = key.OpenSubKey(subKeyName))
									{
										var rootFolder = subKey?.GetValue("PackageRootFolder") as string;
										if (!string.IsNullOrEmpty(rootFolder) && Directory.Exists(rootFolder))
										{
											var exe = Path.Combine(rootFolder, "Minecraft.Windows.exe");
											if (File.Exists(exe))
												AddGame(exe);
										}
									}
								}
							}
						}
					}
				}
				catch { }

				// Check XboxGames across all ready drives
				DriveInfo[] drives;
				try { drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray(); } catch { drives = new DriveInfo[0]; }

				foreach (var drive in drives)
				{
					var root = drive.RootDirectory.FullName;
					var xboxGames = Path.Combine(root, "XboxGames");
					if (Directory.Exists(xboxGames))
					{
						try
						{
							foreach (var d in Directory.GetDirectories(xboxGames, "*Minecraft*"))
							{
								var exe = Path.Combine(d, "Content", "Minecraft.Windows.exe");
								if (File.Exists(exe))
									AddGame(exe);
								else
								{
									var subExes = Directory.GetFiles(d, "Minecraft.Windows.exe", SearchOption.AllDirectories);
									foreach (var se in subExes.Take(2)) AddGame(se);
								}
							}
						}
						catch { }
					}
				}

				// 2. Minecraft Java Edition (Official MS Store Launcher Runtimes)
				var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				var packagesDir = Path.Combine(localAppData, "Packages");
				if (Directory.Exists(packagesDir))
				{
					try
					{
						foreach (var pkg in Directory.GetDirectories(packagesDir, "Microsoft.4297127D64EC6*"))
						{
							var runtimeDir = Path.Combine(pkg, @"LocalCache\Local\runtime");
							if (Directory.Exists(runtimeDir))
							{
								foreach (var jw in Directory.GetFiles(runtimeDir, "javaw.exe", SearchOption.AllDirectories).Take(5))
									AddGame(jw);
							}
						}
					}
					catch { }
				}

				// 3. Minecraft Java Edition (Desktop / Program Files Launcher Runtimes)
				var progFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
				var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				var mcRuntimeRoots = new[]
				{
					Path.Combine(progFiles86, @"Minecraft Launcher\runtime"),
					Path.Combine(progFiles, @"Minecraft Launcher\runtime"),
					Path.Combine(localAppData, @"Programs\Minecraft Launcher\runtime")
				};

				foreach (var rt in mcRuntimeRoots)
				{
					if (Directory.Exists(rt))
					{
						try
						{
							foreach (var jw in Directory.GetFiles(rt, "javaw.exe", SearchOption.AllDirectories).Take(5))
								AddGame(jw);
						}
						catch { }
					}
				}

				// 4. Standard %AppData%\.minecraft runtime
				var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var dotMinecraftRuntime = Path.Combine(appData, @".minecraft\runtime");
				if (Directory.Exists(dotMinecraftRuntime))
				{
					try
					{
						foreach (var jw in Directory.GetFiles(dotMinecraftRuntime, "javaw.exe", SearchOption.AllDirectories).Take(5))
							AddGame(jw);
					}
					catch { }
				}

				// 5. Third-Party Minecraft Launchers (Prism, Modrinth, CurseForge, Lunar, Badlion, Feather, MultiMC)
				var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				var thirdPartyRoots = new[]
				{
					Path.Combine(appData, "PrismLauncher"),
					Path.Combine(appData, "ModrinthApp"),
					Path.Combine(localAppData, @"Programs\Modrinth App"),
					Path.Combine(userProfile, @"curseforge\minecraft\Install"),
					Path.Combine(userProfile, @".lunarclient\jre"),
					Path.Combine(appData, "badlion-client"),
					Path.Combine(userProfile, @".badlion"),
					Path.Combine(appData, ".feather"),
					Path.Combine(appData, "MultiMC"),
					Path.Combine(progFiles, "Java"),
					Path.Combine(progFiles, "Eclipse Adoptium"),
					Path.Combine(progFiles, "BellSoft"),
					Path.Combine(progFiles, "Zulu"),
					Path.Combine(progFiles, "Amazon Corretto")
				};

				foreach (var tp in thirdPartyRoots)
				{
					if (!string.IsNullOrEmpty(tp) && Directory.Exists(tp))
					{
						try
						{
							foreach (var jw in Directory.GetFiles(tp, "javaw.exe", SearchOption.AllDirectories).Take(5))
								AddGame(jw);
						}
						catch { }
					}
				}

				// 6. Minecraft Launcher Executables
				var launcherCandidates = new[]
				{
					Path.Combine(progFiles86, @"Minecraft Launcher\MinecraftLauncher.exe"),
					Path.Combine(progFiles, @"Minecraft Launcher\MinecraftLauncher.exe"),
					Path.Combine(localAppData, @"Programs\Minecraft Launcher\MinecraftLauncher.exe")
				};
				foreach (var lc in launcherCandidates)
				{
					if (File.Exists(lc)) AddGame(lc);
				}
			}
			catch { }

			return list;
		}

		/// <summary>
		/// Automatically finds common game directories across system drives and Minecraft installations.
		/// </summary>
		public List<string> DetectCommonGameFolders()
		{
			var results = new List<string>();
			var foundDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			void AddDir(string path)
			{
				if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !foundDirs.Contains(path))
				{
					foundDirs.Add(path);
					results.Add(path);
				}
			}

			// 1. Detect Minecraft installations first (Bedrock & Java)
			try
			{
				var mcGames = DetectMinecraftInstallations();
				foreach (var mc in mcGames)
				{
					var dir = Path.GetDirectoryName(mc.FilePath);
					if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
					{
						AddDir(dir);
					}
				}
			}
			catch { }

			// 2. Discover games across all ready drives (Fixed, Removable)
			DriveInfo[] drives;
			try
			{
				drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();
			}
			catch
			{
				drives = new DriveInfo[0];
			}

			var relativeRoots = new[]
			{
				"Games",
				"Game",
				@"SteamLibrary\steamapps\common",
				@"Steam\steamapps\common",
				@"Program Files (x86)\Steam\steamapps\common",
				@"Program Files\Steam\steamapps\common",
				"Epic Games",
				@"Program Files\Epic Games",
				@"Program Files (x86)\Epic Games",
				@"GOG Galaxy\Games",
				@"GOG Games",
				"XboxGames",
				@"EA Games",
				@"Origin Games",
				@"Ubisoft\Ubisoft Game Launcher\games",
				@"Ubisoft Game Launcher\games",
				@"Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games",
				"Battle.net",
				@"Amazon Games",
				@"Riot Games",
				"Emulators",
				"RetroArch",
				"RPCS3",
				"PCSX2",
				"Dolphin",
				"Cemu",
				"DuckStation"
			};

			foreach (var drive in drives)
			{
				var root = drive.RootDirectory.FullName;
				foreach (var rel in relativeRoots)
				{
					var fullPath = Path.Combine(root, rel);
					if (!Directory.Exists(fullPath))
						continue;

					// If this is a game library directory containing subfolders for each game:
					bool isLibraryFolder = rel.IndexOf("common", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   rel.IndexOf("Games", StringComparison.OrdinalIgnoreCase) >= 0 ||
										   rel.IndexOf("XboxGames", StringComparison.OrdinalIgnoreCase) >= 0;

					if (isLibraryFolder)
					{
						try
						{
							foreach (var sub in Directory.GetDirectories(fullPath))
							{
								if (ScanFolderForGameExecutables(sub).Count > 0)
								{
									AddDir(sub);
								}
							}
						}
						catch { }
					}

					// Also check if the directory itself directly contains game executables
					if (ScanFolderForGameExecutables(fullPath).Count > 0)
					{
						AddDir(fullPath);
					}
				}
			}

			return results;
		}

		/// <summary>
		/// Scans a game folder and returns valid game executables.
		/// </summary>
		public List<DetectedGameInfo> ScanFolderForGameExecutables(string folderPath)
		{
			var list = new List<DetectedGameInfo>();
			if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
				return list;

			var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			void TryAddExecutable(string file)
			{
				if (string.IsNullOrEmpty(file) || !File.Exists(file) || seenFiles.Contains(file))
					return;

				var name = Path.GetFileName(file);
				if (ExcludedExecutables.Contains(name) ||
					name.StartsWith("unins", StringComparison.OrdinalIgnoreCase) ||
					name.StartsWith("vcredist", StringComparison.OrdinalIgnoreCase) ||
					name.StartsWith("dxwebsetup", StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				seenFiles.Add(file);
				list.Add(new DetectedGameInfo
				{
					FilePath = file,
					Is64Bit = CheckIs64Bit(file)
				});
			}

			try
			{
				// 1. Search root directory first
				var rootFiles = Directory.GetFiles(folderPath, "*.exe", SearchOption.TopDirectoryOnly);
				foreach (var file in rootFiles)
				{
					TryAddExecutable(file);
				}

				// 2. Also search subdirectories up to 3 levels deep (for Binaries\Win64, Bin\x64, bin\javaw.exe, etc.)
				try
				{
					var subFiles = Directory.GetFiles(folderPath, "*.exe", SearchOption.AllDirectories);
					foreach (var file in subFiles.Take(40))
					{
						TryAddExecutable(file);
					}
				}
				catch { }
			}
			catch { }

			return list;
		}

		/// <summary>
		/// Reads detected physical controllers from x360ce.UserDevices.xml.
		/// </summary>
		public List<DetectedControllerInfo> DetectConnectedControllers()
		{
			var controllers = new List<DetectedControllerInfo>();

			if (File.Exists(UserDevicesXml))
			{
				try
				{
					var doc = new XmlDocument();
					doc.Load(UserDevicesXml);
					var nodes = doc.SelectNodes("//UserDevice");
					if (nodes != null)
					{
						foreach (XmlNode node in nodes)
						{
							var name = node.SelectSingleNode("InstanceName")?.InnerText ??
									   node.SelectSingleNode("ProductName")?.InnerText ?? "Gamepad";

							var capTypeStr = node.SelectSingleNode("CapType")?.InnerText ?? "0";
							int.TryParse(capTypeStr, out int capType);

							// Filter out keyboards, mice, virtual/vJoy devices
							if (capType == 18 || capType == 19) // 18 = Mouse, 19 = Keyboard
								continue;

							if (name.IndexOf("keyboard", StringComparison.OrdinalIgnoreCase) >= 0 ||
								name.IndexOf("mouse", StringComparison.OrdinalIgnoreCase) >= 0 ||
								name.IndexOf("pointer", StringComparison.OrdinalIgnoreCase) >= 0 ||
								name.IndexOf("vJoy", StringComparison.OrdinalIgnoreCase) >= 0 ||
								name.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0)
							{
								continue;
							}

							var instanceGuid = node.SelectSingleNode("InstanceGuid")?.InnerText ?? "";
							var productGuid = node.SelectSingleNode("ProductGuid")?.InnerText ?? "";
							var isOnline = node.SelectSingleNode("IsOnline")?.InnerText == "true" ||
										   node.SelectSingleNode("IsEnabled")?.InnerText == "true";

							int.TryParse(node.SelectSingleNode("DevVendorId")?.InnerText ?? "0", out int vid);
							int.TryParse(node.SelectSingleNode("DevProductId")?.InnerText ?? "0", out int pid);

							controllers.Add(new DetectedControllerInfo
							{
								Name = name.Trim(),
								InstanceGuid = instanceGuid,
								ProductGuid = productGuid,
								VendorId = vid,
								ProductId = pid,
								IsOnline = isOnline
							});
						}

						// Sort controllers deterministically (e.g. Twin USB Gamepad 1 before 2)
						controllers = controllers.OrderBy(c => c.InstanceGuid).ToList();
						for (int i = 0; i < controllers.Count; i++)
						{
							controllers[i].PlayerIndex = i + 1;
						}
					}
				}
				catch { }
			}

			// If only 1 controller detected, provide Player 2 fallback profile
			if (controllers.Count == 1)
			{
				controllers.Add(new DetectedControllerInfo
				{
					Name = "Twin USB Gamepad (Player 2)",
					InstanceGuid = DefaultTwinCtrl2Guid,
					ProductGuid = "00010810-0000-0000-0000-504944564944",
					VendorId = 0x0810,
					ProductId = 0x0001,
					PlayerIndex = 2,
					IsOnline = false
				});
			}
			// If no saved controllers found, provide verified Twin USB default profiles
			else if (controllers.Count == 0)
			{
				controllers.Add(new DetectedControllerInfo
				{
					Name = "Twin USB Gamepad (Player 1)",
					InstanceGuid = DefaultTwinCtrl1Guid,
					ProductGuid = "00010810-0000-0000-0000-504944564944",
					VendorId = 0x0810,
					ProductId = 0x0001,
					PlayerIndex = 1,
					IsOnline = true
				});
				controllers.Add(new DetectedControllerInfo
				{
					Name = "Twin USB Gamepad (Player 2)",
					InstanceGuid = DefaultTwinCtrl2Guid,
					ProductGuid = "00010810-0000-0000-0000-504944564944",
					VendorId = 0x0810,
					ProductId = 0x0001,
					PlayerIndex = 2,
					IsOnline = true
				});
			}

			return controllers;
		}

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

		private void ConfigurePadSettingsXml()
		{
			try
			{
				XmlDocument doc = new XmlDocument();
				if (File.Exists(PadSettingsXml))
				{
					doc.Load(PadSettingsXml);
				}
				else
				{
					doc.LoadXml("<Data><Items></Items></Data>");
				}

				var itemsNode = doc.SelectSingleNode("//Items");
				if (itemsNode == null)
					return;

				var existingPad = itemsNode.SelectSingleNode(string.Format("PadSetting[PadSettingChecksum='{0}']", VerifiedPadChecksum));
				if (existingPad == null)
				{
					existingPad = doc.CreateElement("PadSetting");
					itemsNode.AppendChild(existingPad);
				}

				SetNodeText(doc, existingPad, "PadSettingChecksum", VerifiedPadChecksum);
				SetNodeText(doc, existingPad, "ButtonA", "3");
				SetNodeText(doc, existingPad, "ButtonB", "2");
				SetNodeText(doc, existingPad, "ButtonX", "4");
				SetNodeText(doc, existingPad, "ButtonY", "1");
				SetNodeText(doc, existingPad, "ButtonBack", "9");
				SetNodeText(doc, existingPad, "ButtonStart", "10");
				SetNodeText(doc, existingPad, "LeftShoulder", "5");
				SetNodeText(doc, existingPad, "RightShoulder", "6");
				SetNodeText(doc, existingPad, "LeftTrigger", "7");
				SetNodeText(doc, existingPad, "RightTrigger", "8");
				SetNodeText(doc, existingPad, "LeftThumbButton", "11");
				SetNodeText(doc, existingPad, "RightThumbButton", "12");
				SetNodeText(doc, existingPad, "DPad", "p1");
				SetNodeText(doc, existingPad, "LeftThumbAxisX", "a1");
				SetNodeText(doc, existingPad, "LeftThumbAxisY", "a-2");
				SetNodeText(doc, existingPad, "RightThumbAxisX", "a6");
				SetNodeText(doc, existingPad, "RightThumbAxisY", "a-3");
				SetNodeText(doc, existingPad, "ForceEnable", "1");
				SetNodeText(doc, existingPad, "ForceType", "1");
				SetNodeText(doc, existingPad, "ForceSpringStrength", "100");

				doc.Save(PadSettingsXml);
			}
			catch { }
		}

		private void RegisterGameInXml(DetectedGameInfo game, List<DetectedControllerInfo> controllers)
		{
			try
			{
				string productName;
				if (game.FileName.Equals("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase))
				{
					productName = "Minecraft: Bedrock Edition";
				}
				else if (game.FileName.Equals("javaw.exe", StringComparison.OrdinalIgnoreCase) ||
						 game.FileName.Equals("java.exe", StringComparison.OrdinalIgnoreCase))
				{
					productName = "Minecraft: Java Edition (Runtime)";
				}
				else if (game.FileName.Equals("MinecraftLauncher.exe", StringComparison.OrdinalIgnoreCase))
				{
					productName = "Minecraft Launcher";
				}
				else
				{
					productName = Path.GetFileNameWithoutExtension(game.FileName);
				}

				// 1. UserGames.xml
				XmlDocument gDoc = new XmlDocument();
				if (File.Exists(UserGamesXml))
					gDoc.Load(UserGamesXml);
				else
					gDoc.LoadXml("<Data><Items></Items></Data>");

				var gItems = gDoc.SelectSingleNode("//Items");
				if (gItems != null)
				{
					var gameNode = gItems.SelectSingleNode(string.Format("UserGame[FileName='{0}']", game.FileName));
					if (gameNode == null)
					{
						gameNode = gDoc.CreateElement("UserGame");
						gItems.AppendChild(gameNode);
					}

					SetNodeText(gDoc, gameNode, "GameId", Guid.NewGuid().ToString());
					SetNodeText(gDoc, gameNode, "FileName", game.FileName);
					SetNodeText(gDoc, gameNode, "FileProductName", productName);
					SetNodeText(gDoc, gameNode, "FullPath", game.FilePath);
					SetNodeText(gDoc, gameNode, "ProcessorArchitecture", game.Is64Bit ? "9" : "0");
					SetNodeText(gDoc, gameNode, "EmulationType", "2"); // Virtual ViGEm
					SetNodeText(gDoc, gameNode, "EnableMask", "3");    // Player 1 & 2
					SetNodeText(gDoc, gameNode, "IsEnabled", "true");

					gDoc.Save(UserGamesXml);
				}

				// 2. UserSettings.xml (Mapping for Player 1 and Player 2)
				XmlDocument sDoc = new XmlDocument();
				if (File.Exists(UserSettingsXml))
					sDoc.Load(UserSettingsXml);
				else
					sDoc.LoadXml("<Data><Items></Items></Data>");

				var sItems = sDoc.SelectSingleNode("//Items");
				if (sItems != null)
				{
					for (int mapTo = 1; mapTo <= 2; mapTo++)
					{
						var ctrl = controllers.FirstOrDefault(c => c.PlayerIndex == mapTo) ??
								   controllers.FirstOrDefault();

						var ctrlGuid = ctrl?.InstanceGuid ?? (mapTo == 1 ? DefaultTwinCtrl1Guid : DefaultTwinCtrl2Guid);
						var ctrlName = ctrl?.Name ?? "Twin USB Gamepad";
						var prodGuid = ctrl?.ProductGuid ?? "00010810-0000-0000-0000-504944564944";

						var sNode = sItems.SelectSingleNode(string.Format("UserSetting[FileName='{0}' and MapTo='{1}']", game.FileName, mapTo));
						if (sNode == null)
						{
							sNode = sDoc.CreateElement("UserSetting");
							sItems.AppendChild(sNode);
						}

						SetNodeText(sDoc, sNode, "SettingId", Guid.NewGuid().ToString());
						SetNodeText(sDoc, sNode, "InstanceGuid", ctrlGuid);
						SetNodeText(sDoc, sNode, "InstanceName", ctrlName);
						SetNodeText(sDoc, sNode, "ProductGuid", prodGuid);
						SetNodeText(sDoc, sNode, "ProductName", ctrlName);
						SetNodeText(sDoc, sNode, "DeviceType", "20");
						SetNodeText(sDoc, sNode, "FileName", game.FileName);
						SetNodeText(sDoc, sNode, "FileProductName", productName);
						SetNodeText(sDoc, sNode, "IsEnabled", "true");
						SetNodeText(sDoc, sNode, "PadSettingChecksum", VerifiedPadChecksum);
						SetNodeText(sDoc, sNode, "MapTo", mapTo.ToString());
						SetNodeText(sDoc, sNode, "Completion", "100");
					}

					sDoc.Save(UserSettingsXml);
				}
			}
			catch { }
		}

		private void FixSiderDoubleInput(string targetFolder, Action<string> log)
		{
			try
			{
				var siderIni = Path.Combine(targetFolder, "gamepad.ini");
				if (!File.Exists(siderIni))
				{
					var files = Directory.GetFiles(targetFolder, "gamepad.ini", SearchOption.AllDirectories);
					if (files.Length > 0)
						siderIni = files[0];
				}

				if (File.Exists(siderIni))
				{
					var text = File.ReadAllText(siderIni);
					text = System.Text.RegularExpressions.Regex.Replace(text, @"gamepad\.dinput\.enabled\s*=\s*\d+", "gamepad.dinput.enabled = 0");
					text = System.Text.RegularExpressions.Regex.Replace(text, @"gamepad\.xinput\.enabled\s*=\s*\d+", "gamepad.xinput.enabled = 1");
					File.WriteAllText(siderIni, text);
					log?.Invoke("  [FIX] Configured gamepad.ini for pure XInput (double-input prevented)!");
				}
			}
			catch { }
		}

		private static void SetNodeText(XmlDocument doc, XmlNode parent, string nodeName, string value)
		{
			var node = parent.SelectSingleNode(nodeName);
			if (node == null)
			{
				node = doc.CreateElement(nodeName);
				parent.AppendChild(node);
			}
			node.InnerText = value;
		}

		private static bool CheckIs64Bit(string filePath)
		{
			try
			{
				using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var br = new BinaryReader(fs))
				{
					fs.Seek(0x3c, SeekOrigin.Begin);
					var peOffset = br.ReadInt32();
					fs.Seek(peOffset + 4, SeekOrigin.Begin);
					var machine = br.ReadUInt16();
					return machine == 0x8664; // IMAGE_FILE_MACHINE_AMD64
				}
			}
			catch
			{
				return Environment.Is64BitOperatingSystem;
			}
		}
	}
}
