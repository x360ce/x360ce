// @under-test: App.v4/Forms/TestDeviceForm.cs, App.v4/Forms/TestDeviceViews.cs
// @area: devices   @layer: ui
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Forms;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>The Test Device window following a scripted gamepad, step by step, without the engine.</summary>
	[TestClass]
	public class DeviceTestWindowTest
	{
		[TestMethod, TestCategory("ui")]
		[Description("In the light and the dark theme, the window follows a scripted gamepad through every step: the step in its header, the instruction, the countdown and Start under it, a view for each stick and axis that fills, the buttons ticked, then the overall grade and the 7 cards without scrolling, and Details whole")]
		public void The_window_follows_a_scripted_device_through_every_step()
		{
			Ui.OnUiThread(() =>
			{
				try
				{
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						Follow(theme.ToString().ToLowerInvariant());
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			}, "Following a scripted device");
		}

		static void Follow(string theme)
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			using (var form = new TestDeviceForm { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ShowInTaskbar = false })
			{
				form.Show();
				Show(form, theme, "0-idle");
				Assert.AreEqual("Press Start to test the controller.", form.InstructionLabel.Text);
				var start = form.StartButton.Parent.PointToScreen(form.StartButton.Location);
				var instruction = form.InstructionLabel.Parent.PointToScreen(new Point(form.InstructionLabel.Left, form.InstructionLabel.Bottom));
				Assert.IsTrue(start.Y > instruction.Y && start.X - instruction.X < form.Width / 4, "Start is not under the instruction.");
				form.Steps = s.Steps;
				s.Start();
				Show(form, theme, "1-get-ready");
				Assert.AreEqual("Put the controller on the desk and let go.", form.InstructionLabel.Text);
				Assert.AreEqual("10", form.CountdownLabel.Text);
				Assert.AreEqual("Stop", form.StartButton.AccessibleName);
				StringAssert.StartsWith(form.Controls.Find("StepsView", true).Single().AccessibleName, "1. Get ready, now");
				s.Run(10100);
				Show(form, theme, "2-rest");
				Assert.AreEqual("Measuring, don't touch.", form.InstructionLabel.Text);
				s.Run(5000);
				s.Run(5000);
				s.Run(4000, s.Circle(0, 1));
				s.Rest();
				s.Run(1200, s.Circle(3, 4));
				Show(form, theme, "4-sticks");
				Assert.IsTrue(form.SkipButton.Visible, "Skip is not beside Stop while the test waits on the sticks.");
				var views = form.ControlsFlowLayoutPanel.Controls.Cast<Control>().Select(x => x.AccessibleName).ToArray();
				Assert.AreEqual(3, views.Length, string.Join("; ", views));
				Assert.AreEqual("Left stick (X Axis, Y Axis): \u2713 full", views[0]);
				s.Rest();
				s.Run(4000, s.Circle(3, 4));
				s.Rest();
				s.Run(4000, s.Sweep(2));
				s.Rest();
				s.Run(200);
				s.Press(0);
				s.Press(1);
				Show(form, theme, "5-buttons");
				Assert.AreEqual("Press each button once.", form.InstructionLabel.Text);
				s.Press(2);
				s.Press(3);
				Show(form, theme, "6-results");
				var cards = form.CardsFlowLayoutPanel.Controls.Cast<Control>().Select(x => x.AccessibleName).ToArray();
				Assert.AreEqual(1 + DeviceTestGrades.Criteria.Length, cards.Length);
				StringAssert.StartsWith(cards[0], "Overall grade: Best.");
				Assert.IsFalse(form.InstructionLabel.Visible, "The results repeat the header's \"Results\" in a heading.");
				Assert.AreEqual("Test again", form.StartButton.AccessibleName);
				Assert.IsFalse(form.SkipButton.Visible, "Skip shows once the test is over.");
				Assert.IsTrue(form.ButtonsHeadingLabel.Visible);
				var content = form.CardsFlowLayoutPanel.Parent.Parent;
				Assert.IsTrue(form.ButtonsHeadingLabel.Bottom <= content.ClientSize.Height, "The results do not fit without scrolling.");
				Assert.IsTrue(form.ResultsDataGridView.Visible, "The details table is not shown under the cards.");
				Assert.AreEqual(ScrollBars.None, form.ResultsDataGridView.ScrollBars, "The details table does not fit the window.");
				((ScrollableControl)form.CardsFlowLayoutPanel.Parent.Parent).ScrollControlIntoView(form.ResultsDataGridView);
				Show(form, theme, "7-details");
			}
		}

		/// <summary>The folder a picture of each step is saved in, to be looked at, when the environment names one.</summary>
		static readonly string PictureFolder = System.Environment.GetEnvironmentVariable("X360CE_TEST_PICTURES");

		/// <summary>Shows the steps as they are, lets the window lay out, and saves a picture of it when <see cref="PictureFolder"/> is set.</summary>
		static void Show(TestDeviceForm form, string theme, string name)
		{
			form.ShowSteps();
			Application.DoEvents();
			if (string.IsNullOrEmpty(PictureFolder))
				return;
			Directory.CreateDirectory(PictureFolder);
			using (var bitmap = new Bitmap(form.Width, form.Height))
			{
				form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
				bitmap.Save(Path.Combine(PictureFolder, "test-device-window-" + theme + "-" + name + ".png"), ImageFormat.Png);
			}
		}
	}
}
