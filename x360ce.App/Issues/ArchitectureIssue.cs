using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using x360ce.Engine;
using System.Linq;
using System.Windows.Forms;

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

		public override void Check()
		{
			var architectures = new Dictionary<string, ProcessorArchitecture>();
			var architecture = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
			var exes = System.IO.Directory.GetFiles(".", "*.exe", System.IO.SearchOption.TopDirectoryOnly);
			foreach (var exe in exes)
			{
				var pa = Engine.Win32.PEReader.GetProcessorArchitecture(exe);
				architectures.Add(exe, pa);
			}
			var fi = new FileInfo(Application.ExecutablePath);
			// Select all architectures of executables.
			var archs = architectures.Where(x => !x.Key.ToLower().Contains("x360ce")).Select(x => x.Value).ToArray();
			var x86Count = archs.Count(x => x == ProcessorArchitecture.X86);
			var x64Count = archs.Count(x => x == ProcessorArchitecture.Amd64);
			// If executables are 32-bit, but this program is 64-bit then warn user.
			if (x86Count > 0 && x64Count == 0 && architecture == ProcessorArchitecture.Amd64)
			{
				Description = "This folder contains 32-bit game. You should use 32-bit X360CE Application:\r\n" +
				"http://www.x360ce.com/Files/x360ce.zip";
				_architecture = ProcessorArchitecture.X86;
				FixName = "Download";
				Severity = IssueSeverity.Moderate;
				return;
			}
			// If executables are 64-bit, but this program is 32-bit then warn user.
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
				EngineHelper.OpenUrl("http://www.x360ce.com/Files/x360ce.zip");
			}
			else
			{
				EngineHelper.OpenUrl("http://www.x360ce.com/Files/x360ce_x64.zip");
			}
		}

	}
}
