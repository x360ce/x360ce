// @under-test: App.v4/Controls/PadControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The mapping menu of a controller page offers [Invert] beside [Record], for a box whose control has
	/// another way round.
	/// </summary>
	[TestClass]
	public class MappingMenuInvertTest
	{
		static UserDevice Radio()
		{
			return new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				CapButtonCount = 4,
				DiAxeMask = 0x1 | 0x2 | 0x4 | 0x8,
				DiSliderMask = 0x1,
			};
		}

		static ToolStripItem Invert(PadControl page)
		{
			return page.DiMenuStrip.Items.Cast<ToolStripItem>().FirstOrDefault(x => x.Text == "[Invert]");
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The mapping menu has [Invert] right after [Record], and nothing to invert without a device")]
		public void The_menu_offers_invert_after_record()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				{
					form.Controls.Add(page);
					page.ResetDiMenuStrip(Radio());
					var texts = page.DiMenuStrip.Items.Cast<ToolStripItem>().Select(x => x.Text).ToList();
					Assert.AreEqual(texts.IndexOf("[Record]") + 1, texts.IndexOf("[Invert]"),
						"[Invert] is not beside [Record]: " + string.Join(", ", texts));
					page.ResetDiMenuStrip(null);
					Assert.IsNull(Invert(page), "With no device there is nothing to invert.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("[Invert] shows only for a box holding a button, an axis or a slider, and shows again after a box without one")]
		public void Invert_shows_only_where_there_is_another_way_round()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				{
					form.Controls.Add(page);
					page.ResetDiMenuStrip(Radio());
					// Alternating hide/show pairs, so a ShowInvertFor that only ever hides the item cannot
					// pass: each "shown" assertion follows a box that must have hidden it first.
					var pairs = new[] { "POV 1", "Slider 1", "=s1", "ISlider 1", "POV 1 Up", "Button 3", "", "IButton 3", "POV 1", "HAxis 2" };
					for (var i = 0; i < pairs.Length; i += 2)
					{
						using (var hidden = new ComboBox { Text = pairs[i] })
						{
							page.ShowInvertFor(hidden);
							Assert.IsFalse(Invert(page).Available, "[Invert] is offered for '" + pairs[i] + "'.");
						}
						using (var shown = new ComboBox { Text = pairs[i + 1] })
						{
							page.ShowInvertFor(shown);
							Assert.IsTrue(Invert(page).Available, "[Invert] stays hidden for '" + pairs[i + 1] + "' after a box without one.");
						}
					}
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Choosing [Invert] swaps the box's text the other way round, and back on a second click")]
		public void Invert_click_swaps_the_box_text_and_back()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				using (var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList })
				{
					form.Controls.Add(box);
					form.Controls.Add(page);
					page.ResetDiMenuStrip(Radio());
					SettingsManager.Current.SetComboBoxValue(box, "Slider 1");
					var targetField = typeof(PadControl).GetField("MenuTargetCbx", BindingFlags.NonPublic | BindingFlags.Instance);
					targetField.SetValue(page, box);
					page.ShowInvertFor(box);
					((ToolStripMenuItem)Invert(page)).PerformClick();
					Assert.AreEqual("ISlider 1", box.Text, "Choosing [Invert] did not swap the box the other way round.");
					targetField.SetValue(page, box);
					page.ShowInvertFor(box);
					((ToolStripMenuItem)Invert(page)).PerformClick();
					Assert.AreEqual("Slider 1", box.Text, "Choosing [Invert] again did not swap it back.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The D-Pad menu hides [Invert], and the next ordinary box hides it again for a D-Pad-shaped text")]
		public void D_pad_menu_hides_invert_and_restores_it()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				{
					form.Controls.Add(page);
					page.ResetDiMenuStrip(Radio());
					page.EnableDPadMenu(true);
					Assert.IsFalse(Invert(page).Available, "[Invert] is offered while the D-Pad menu is open.");
					page.EnableDPadMenu(false);
					using (var box = new ComboBox { Text = "POV 1" })
					{
						page.ShowInvertFor(box);
						Assert.IsFalse(Invert(page).Available, "[Invert] is offered for 'POV 1' after the D-Pad menu closes.");
					}
				}
			});
		}
	}
}
