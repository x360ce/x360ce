using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.App.Plugins;

namespace x360ce.App.Issues
{
	/// <summary>Plugins that did not load, or that were switched off after they failed.</summary>
	/// <remarks>
	/// A DLL put into the plugins folder that then does nothing would leave the person guessing, so each one that did
	/// not load says why here, as on the Plugins page. Information only, with no button: letting a plugin from someone
	/// else load is the person's choice, made on the Plugins page.
	/// </remarks>
	class PluginIssue : IssueItem
	{
		public PluginIssue() : base()
		{
			Name = "Plugins";
			FixName = null;
		}

		public override void CheckTask()
		{
			var manager = PluginManager.Current;
			var problems = manager == null ? new string[0] : Problems(manager);
			if (problems.Length == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			SetSeverity(IssueSeverity.Low, 0,
				(problems.Length == 1 ? "A plugin did not load or was switched off." : problems.Length + " plugins did not load or were switched off.")
				+ " The Plugins page on the Options tab lists every plugin and why."
				+ Environment.NewLine + Environment.NewLine
				+ string.Join(Environment.NewLine, problems.Select(x => "    " + x).ToArray()));
		}

		/// <summary>One line for each plugin that did not load, loaded with nothing to offer, or was switched off.</summary>
		public static string[] Problems(PluginManager manager)
		{
			var list = new List<string>();
			foreach (var file in manager.Files)
			{
				if (file.State != PluginFileState.Loaded || file.Reason != null)
					list.Add(file.Name + ": " + file.Reason);
				foreach (var type in file.DeviceTypes.Where(x => x.Faulted))
					list.Add(file.Name + ", " + type.Name + ": switched off after it failed: " + type.FaultReason);
			}
			return list.ToArray();
		}
	}
}
