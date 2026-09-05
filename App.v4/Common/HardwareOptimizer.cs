using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	/// <summary>
	/// Automatically detects PC hardware capabilities (CPU cores, RAM, Architecture, Power source)
	/// and applies optimal performance settings (1000Hz polling rate, thread priority, buffer sizes, etc.).
	/// </summary>
	public static class HardwareOptimizer
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private class MEMORYSTATUSEX
		{
			public uint dwLength;
			public uint dwMemoryLoad;
			public ulong ullTotalPhys;
			public ulong ullAvailPhys;
			public ulong ullTotalPageFile;
			public ulong ullAvailPageFile;
			public ulong ullTotalVirtual;
			public ulong ullAvailVirtual;
			public ulong ullAvailExtendedVirtual;

			public MEMORYSTATUSEX()
			{
				dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
			}
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
		private static extern uint timeBeginPeriod(uint uMilliseconds);

		/// <summary>
		/// Gets total physical memory in gigabytes.
		/// </summary>
		public static double GetTotalMemoryGb()
		{
			try
			{
				var memStatus = new MEMORYSTATUSEX();
				if (GlobalMemoryStatusEx(memStatus))
				{
					return Math.Round((double)memStatus.ullTotalPhys / (1024 * 1024 * 1024), 1);
				}
			}
			catch { }
			return 8.0; // Default fallback
		}

		/// <summary>
		/// Logical CPU core count.
		/// </summary>
		public static int CpuCores => Environment.ProcessorCount;

		/// <summary>
		/// Whether the system is running on battery power.
		/// </summary>
		public static bool IsOnBattery => SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline;

		/// <summary>
		/// Summary of auto-detected PC hardware.
		/// </summary>
		public static string HardwareSummary { get; private set; } = string.Empty;

		/// <summary>
		/// Applies optimal settings automatically according to PC hardware.
		/// </summary>
		public static void AutoOptimize(bool force = false)
		{
			try
			{
				var cores = CpuCores;
				var ramGb = GetTotalMemoryGb();
				var onBattery = IsOnBattery;
				var is64Bit = Environment.Is64BitOperatingSystem;

				var o = SettingsManager.Options;

				// Determine optimal polling frequency
				UpdateFrequency optimalRate;
				ProcessPriorityClass optimalProcessPriority;

				if (cores >= 6 && ramGb >= 7.5 && !onBattery)
				{
					// High-tier Gaming PC / Desktop
					optimalRate = UpdateFrequency.ms1_1000Hz; // 1000 Hz, 1ms latency
					optimalProcessPriority = ProcessPriorityClass.AboveNormal;
				}
				else if (cores >= 4 && !onBattery)
				{
					// Mid-tier PC
					optimalRate = UpdateFrequency.ms1_1000Hz; // 1000 Hz if AC powered
					optimalProcessPriority = ProcessPriorityClass.AboveNormal;
				}
				else
				{
					// Economy / Battery saving
					optimalRate = UpdateFrequency.ms2_500Hz; // 500 Hz
					optimalProcessPriority = ProcessPriorityClass.Normal;
				}

				// Apply settings
				o.PollingRate = optimalRate;
				o.AllowOnlyOneCopy = true;
				o.MinimizeToTray = true;
				o.AutoDetectForegroundWindow = true;
				o.ExcludeVirtualDevices = true;
				o.ExcludeSupplementalDevices = false;

				// Elevate current process priority for lag-free gaming
				try
				{
					using (var currentProcess = Process.GetCurrentProcess())
					{
						currentProcess.PriorityClass = optimalProcessPriority;
					}
				}
				catch { }

				// If DHelper exists, update frequency immediately
				if (Global.DHelper != null)
				{
					Global.DHelper.Frequency = optimalRate;
				}

				// Request 1ms high precision timer resolution for ultra-low latency DirectInput polling
				try { timeBeginPeriod(1); } catch { }

				// Reduce garbage collection pauses during controller polling
				try
				{
					GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
				}
				catch { }

				// Auto scan and register installed games across system drives
				AutoScanAndRegisterGames();

				HardwareSummary = string.Format("⚡ Optimized for your PC: {0} CPU Cores | {1:F0} GB RAM | {2} Ultra-Low Latency",
					cores, ramGb, optimalRate == UpdateFrequency.ms1_1000Hz ? "1000Hz (1ms)" : "500Hz (2ms)");

				Trace.TraceInformation(HardwareSummary);
			}
			catch (Exception ex)
			{
				Trace.TraceError("HardwareOptimizer failed: {0}", ex);
			}
		}

		/// <summary>
		/// Automatically detects installed games folders, Minecraft (Bedrock & Java), and registers game executables with Virtual ViGEm controller emulation.
		/// </summary>
		public static void AutoScanAndRegisterGames()
		{
			try
			{
				var o = SettingsManager.Options;
				if (o.GameScanLocations == null)
					o.GameScanLocations = new System.ComponentModel.BindingList<string>();

				DriveInfo[] drives;
				try
				{
					drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();
				}
				catch
				{
					drives = new DriveInfo[0];
				}

				var libraryRelPaths = new[]
				{
					"Games",
					"Game",
					@"SteamLibrary\steamapps\common",
					@"Steam\steamapps\common",
					@"Program Files (x86)\Steam\steamapps\common",
					@"Program Files\Steam\steamapps\common",
					"Epic Games",
					@"Program Files\Epic Games",
					@"GOG Galaxy\Games",
					@"GOG Games",
					"XboxGames",
					@"EA Games",
					@"Origin Games",
					@"Ubisoft\Ubisoft Game Launcher\games",
					@"Ubisoft Game Launcher\games",
					"Battle.net",
					@"Amazon Games",
					@"Riot Games",
					"Emulators"
				};

				foreach (var drive in drives)
				{
					var root = drive.RootDirectory.FullName;
					foreach (var rel in libraryRelPaths)
					{
						var full = Path.Combine(root, rel);
						if (Directory.Exists(full) && !o.GameScanLocations.Contains(full))
						{
							o.GameScanLocations.Add(full);
						}
					}
				}

				// Exclude setup utilities, redistributables, launcher helpers, and uninstaller executables
				var excludedExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

				var registeredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				void RegisterExe(string exePath, string customProductName = null)
				{
					if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath) || registeredFiles.Contains(exePath))
						return;

					var fileName = Path.GetFileName(exePath);
					if (excludedExes.Contains(fileName) ||
						fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase) ||
						fileName.StartsWith("vcredist", StringComparison.OrdinalIgnoreCase))
					{
						return;
					}

					registeredFiles.Add(exePath);

					var game = SettingsManager.ProcessExecutable(exePath);
					if (game != null)
					{
						game.EmulationType = (int)EmulationType.Virtual;
						game.EnableMask = 15; // Enable virtual controllers (1 | 2 | 4 | 8)
						game.IsEnabled = true;
						if (!string.IsNullOrEmpty(customProductName))
						{
							game.FileProductName = customProductName;
						}
					}
				}

				// Dedicated Minecraft Auto-Discovery (Bedrock Edition & Java Edition runtimes)
				try
				{
					// 1. Bedrock Edition (UWP Package via Registry)
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
												RegisterExe(exe, "Minecraft: Bedrock Edition");
										}
									}
								}
							}
						}
					}

					// 2. Java Edition (Official Store Launcher Runtimes)
					var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					var packagesDir = Path.Combine(localAppData, "Packages");
					if (Directory.Exists(packagesDir))
					{
						foreach (var pkg in Directory.GetDirectories(packagesDir, "Microsoft.4297127D64EC6*"))
						{
							var runtimeDir = Path.Combine(pkg, @"LocalCache\Local\runtime");
							if (Directory.Exists(runtimeDir))
							{
								foreach (var jw in Directory.GetFiles(runtimeDir, "javaw.exe", SearchOption.AllDirectories).Take(5))
									RegisterExe(jw, "Minecraft: Java Edition (Runtime)");
							}
						}
					}

					// 3. Java Edition (Standard .minecraft runtime)
					var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					var dotMcRuntime = Path.Combine(appData, @".minecraft\runtime");
					if (Directory.Exists(dotMcRuntime))
					{
						foreach (var jw in Directory.GetFiles(dotMcRuntime, "javaw.exe", SearchOption.AllDirectories).Take(5))
							RegisterExe(jw, "Minecraft: Java Edition (Runtime)");
					}

					// 4. Third-Party Launchers and JREs
					var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
					var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
					var progFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
					var thirdPartyMcPaths = new[]
					{
						Path.Combine(appData, "PrismLauncher"),
						Path.Combine(appData, "ModrinthApp"),
						Path.Combine(localAppData, @"Programs\Modrinth App"),
						Path.Combine(userProfile, @"curseforge\minecraft\Install"),
						Path.Combine(userProfile, @".lunarclient\jre"),
						Path.Combine(appData, "badlion-client"),
						Path.Combine(appData, ".feather"),
						Path.Combine(appData, "MultiMC"),
						Path.Combine(progFiles86, @"Minecraft Launcher\runtime"),
						Path.Combine(progFiles, @"Minecraft Launcher\runtime"),
						Path.Combine(localAppData, @"Programs\Minecraft Launcher\runtime")
					};

					foreach (var tp in thirdPartyMcPaths)
					{
						if (Directory.Exists(tp))
						{
							foreach (var jw in Directory.GetFiles(tp, "javaw.exe", SearchOption.AllDirectories).Take(5))
								RegisterExe(jw, "Minecraft: Java Edition (Runtime)");
						}
					}
				}
				catch { }

				// Scan standard game library locations
				foreach (var location in o.GameScanLocations.ToArray())
				{
					if (!Directory.Exists(location))
						continue;

					try
					{
						var files = Directory.GetFiles(location, "*.exe", SearchOption.AllDirectories);
						foreach (var file in files.Take(100))
						{
							RegisterExe(file);
						}
					}
					catch { }
				}

				SettingsManager.UserGames.Save();
			}
			catch (Exception ex)
			{
				Trace.TraceError("AutoScanAndRegisterGames failed: {0}", ex);
			}
		}

		/// <summary>
		/// Automatically maps any connected online controller (Twin USB, PS4/PS5, Switch, Wheels)
		/// to the next available Controller slot (1, 2, 3, 4) with 100% correct presets.
		/// </summary>
		public static void AutoConfigureConnectedControllers()
		{
			try
			{
				var game = SettingsManager.CurrentGame;
				if (game == null)
				{
					game = SettingsManager.UserGames.Items.FirstOrDefault(x => x.IsEnabled);
					if (game != null)
						SettingsManager.CurrentGame = game;
				}

				if (game == null)
					return;

				game.EmulationType = (int)EmulationType.Virtual;
				if (game.EnableMask == 0)
					game.EnableMask = 15;

				var devices = SettingsManager.UserDevices.Items
					.Where(x => x.IsOnline &&
						!(x.InstanceName ?? "").Contains("vJoy") &&
						!(x.InstanceName ?? "").Contains("Virtual") &&
						!(x.ProductName ?? "").Contains("Virtual"))
					.ToList();

				if (devices.Count == 0)
					return;

				var mappedSlots = new HashSet<MapTo>();
				var existingSettings = SettingsManager.GetSettings(game.FileName).Where(x => x.IsEnabled).ToList();
				foreach (var s in existingSettings)
				{
					mappedSlots.Add((MapTo)s.MapTo);
				}

				var availableSlots = new[] { MapTo.Controller1, MapTo.Controller2, MapTo.Controller3, MapTo.Controller4 }
					.Where(slot => !mappedSlots.Contains(slot))
					.ToList();

				var newlyMapped = false;
				foreach (var ud in devices)
				{
					var setting = SettingsManager.GetSetting(ud.InstanceGuid, game.FileName);
					if (setting == null && availableSlots.Count > 0)
					{
						var slot = availableSlots[0];
						availableSlots.RemoveAt(0);

						SettingsManager.MapGamePadDevices(game, slot, new[] { ud }, SettingsManager.Options.HidGuardianConfigureAutomatically);
						newlyMapped = true;
					}
				}

				if (newlyMapped)
				{
					SettingsManager.UserSettings.Save();
					SettingsManager.PadSettings.Save();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("AutoConfigureConnectedControllers failed: {0}", ex);
			}
		}
	}
}
