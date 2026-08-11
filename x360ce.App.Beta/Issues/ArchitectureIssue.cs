using System.Collections.Generic;
using System.Reflection;
using x360ce.Engine;
using System.Linq;
using JocysCom.ClassLibrary.Win32;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	public class ArchitectureIssue : IssueItem
	{
		public ArchitectureIssue() : base()
		{
			Name = "Architecture";
			FixName = "Open Releases";
		}

		// Check file only once.
		bool CheckFile = true;

		public override void CheckTask()
		{
			var required = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Library);
			if (!required)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var architectures = new Dictionary<string, ProcessorArchitecture>();
			var architecture = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
			var exes = EngineHelper.GetFiles(".", "*.exe");
			// Exclude x360ce files.
			exes = exes.Where(x => !x.ToLower().Contains("x360ce")).ToArray();
			// If single executable was found then...
			if (exes.Length == 1 && CheckFile)
			{
				CheckFile = false;
				// Update current settings file.
				ControlsHelper.Invoke(() =>
				{
					Global._MainWindow.UserProgramsPanel.ListPanel.ProcessExecutable(exes[0]);
				});
			}
			foreach (var exe in exes)
			{
				var pa = PEReader.GetProcessorArchitecture(exe);
				architectures.Add(exe, pa);
			}
			// Select all architectures of executables.
			var archs = architectures.Select(x => x.Value).ToArray();
			var x86Count = archs.Count(x => x == ProcessorArchitecture.X86);
			var x64Count = archs.Count(x => x == ProcessorArchitecture.Amd64);
			// If executables are 32-bit, but this program is 64-bit then...
			if (x86Count > 0 && x64Count == 0 && architecture == ProcessorArchitecture.Amd64)
			{
				SetSeverity(
					IssueSeverity.Moderate, 1,
					"This folder contains a 32-bit game. Select a maintained 32-bit build from the releases page."
				);
				return;
			}
			// If executables are 64-bit, but this program is 32-bit then...
			if (x64Count > 0 && x86Count == 0 && architecture == ProcessorArchitecture.X86)
			{
				SetSeverity(
					IssueSeverity.Moderate, 2,
					"This folder contains a 64-bit game. Select a maintained 64-bit build from the releases page."
				);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void FixTask()
		{
			ControlsHelper.OpenUrl("https://github.com/x360ce/x360ce/releases");
		}

	}
}
