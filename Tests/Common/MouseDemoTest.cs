// @under-test: App.v4/Controls/MouseDemoUserControl.cs, App.v4/Common/DInput/MouseStick.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// The Mouse page's demo: each style's script fed to the real <see cref="MouseStick"/> shows what sets that style apart,
	/// and no other style's; and it moves nothing while its page is not shown.
	/// </summary>
	[TestClass]
	public class MouseDemoTest
	{
		static readonly MouseStyle[] Styles = { MouseStyle.Speed, MouseStyle.Position, MouseStyle.Wheel, MouseStyle.Screen };

		/// <summary>One frame of a demo: the mouse, its button, the stick, -1 to 1, and the line under the picture.</summary>
		sealed class Frame
		{
			public double Seconds, MouseX, MouseY, StickX, StickY;
			public bool Pressed;
			public string Text;
			/// <summary>How long the mouse and its button have been as they are now.</summary>
			public double Resting;
			public double Stick { get { return Math.Sqrt(StickX * StickX + StickY * StickY); } }
		}

		/// <summary>A style's demo played through at its 30 frames a second, as the timer plays it.</summary>
		static List<Frame> Record(MouseStyle style)
		{
			var run = new MouseDemoUserControl.Run(style);
			var frames = new List<Frame>();
			for (var i = 0; i / MouseDemoUserControl.FramesPerSecond <= run.Script.Seconds + 1e-9; i++)
			{
				var t = i / MouseDemoUserControl.FramesPerSecond;
				run.Step(t);
				var last = frames.LastOrDefault();
				var same = last != null && Math.Abs(last.MouseX - run.MouseX) < 1e-9 && Math.Abs(last.MouseY - run.MouseY) < 1e-9 && last.Pressed == run.Pressed;
				frames.Add(new Frame
				{
					Seconds = t,
					MouseX = run.MouseX,
					MouseY = run.MouseY,
					Pressed = run.Pressed,
					StickX = (run.StickX - 32767.5) / 32767.5,
					StickY = (run.StickY - 32767.5) / 32767.5,
					Text = run.Text,
					Resting = same ? last.Resting + 1 / MouseDemoUserControl.FramesPerSecond : 0,
				});
			}
			return frames;
		}

		/// <summary>Frames the mouse has rested in long enough for any style's stick to settle.</summary>
		static IEnumerable<Frame> Rests(List<Frame> frames)
		{
			return frames.Where(x => x.Resting >= 0.3);
		}

		static double Distance(double x1, double y1, double x2, double y2)
		{
			return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
		}

		/// <summary>
		/// What sets each style apart, as a demo shows it, or why it does not: null when it does.
		/// </summary>
		static string Shows(MouseStyle style, List<Frame> frames)
		{
			var rests = Rests(frames).ToList();
			var first = frames[0];
			switch (style)
			{
				case MouseStyle.Speed:
					// The stick tilts while the mouse moves, and is at the centre whenever the mouse rests.
					var away = rests.FirstOrDefault(x => x.Stick > 0.05);
					if (away != null)
						return string.Format("at {0:0.00} s the mouse rests and the stick is at {1:0.00}, not the centre", away.Seconds, away.Stick);
					return frames.Any(x => x.Stick > 0.3) ? null : "the stick never tilts while the mouse moves";
				case MouseStyle.Position:
					// The stick is as far from the centre as the mouse is from where it started, and stays there while it rests.
					var off = rests.FirstOrDefault(x => Distance(x.StickX, x.StickY, x.MouseX - first.MouseX, x.MouseY - first.MouseY) > 0.05);
					if (off != null)
						return string.Format("at {0:0.00} s the stick is not as far as the mouse moved", off.Seconds);
					return rests.Any(x => x.Stick > 0.3) ? null : "the stick never stays away from the centre while the mouse rests";
				case MouseStyle.Screen:
					// The stick is where the pointer is from the screen's centre, away from it before the pointer has moved at all.
					var still = rests.FirstOrDefault(x => Distance(x.MouseX, x.MouseY, first.MouseX, first.MouseY) < 1e-9);
					if (still == null || still.Stick < 0.3)
						return "before the pointer moves the stick is at the centre, not where the pointer is";
					var apart = rests.FirstOrDefault(x => Distance(x.StickX, x.StickY, x.MouseX, x.MouseY) > 0.05);
					if (apart != null)
						return string.Format("at {0:0.00} s the stick is not where the pointer is", apart.Seconds);
					return rests.Any(x => x.Stick > 0.3) ? null : "the stick is never away from the centre while the pointer rests";
				default:
					// Held, the stick stays while the mouse rests; let go, it glides back to the centre over time, and gets there.
					if (!rests.Any(x => x.Pressed && x.Stick > 0.3))
						return "the stick does not stay away from the centre while the button is held and the mouse rests";
					for (var i = 1; i < frames.Count; i++)
					{
						if (!frames[i - 1].Pressed || frames[i].Pressed)
							continue;
						var held = frames[i - 1].Stick;
						var later = frames.First(x => x.Seconds >= frames[i].Seconds + 0.2).Stick;
						if (later < held * 0.2 || later > held * 0.9)
							return string.Format("0.2 s after the button is let go the stick is at {0:0.00} of {1:0.00}: it does not glide back", later, held);
					}
					return frames.Last().Stick < 0.05 ? null : "the stick does not get back to the centre once the button is let go";
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Each style's demo shows what sets its style apart and nothing another style does: Speed at the centre whenever the mouse rests, Position as far as the mouse moved and staying there, Wheel staying while held and gliding back once let go, Screen where the pointer is from the screen's centre")]
		public void Each_demo_shows_its_own_style_and_no_other()
		{
			var faults = new List<string>();
			foreach (var demo in Styles)
			{
				var frames = Record(demo);
				foreach (var style in Styles)
				{
					var why = Shows(style, frames);
					if (style == demo && why != null)
						faults.Add(demo + "'s demo does not show " + style + ": " + why + ".");
					if (style != demo && why == null)
						faults.Add(demo + "'s demo shows what " + style + " does too, so the two cannot be told apart.");
				}
			}
			Assert.AreEqual(0, faults.Count, string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("No two styles' demos move the stick the same way")]
		public void No_two_demos_give_the_same_stick_path()
		{
			var paths = Styles.ToDictionary(x => x, Record);
			foreach (var a in Styles)
				foreach (var b in Styles.Where(x => x > a))
				{
					var count = Math.Min(paths[a].Count, paths[b].Count);
					var most = Enumerable.Range(0, count).Max(i => Distance(paths[a][i].StickX, paths[a][i].StickY, paths[b][i].StickX, paths[b][i].StickY));
					Assert.IsTrue(most > 0.3, a + " and " + b + " move the stick the same way: at most " + most.ToString("0.00") + " apart.");
				}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Wheel's stick glides back after the button is let go visibly slower than Speed's goes back when the mouse stops")]
		public void Wheel_glides_back_slower_than_speed_returns()
		{
			Func<List<Frame>, Func<Frame, Frame, bool>, double> after = (frames, when) =>
			{
				var i = Enumerable.Range(1, frames.Count - 1).First(x => when(frames[x - 1], frames[x]));
				var start = frames[i - 1].Stick;
				return frames.First(x => x.Seconds >= frames[i].Seconds + 0.2).Stick / start;
			};
			var speed = after(Record(MouseStyle.Speed), (a, b) => a.Stick > 0.3 && Math.Abs(a.MouseX - b.MouseX) < 1e-9);
			var wheel = after(Record(MouseStyle.Wheel), (a, b) => a.Pressed && !b.Pressed);
			Assert.IsTrue(speed < 0.1, "0.2 s after the mouse stops Speed's stick is still at " + speed.ToString("0.00") + " of its tilt.");
			Assert.IsTrue(wheel > 0.5, "0.2 s after the button is let go Wheel's stick is at " + wheel.ToString("0.00") + " of where it was: it does not glide.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Each demo pauses about 1.5 s at the moment that sets its style apart, and the line under the picture says what happens")]
		public void Each_demo_pauses_and_says_what_happens()
		{
			foreach (var style in Styles)
			{
				var script = MouseDemoUserControl.ScriptOf(style);
				Assert.IsTrue(script.Phases.Any(x => x.Seconds >= 1.5), style + "'s demo has no pause of 1.5 s.");
				Assert.IsTrue(script.Phases.All(x => !string.IsNullOrWhiteSpace(x.Text)), style + "'s demo has a step that says nothing.");
			}
			var lines = Styles.ToDictionary(x => x, x => Record(x).Select(f => f.Text).Distinct().ToList());
			StringAssert.Contains(string.Join("|", lines[MouseStyle.Speed]), "stick returns");
			StringAssert.Contains(string.Join("|", lines[MouseStyle.Position]), "stick stays");
			StringAssert.Contains(string.Join("|", lines[MouseStyle.Wheel]), "glides back");
			StringAssert.Contains(string.Join("|", lines[MouseStyle.Screen]), "where the pointer is");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The demo says so where the group's Active setting keeps the style from doing what it shows: a Wheel always active, or kept where it was, never glides back")]
		public void A_note_says_when_the_groups_setting_differs()
		{
			Assert.AreNotEqual("", MouseDemoUserControl.NoteFor(new MouseStickSettings { Style = MouseStyle.Wheel, Active = MouseActive.Always }));
			Assert.AreNotEqual("", MouseDemoUserControl.NoteFor(new MouseStickSettings { Style = MouseStyle.Wheel, Active = MouseActive.HeldStay }));
			Assert.AreEqual("", MouseDemoUserControl.NoteFor(new MouseStickSettings { Style = MouseStyle.Wheel, Active = MouseActive.HeldCentre, Button = 1 }));
			Assert.AreEqual("", MouseDemoUserControl.NoteFor(new MouseStickSettings { Style = MouseStyle.Position }));
			Assert.AreEqual("", MouseDemoUserControl.NoteFor(new MouseStickSettings { Style = MouseStyle.Speed }));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The demo rests until Play, and its timer runs only while it plays on a page that is shown and enabled, in a window that is not minimised; Play while it plays stops it")]
		public void The_timer_is_off_while_the_page_is_hidden()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var page = new Panel { Dock = DockStyle.Fill };
					var demo = new MouseDemoUserControl();
					page.Controls.Add(demo);
					form.Controls.Add(page);
					form.Show();
					Application.DoEvents();
					Assert.IsFalse(demo.IsRunning, "Shown, the demo moves before a style is chosen or Play is pressed.");
					demo.PlayButton.PerformClick();
					Assert.IsTrue(demo.IsRunning, "Playing on a page shown, the demo does not move.");
					page.Visible = false;
					// Within a frame: the controls on a page hidden are not told, so the next frame stops.
					var until = DateTime.UtcNow.AddSeconds(1);
					while (demo.IsRunning && DateTime.UtcNow < until)
						Application.DoEvents();
					Assert.IsFalse(demo.IsRunning, "The demo moves while its page is hidden.");
					page.Visible = true;
					Application.DoEvents();
					Assert.IsTrue(demo.IsRunning, "Shown again, the demo does not move.");
					// The page disables its controls when the controller has no device.
					page.Enabled = false;
					Application.DoEvents();
					Assert.IsFalse(demo.IsRunning, "The demo moves while the page's controls are disabled.");
					page.Enabled = true;
					Application.DoEvents();
					Assert.IsTrue(demo.IsRunning, "Enabled again, the demo does not move.");
					form.WindowState = FormWindowState.Minimized;
					Application.DoEvents();
					Assert.IsFalse(demo.IsRunning, "The demo moves while its window is minimised.");
					form.WindowState = FormWindowState.Normal;
					demo.PlayButton.PerformClick();
					Application.DoEvents();
					Assert.IsFalse(demo.IsRunning, "Paused, the demo moves.");
					form.Close();
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		[Description("In the main window at its smallest size, in the program's font and both themes, the Mouse page shows the demo under its two stick groups, whole, with its picture, caption and Play button")]
		public void The_mouse_page_shows_the_demo_under_the_sticks()
		{
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(new PadControl(x360ce.Engine.MapTo.Controller1), "Controller 1") }, (window, when) =>
			{
				window.MinimumSize = PadPageShortWindowTest.MinimumWindowSize();
				window.Size = window.MinimumSize;
				Application.DoEvents();
				var page = PadPageShortWindowTest.Descendants(window).OfType<PadControl>().Single();
				page.PadTabControl.SelectedTab = page.MouseTabPage;
				Application.DoEvents();
				var demo = page.MouseDemoPanel;
				var tab = page.MouseTabPage;
				var at = tab.RectangleToClient(demo.Parent.RectangleToScreen(demo.Bounds));
				var groups = tab.RectangleToClient(page.LeftMouseStickPanel.Parent.RectangleToScreen(page.LeftMouseStickPanel.Bounds));
				var line = string.Format("{0}: the demo is at {1} on a tab {2}, the stick group at {3}, its picture {4}.", when, at, tab.ClientSize, groups, demo.DemoPictureBox.Size);
				Assert.IsTrue(demo.Visible && at.Width > 0 && at.Height > 0 && demo.DemoPictureBox.Width > 0 && demo.DemoPictureBox.Height > 0, "The demo is not shown. " + line);
				Assert.IsTrue(at.Top >= groups.Bottom && at.Bottom <= tab.ClientSize.Height, "The demo is not whole under the stick groups. " + line);
			});
		}
	}
}
