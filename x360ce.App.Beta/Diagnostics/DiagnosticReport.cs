using JocysCom.ClassLibrary.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using x360ce.App.DInput;
using x360ce.App.Issues;

namespace x360ce.App.Diagnostics
{
	public static class DiagnosticReport
	{
		public static string CreateCurrent()
		{
			var runtimeDetector = new CppRuntimeDetector();
			var x86 = runtimeDetector.Detect(CppRuntimeArchitecture.X86);
			var x64 = runtimeDetector.Detect(CppRuntimeArchitecture.X64);
			var bus = Nefarius.ViGEm.Client.ViGEmClient.GetBusHealth();
			var controllers = Global.DHelper?.GetControllerHealth() ?? new ControllerPipelineHealth[0];
			var events = ReadRecentEvents(OperationalLog.Current?.CurrentFilePath, 200);
			return Build(
				new AssemblyInfo().Version.ToString(),
				Environment.OSVersion.VersionString,
				x86, x64, bus, controllers, events);
		}

		public static string Build(
			string appVersion,
			string operatingSystem,
			CppRuntimeDetectionResult x86,
			CppRuntimeDetectionResult x64,
			ViGEmBusHealthResult bus,
			IEnumerable<ControllerPipelineHealth> controllers,
			IEnumerable<string> recentEvents)
		{
			var text = new StringBuilder();
			text.AppendLine("x360ce diagnostics");
			text.AppendLine("Generated UTC: " + DateTime.UtcNow.ToString("O"));
			text.AppendLine("Application: " + (appVersion ?? "Unknown"));
			text.AppendLine("Operating system: " + (operatingSystem ?? "Unknown"));
			text.AppendLine("Process architecture: " + (Environment.Is64BitProcess ? "x64" : "x86"));
			text.AppendLine();
			AppendRuntime(text, "x86", x86);
			AppendRuntime(text, "x64", x64);
			text.AppendLine();
			text.AppendLine("ViGEm installed: " + YesNo(bus?.Installed == true));
			text.AppendLine("ViGEm service: " + (bus?.ServiceState.ToString() ?? "Unknown"));
			text.AppendLine("ViGEm driver running: " + YesNo(bus?.DriverRunning == true));
			text.AppendLine("ViGEm API/client: " + (bus?.ClientConnectionState.ToString() ?? "Unknown"));
			text.AppendLine("ViGEm compatible: " + YesNo(bus != null && !bus.VersionIncompatible));
			text.AppendLine("ViGEm usable: " + YesNo(bus?.IsUsable == true));
			if (!string.IsNullOrWhiteSpace(bus?.ErrorMessage))
				text.AppendLine("ViGEm error: " + bus.ErrorMessage);

			var health = controllers?.ToArray() ?? new ControllerPipelineHealth[0];
			for (var i = 0; i < health.Length; i++)
			{
				var item = health[i] ?? new ControllerPipelineHealth();
				text.AppendLine();
				text.AppendLine("Controller slot " + (i + 1) + ":");
				text.AppendLine("  Physical input OK: " + YesNo(item.PhysicalInputOk));
				text.AppendLine("  Mapping OK: " + YesNo(item.MappingOk));
				text.AppendLine("  Virtual bus OK: " + YesNo(item.VirtualBusOk));
				text.AppendLine("  Virtual target connected: " + YesNo(item.VirtualTargetConnected));
				text.AppendLine("  State submit OK: " + YesNo(item.StateSubmitOk));
				if (!string.IsNullOrWhiteSpace(item.LastError))
					text.AppendLine("  Last error: " + item.LastError);
			}

			text.AppendLine();
			text.AppendLine("Recent sanitized operational events (JSON lines):");
			foreach (var line in recentEvents ?? Enumerable.Empty<string>())
			{
				if (IsSafeOperationalEvent(line))
					text.AppendLine(line);
			}
			return text.ToString();
		}

		static void AppendRuntime(StringBuilder text, string architecture, CppRuntimeDetectionResult result)
		{
			if (result == null)
			{
				text.AppendLine("VC++ " + architecture + ": Unknown");
				return;
			}
			var state = result.IsInstalled ? "Installed" : result.IsApplicable ? "Not detected" : "Not applicable";
			var version = result.Version == null ? string.Empty : ", " + result.Version;
			var source = result.RegistryView == 0 ? string.Empty : ", " + result.RegistryView;
			text.AppendLine("VC++ " + architecture + ": " + state + version + source);
			if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
				text.AppendLine("VC++ " + architecture + " error: " + result.ErrorMessage);
		}

		static IEnumerable<string> ReadRecentEvents(string path, int maximum)
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
				return Enumerable.Empty<string>();
			try
			{
				var queue = new Queue<string>(maximum);
				foreach (var line in File.ReadLines(path))
				{
					if (queue.Count == maximum)
						queue.Dequeue();
					queue.Enqueue(line);
				}
				return queue.ToArray();
			}
			catch (Exception ex)
			{
				return new[] { "Could not read current log: " + ex.Message };
			}
		}

		static string YesNo(bool value) => value ? "Yes" : "No";

		static bool IsSafeOperationalEvent(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
				return false;
			// Copy only events whose schemas contain allowlisted stage/status data.
			// Exception messages/stacks and raw device identifiers remain in the local
			// rotating file and are deliberately excluded from the clipboard report.
			var safeEvents = new[]
			{
				"application_session_started",
				"startup_stage_started",
				"startup_stage_completed",
				"startup_stage_slow",
				"startup_window_shown",
				"ui_dispatcher_started",
				"main_window_loaded",
				"main_window_shown",
				"main_window_minimized",
				"main_window_close_minimized",
				"main_window_close_exit",
				"cpp_runtime_detected",
				"vigem_health_detected",
				"dinput_worker_started",
				"dinput_worker_stopped",
				"dinput_enumeration_completed",
				"device_notification",
				"controller_poll_frequency",
				"controller_pipeline_health_changed",
			};
			return safeEvents.Any(name =>
				line.IndexOf("\"event\":\"" + name + "\"", StringComparison.Ordinal) >= 0);
		}
	}
}
