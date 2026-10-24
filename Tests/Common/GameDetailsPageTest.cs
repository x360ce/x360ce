// @under-test: App.v4/Controls/GameDetailsUserControl.cs, App.v4/Controls/GameDetailsUserControl.Designer.cs
// @area: options-layout   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// The settings of a game, under the list on the Games page: groups laid out in tables, each as big as what it holds,
	/// at every zoom.
	/// </summary>
	[TestClass]
	public class GameDetailsPageTest
	{
		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("At every zoom each group holds its controls whole, nothing covers anything, the boxes of a row of other options stand level, and a number box keeps the margin every other control has")]
		public void Groups_hold_their_controls_whole_at_every_zoom()
		{
			Ui.OnUiThread(() =>
			{
				FormsTheme.SetTheme(ThemeType.Light);
				// The zooms Windows offers; the settings were drawn at 100 %.
				foreach (var zoom in new[] { 1f, 1.25f, 1.5f, 2f })
					using (var form = new Form { ClientSize = new Size(2000, 900), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
					{
						var details = new GameDetailsUserControl { Dock = DockStyle.Bottom };
						form.Controls.Add(PageAt(details, zoom));
						FormsTheme.Apply(form);
						form.Show();
						foreach (var group in Descendants(details).OfType<GroupBox>())
						{
							var inside = group.DisplayRectangle;
							foreach (var control in Descendants(group).Where(x => !(x is TableLayoutPanel) && x.Parent is TableLayoutPanel))
							{
								var bounds = group.RectangleToClient(control.Parent.RectangleToScreen(control.Bounds));
								Assert.IsTrue(inside.Contains(bounds), string.Format(
									"At {0:P0} {1} {2} is cut by {3}, which holds {4}.", zoom, control.Name, bounds, group.Name, inside));
							}
						}
						var covered = ControlOverlapTest.Covered(details).ToList();
						Assert.AreEqual(0, covered.Count, string.Format("At {0:P0}:{1}{2}",
							zoom, Environment.NewLine, string.Join(Environment.NewLine, covered)));
						foreach (var row in new[]
						{
							new[] { "HookModeFakeVidNumericUpDown", "HookModeFakeVidTextBox", "HookModeFakePidNumericUpDown", "HookModeFakePidTextBox", "TimeoutNumericUpDown" },
							new[] { "XInputPathTextBox", "ProcessorArchitectureComboBox" },
							new[] { "DInputFileTextBox", "EmulationTypeComboBox" },
						})
						{
							var tops = row.Select(x => details.Controls.Find(x, true).Single().Top).Distinct().ToList();
							Assert.AreEqual(1, tops.Count, string.Format(
								"At {0:P0} {1} stand at {2}.", zoom, string.Join(", ", row), string.Join(", ", tops)));
						}
						// Windows Forms scales the margin of a number box, a container itself, again for each table round it
						// while the settings scale themselves.
						var margin = details.Controls.Find("HookCOMCheckBox", true).Single().Margin;
						foreach (var box in Descendants(details).OfType<NumericUpDown>())
							Assert.AreEqual(margin, box.Margin, string.Format("At {0:P0} {1} has a margin of its own.", zoom, box.Name));
					}
			});
		}

		/// <summary>
		/// The settings held as the Games page holds them: a page drawn at 100 % that scales what it holds from its font, which
		/// is how a screen's zoom reaches the controls.
		/// </summary>
		static UserControl PageAt(GameDetailsUserControl details, float zoom)
		{
			var page = new UserControl { Dock = DockStyle.Fill };
			page.SuspendLayout();
			page.Font = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * zoom);
			page.Controls.Add(details);
			page.AutoScaleDimensions = new SizeF(6F, 13F);
			page.AutoScaleMode = AutoScaleMode.Font;
			page.ResumeLayout(false);
			return page;
		}

		/// <summary>Every control below this one, at any depth.</summary>
		static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandChild in Descendants(child))
					yield return grandChild;
			}
		}
	}
}
