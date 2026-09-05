// @under-test: App.v4/Controls/OptionsUserControl.cs, App.v4/MainForm.cs, README.MD
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>Where a person switches an assistant on, what they are shown to register it, and where an AI reader is told to look.</summary>
	[TestClass]
	public class AiAccessOptionsPageTest
	{
		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The page carries the level list, the port in the remote port's range, the token and a snippet naming this program and the /Mcp switch")]
		public void Options_page_offers_the_controls_and_a_snippet()
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new OptionsUserControl())
				{
					page.CreateControl();
					foreach (var name in new[] { "AiAccessComboBox", "AiAccessTokenTextBox", "AiAccessRegenerateButton", "AiAccessCopyButton" })
						Assert.AreEqual(1, page.Controls.Find(name, true).Length, name + " is missing.");
					var group = page.Controls.Find("AiAccessGroupBox", true).OfType<GroupBox>().First();
					Assert.AreEqual("AI assistant access", group.Text);
					// Typed text would leave the list's selection empty and the binding would write Off.
					var level = page.Controls.Find("AiAccessComboBox", true).OfType<ComboBox>().First();
					Assert.AreEqual(ComboBoxStyle.DropDownList, level.DropDownStyle);
					var port = page.Controls.Find("AiAccessPortNumericUpDown", true).OfType<NumericUpDown>().First();
					Assert.AreEqual(1024, port.Minimum);
					Assert.AreEqual(49151, port.Maximum);
					// The door's own controls, which McpTools refuses at every level, are these three.
					McpUiToolsTest.AssertField("AiAccessComboBox", typeof(ComboBox));
					McpUiToolsTest.AssertField("AiAccessPortNumericUpDown", typeof(NumericUpDown));
					McpUiToolsTest.AssertField("AiAccessRegenerateButton", typeof(Button));
					CollectionAssert.AreEquivalent(new[] { "AiAccessComboBox", "AiAccessPortNumericUpDown", "AiAccessRegenerateButton" }, x360ce.App.Mcp.McpTools.DoorControls);
					var snippet = page.Controls.Find("AiAccessSnippetTextBox", true).OfType<TextBox>().First();
					StringAssert.Contains(snippet.Text, "\"/Mcp\"");
					StringAssert.Contains(snippet.Text, Application.ExecutablePath.Replace("\\", "\\\\"));
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The README tells an AI reader where the latest facts live")]
		public void Readme_points_an_ai_reader_at_the_sources()
		{
			var readme = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "README.MD"));
			foreach (var pointer in new[] { "docs/ui-tree.md", "docs/Help.v4.md", "AGENTS.md", "/Mcp", "/Ai" })
				StringAssert.Contains(readme, pointer, "README does not point at " + pointer);
		}
	}
}
