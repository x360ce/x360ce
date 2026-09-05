using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace x360ce.Setup
{
	public partial class SetupEngine
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
