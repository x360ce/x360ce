using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using x360ce.Engine;
using System.Linq;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Win32;
using JocysCom.ClassLibrary.Controls;

namespace x360ce.App.Issues
{
	public class ArchitectureIssue : WarningItem
	{
		public ArchitectureIssue()
		{
			Name = "Architecture";
			FixName = "Download";
			Description = "";
		}

		// Check file only once.
		bool CheckFile = true;

		public override void Check()
		{
			var architectures = new Dictionary<string, ProcessorArchitecture>();
			var architecture = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
			var exes = AppHelper.GetFiles(".", "*.exe");
			// Exclude x360ce files.
			exes = exes.Where(x => !x.ToLower().Contains("x360ce")).ToArray();
			// If single executable was found then...
			if (exes.Length == 1 && CheckFile)
			{
				CheckFile = false;
				// Update current settings file.
				MainForm.Current.Invoke(new Action(() =>
				{
					MainForm.Current.GameSettingsPanel.ProcessExecutable(exes[0]);
				}));
			}
			foreach (var exe in exes)
			{
				var pa = PEReader.GetProcessorArchitecture(exe);
				architectures.Add(exe, pa);
			}
			var fi = new FileInfo(Application.ExecutablePath);
			// Select all architectures of executables.
			var archs = architectures.Select(x => x.Value).ToArray();
			var x86Count = archs.Count(x => x == ProcessorArchitecture.X86);
			var x64Count = archs.Count(x => x == ProcessorArchitecture.Amd64);
			// If executables are 32-bit, but this program is 64-bit then...
			if (x86Count > 0 && x64Count == 0 && architecture == ProcessorArchitecture.Amd64)
			{
				Description = "This folder contains 32-bit game. You should use 32-bit X360CE Application:\r\n" +
				"http://www.x360ce.com/Files/x360ce.zip";
				_architecture = ProcessorArchitecture.X86;
				FixName = "Download";
				Severity = IssueSeverity.Moderate;
				return;
			}
			// If executables are 64-bit, but this program is 32-bit then...
			if (x64Count > 0 && x86Count == 0 && architecture == ProcessorArchitecture.X86)
			{
				Description = "This folder contains 64-bit game. You should use 64-bit X360CE Application:\r\n" +
				"http://www.x360ce.com/Files/x360ce_x64.zip";
				_architecture = ProcessorArchitecture.Amd64;
				FixName = "Download";
				Severity = IssueSeverity.Moderate;
				return;
			}
			Severity = IssueSeverity.None;
		}

		ProcessorArchitecture _architecture;

		public override void Fix()
		{
			if (_architecture == ProcessorArchitecture.X86)
			{
				ControlsHelper.OpenUrl("http://www.x360ce.com/Files/x360ce.zip");
			}
			else
			{
				ControlsHelper.OpenUrl("http://www.x360ce.com/Files/x360ce_x64.zip");
			}
			RaiseFixApplied();
		}

	}
}
