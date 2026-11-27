using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.Mcp;
using System;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace x360ce.App.Issues
{
	/// <summary>The x360ce skill installed for AI agents by an older program, which would describe pages and tools this version has changed.</summary>
	/// <remarks>
	/// Silent when no skill is installed: installing one is the person's choice, made on the Options tab's AI page.
	/// A copy a newer program wrote is left alone too. Low, because the program works the same either way; only an
	/// agent's advice drifts.
	/// </remarks>
	public class AiSkillIssue : OnChangeIssue
	{
		/// <inheritdoc/>
		public override void SetTexts()
		{
			Name = UiLanguage.T("AiSkillIssue.AISkill", "AI skill");
			FixName = UiLanguage.T("AiSkillIssue.Update", "Update");
		}

		static bool IsOlder(string folder)
		{
			Version installed;
			return AiSkill.Check(folder, AiSkill.ProgramVersion, out installed) == AiSkill.State.Older;
		}

		public override void CheckTask()
		{
			var claude = IsOlder(AiSkill.ClaudeFolder);
			var agents = IsOlder(AiSkill.AgentsFolder);
			if (!claude && !agents)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			// One whole sentence for each case, so each language can say it its own way.
			var text = claude && agents
				? UiLanguage.T("AiSkillIssue.OlderForBoth", "The x360ce skill installed for Claude Code and other agents is older than this program, so an AI agent may describe pages and tools that have changed.")
				: claude
					? UiLanguage.T("AiSkillIssue.OlderForClaude", "The x360ce skill installed for Claude Code is older than this program, so an AI agent may describe pages and tools that have changed.")
					: UiLanguage.T("AiSkillIssue.OlderForAgents", "The x360ce skill installed for other agents is older than this program, so an AI agent may describe pages and tools that have changed.");
			SetSeverity(IssueSeverity.Low, 1, text);
		}

		public override void FixTask()
		{
			foreach (var folder in new[] { AiSkill.ClaudeFolder, AiSkill.AgentsFolder })
				if (IsOlder(folder))
					AiSkill.Install(folder);
		}
	}
}
