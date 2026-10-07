using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Collections.Generic;

namespace x360ce.App.Issues
{
	/// <summary>The x360ce skill installed for AI agents by an older program, which would describe pages and tools this version has changed.</summary>
	/// <remarks>
	/// Silent when no skill is installed: installing one is the person's choice, made on the Options tab's AI page.
	/// A copy a newer program wrote is left alone too. Low, because the program works the same either way; only an
	/// agent's advice drifts.
	/// </remarks>
	public class AiSkillIssue : IssueItem
	{
		public AiSkillIssue() : base()
		{
			Name = "AI skill";
			FixName = "Update";
		}

		static bool IsOlder(string folder)
		{
			Version installed;
			return AiSkill.Check(folder, AiSkill.ProgramVersion, out installed) == AiSkill.State.Older;
		}

		public override void CheckTask()
		{
			var older = new List<string>();
			if (IsOlder(AiSkill.ClaudeFolder))
				older.Add("Claude Code");
			if (IsOlder(AiSkill.AgentsFolder))
				older.Add("other agents");
			if (older.Count == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			SetSeverity(IssueSeverity.Low, 1, "The x360ce skill installed for " + string.Join(" and ", older) +
				" is older than this program, so an AI agent may describe pages and tools that have changed.");
		}

		public override void FixTask()
		{
			foreach (var folder in new[] { AiSkill.ClaudeFolder, AiSkill.AgentsFolder })
				if (IsOlder(folder))
					AiSkill.Install(folder);
		}
	}
}
