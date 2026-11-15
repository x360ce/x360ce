using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using x360ce.App.DInput;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The Mouse page's demo: a mouse moving on a pad, or a pointer on a screen, and the stick it moves, in the style a stick
	/// group is set to. Each style has a script of its own that pauses at the moment that sets it apart, says in a line under
	/// the picture what happens, and goes through the real <see cref="MouseStick"/>, so the stick does what the real one would.
	/// </summary>
	/// <remarks>
	/// The stick runs with settings of the demo's own, chosen to show the style plainly: the demo explains the style, it does not
	/// preview the group's sliders. Where the group's Active setting keeps the style from doing what the demo shows, a note says
	/// so. It plays once when a style is chosen, unless Windows' Show animations is off, and again on [Play]; otherwise it shows
	/// the script's last moment. It moves only while it plays, on the page shown, in a window that is not minimised. Its stick
	/// is its own, on its own clock: the engine's sticks are never touched.
	/// </remarks>
	public partial class MouseDemoUserControl : UserControl
	{
		public MouseDemoUserControl()
		{
			InitializeComponent();
			// Scaled by the font once, as the stick groups beside it are.
			SuspendLayout();
			AutoScaleMode = AutoScaleMode.Font;
			AutoScaleDimensions = new SizeF(6F, 13F);
			ResumeLayout(false);
			if (ControlsHelper.IsDesignMode(this))
				return;
			Disposed += (s, e) => WatchForm(null);
		}

		const string PlayText = "Play";
		const string StopText = "Stop";

		#region The scripts

		/// <summary>The demo's frames a second, as it is drawn and as its stick is read.</summary>
		public const double FramesPerSecond = 30;

		/// <summary>
		/// One step of a style's script: the mouse goes in a straight line to a place, -1 to 1 across and up and down with up
		/// below nought, over a time, its button held or not, and what happens then, in words.
		/// </summary>
		public sealed class Phase
		{
			public Phase(double seconds, double x, double y, bool pressed, string text)
			{
				Seconds = seconds;
				X = x;
				Y = y;
				Pressed = pressed;
				Text = text;
			}

			public readonly double Seconds;
			public readonly double X, Y;
			public readonly bool Pressed;
			public readonly string Text;
		}

		/// <summary>A style's script: where its mouse starts, and its steps.</summary>
		public sealed class DemoScript
		{
			public DemoScript(double x, double y, params Phase[] phases)
			{
				StartX = x;
				StartY = y;
				Phases = phases;
				Seconds = phases.Sum(p => p.Seconds);
			}

			public readonly double StartX, StartY;
			public readonly Phase[] Phases;
			/// <summary>The script's length.</summary>
			public readonly double Seconds;

			/// <summary>Where the mouse is at a moment, and the step it is in, which says whether its button is held and what happens.</summary>
			public Phase At(double seconds, out double x, out double y)
			{
				var fromX = StartX;
				var fromY = StartY;
				var start = 0.0;
				foreach (var phase in Phases)
				{
					if (seconds < start + phase.Seconds)
					{
						var part = Math.Max(0, (seconds - start) / phase.Seconds);
						x = fromX + (phase.X - fromX) * part;
						y = fromY + (phase.Y - fromY) * part;
						return phase;
					}
					fromX = phase.X;
					fromY = phase.Y;
					start += phase.Seconds;
				}
				x = fromX;
				y = fromY;
				return Phases[Phases.Length - 1];
			}
		}

		/// <summary>Speed: the stick tilts while the mouse moves, and is back at the centre while the mouse rests away from where it started.</summary>
		static readonly DemoScript SpeedScript = new DemoScript(0, 0,
			new Phase(0.5, 0, 0, false, "Mouse rests: stick at the centre"),
			new Phase(0.8, 0.6, 0, false, "Mouse moves right: stick tilts right"),
			new Phase(1.5, 0.6, 0, false, "Mouse stops: stick returns to the centre"),
			new Phase(0.8, -0.2, 0, false, "Mouse moves left: stick tilts left"),
			new Phase(1.5, -0.2, 0, false, "Mouse stops: stick returns to the centre"));

		/// <summary>
		/// Position: the stick follows how far the mouse has moved from where it started, which is not the pad's middle, stays
		/// while the mouse rests, and comes back with it.
		/// </summary>
		static readonly DemoScript PositionScript = new DemoScript(-0.35, 0.25,
			new Phase(0.5, -0.35, 0.25, false, "Mouse rests: stick at the centre"),
			new Phase(0.8, 0.35, -0.25, false, "Mouse moves: stick follows"),
			new Phase(1.5, 0.35, -0.25, false, "Mouse stops: stick stays"),
			new Phase(0.9, -0.35, 0.25, false, "Mouse moves back: stick comes back"),
			new Phase(0.5, -0.35, 0.25, false, "Mouse rests: stick at the centre"));

		/// <summary>Wheel: held, the stick follows the mouse and stays; let go, it glides back to the centre over Back to centre's time.</summary>
		static readonly DemoScript WheelScript = new DemoScript(0, 0,
			new Phase(0.5, 0, 0, false, "Button up: stick at the centre"),
			new Phase(0.5, 0, 0, true, "Button held"),
			new Phase(0.8, 0.6, 0, true, "Button held, mouse moves: stick follows"),
			new Phase(1.2, 0.6, 0, true, "Mouse stops: stick stays while held"),
			new Phase(2.0, 0.6, 0, false, "Button let go: stick glides back"));

		/// <summary>Screen: the stick is where the pointer is from the screen's centre, before the pointer moves, round the centre and through it.</summary>
		static readonly DemoScript ScreenScript = PointerScript();

		static DemoScript PointerScript()
		{
			const string shows = "Stick shows where the pointer is";
			var phases = new List<Phase> { new Phase(1.5, 0.5, 0, false, shows) };
			for (var i = 1; i <= 8; i++)
				phases.Add(new Phase(0.2, 0.5 * Math.Cos(i * Math.PI / 4), -0.5 * Math.Sin(i * Math.PI / 4), false, "Pointer circles the centre: stick too"));
			phases.Add(new Phase(1.0, -0.5, 0, false, "Pointer through the centre: stick too"));
			phases.Add(new Phase(1.5, -0.5, 0, false, shows));
			return new DemoScript(0.5, 0, phases.ToArray());
		}

		/// <summary>The script of a style, which pauses about 1.5 s at the moment that sets the style apart.</summary>
		public static DemoScript ScriptOf(MouseStyle style)
		{
			switch (style)
			{
				case MouseStyle.Speed:
					return SpeedScript;
				case MouseStyle.Wheel:
					return WheelScript;
				case MouseStyle.Screen:
					return ScreenScript;
				default:
					return PositionScript;
			}
		}

		/// <summary>The settings a style's demo runs with, chosen to show the style plainly rather than the group's.</summary>
		public static MouseStickSettings DemoSettings(MouseStyle style)
		{
			switch (style)
			{
				case MouseStyle.Speed:
					// Twice the reach, so a moderate move tilts the stick well.
					return new MouseStickSettings { Style = style, SensitivityX = 200, SensitivityY = 200, SmoothingMs = 60 };
				case MouseStyle.Wheel:
					// Held by the left button, and back to the centre over a time long enough to see.
					return new MouseStickSettings { Style = style, Active = MouseActive.HeldCentre, Button = 1, ReturnMs = 600 };
				case MouseStyle.Screen:
					return new MouseStickSettings { Style = style };
				default:
					// Nothing pulls it back.
					return new MouseStickSettings { Style = MouseStyle.Position };
			}
		}

		/// <summary>A line when a group's Active setting keeps its style from doing what the demo shows, otherwise empty.</summary>
		public static string NoteFor(MouseStickSettings group)
		{
			if (group == null)
				return "";
			if (group.Style == MouseStyle.Wheel && group.Active == MouseActive.Always)
				return "Active is Always here, so this stick never glides back.";
			if (group.Style == MouseStyle.Wheel && group.Active == MouseActive.HeldStay)
				return "Active keeps the stick where it was here, so it never glides back.";
			if (group.Style == MouseStyle.Screen && group.Active != MouseActive.Always)
				return "Turned on by a button here, it counts from the pointer, not the centre.";
			return "";
		}

		/// <summary>A style's script fed to a stick of its own with the demo's settings: the real <see cref="MouseStick"/>, on the demo's clock.</summary>
		public sealed class Run
		{
			public Run(MouseStyle style)
			{
				Script = ScriptOf(style);
				Stick = new MouseStick(DemoSettings(style));
				MouseX = Script.StartX;
				MouseY = Script.StartY;
				// The pointer for Screen is the demo's pointer on a screen of its own.
				Stick.Screen = () => new ScreenPoint { X = (int)Math.Round(MouseX * ScreenHalf), Y = (int)Math.Round(MouseY * ScreenHalf), Half = ScreenHalf };
			}

			const int ScreenHalf = 1000;

			public readonly DemoScript Script;
			public readonly MouseStick Stick;
			readonly SourceState _state = new SourceState();
			public double MouseX, MouseY;
			public bool Pressed;
			/// <summary>What happens now, in words.</summary>
			public string Text = "";
			/// <summary>The stick, as an axis reads: 0 to 65535, the centre in the middle.</summary>
			public ushort StickX = 32768, StickY = 32768;

			/// <summary>Moves the mouse to where the script has it at a moment, and reads the stick.</summary>
			public void Step(double seconds)
			{
				var phase = Script.At(seconds, out MouseX, out MouseY);
				Pressed = phase.Pressed;
				Text = phase.Text;
				var button = Stick.Settings.Button;
				if (button > 0 && button <= _state.Buttons.Length)
					_state.Buttons[button - 1] = Pressed;
				Stick.BeginPass(_state);
				// The script's whole movement is the stick's whole travel in Position at 100 %.
				var ticks = 1 + (long)(seconds * Stopwatch.Frequency);
				StickX = Stick.Value(0, (int)Math.Round(MouseX * MouseStick.PositionCounts), 1, ticks);
				StickY = Stick.Value(1, (int)Math.Round(MouseY * MouseStick.PositionCounts), 1, ticks);
			}
		}

		#endregion

		#region Source, play and the timer

		MouseStickUserControl _source;

		/// <summary>The stick group the demo shows, which it follows as its settings change.</summary>
		public MouseStickUserControl Source
		{
			get { return _source; }
			set
			{
				if (_source == value)
					return;
				if (_source != null)
					_source.SettingsChanged -= Source_SettingsChanged;
				_source = value;
				if (_source != null)
					_source.SettingsChanged += Source_SettingsChanged;
				Restart();
			}
		}

		void Source_SettingsChanged(object sender, EventArgs e)
		{
			Restart();
		}

		Run _run = new Run(MouseStyle.Position);
		/// <summary>The moment of the script shown; its end while the demo rests.</summary>
		double _time;
		readonly Stopwatch _clock = new Stopwatch();
		bool _playing;

		/// <summary>True from Play until the script ends or Play is pressed again.</summary>
		public bool IsPlaying
		{
			get { return _playing; }
		}

		/// <summary>Plays the script once from its start, as when a style is chosen, unless Windows' Show animations is off.</summary>
		public void PlayChosenStyle()
		{
			if (AnimationsOn())
				SetPlaying(true);
		}

		/// <summary>True while the demo moves: playing, on the page shown, in a window that is not minimised.</summary>
		public bool IsRunning
		{
			get { return DemoTimer.Enabled; }
		}

		/// <summary>
		/// A stick of its own in the group's style, brought to the moment shown frame by frame as the timer would, the group's
		/// name and style in words, and a note where the group's Active setting keeps the style from doing what the demo shows.
		/// </summary>
		void Restart()
		{
			var group = _source == null ? null : _source.ReadSettings();
			_run = new Run(group == null ? MouseStyle.Position : group.Style);
			if (!_playing)
				_time = _run.Script.Seconds;
			for (var frame = 0; frame / FramesPerSecond <= _time; frame++)
				_run.Step(frame / FramesPerSecond);
			ControlsHelper.SetText(PhaseLabel, _run.Text);
			var note = NoteFor(group);
			ControlsHelper.SetText(NoteLabel, note);
			if (NoteLabel.Visible != (note.Length > 0))
				NoteLabel.Visible = note.Length > 0;
			if (_source != null)
			{
				// The Style list's words: the style's name, then what it does.
				var text = _source.StyleText ?? "";
				var colon = text.IndexOf(':');
				ControlsHelper.SetText(CaptionLabel, _source.StickName + ": " + (colon < 0 ? text : text.Substring(0, colon)));
				var what = colon < 0 ? "" : text.Substring(colon + 1).Trim();
				ControlsHelper.SetText(DescriptionLabel, what.Length == 0 ? "" : char.ToUpperInvariant(what[0]) + what.Substring(1) + ".");
			}
			DemoPictureBox.Invalidate();
		}

		void PlayButton_Click(object sender, EventArgs e)
		{
			SetPlaying(!_playing);
		}

		/// <summary>Plays the script from its start, or stops it and shows its end.</summary>
		void SetPlaying(bool playing)
		{
			_playing = playing;
			_time = 0;
			PlayButton.Text = playing ? StopText : PlayText;
			Restart();
			UpdateTimer();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			WatchForm(FindForm());
			UpdateTimer();
		}

		Form _form;

		/// <summary>Follows the window the demo is on, which minimises without hiding the demo.</summary>
		void WatchForm(Form form)
		{
			if (form == _form)
				return;
			if (_form != null)
				_form.Resize -= Form_Resize;
			_form = form;
			if (_form != null)
				_form.Resize += Form_Resize;
		}

		void Form_Resize(object sender, EventArgs e)
		{
			UpdateTimer();
		}

		/// <summary>Whether the demo may move now: playing, on the page shown, in a window that is not minimised.</summary>
		bool CanRun()
		{
			return _playing && Visible && _form != null && _form.WindowState != FormWindowState.Minimized;
		}

		void UpdateTimer()
		{
			var run = CanRun();
			if (run == DemoTimer.Enabled)
				return;
			if (run)
				_clock.Restart();
			DemoTimer.Enabled = run;
		}

		void DemoTimer_Tick(object sender, EventArgs e)
		{
			// A page hidden with what holds it tells the controls on it nothing: the next frame finds it and stops.
			if (!CanRun())
			{
				DemoTimer.Enabled = false;
				return;
			}
			// Real time, but never a jump: a tick held up moves the demo no further than a tenth of a second.
			var end = _run.Script.Seconds;
			_time = Math.Min(end, _time + Math.Min(0.1, _clock.Elapsed.TotalSeconds));
			_clock.Restart();
			_run.Step(_time);
			ControlsHelper.SetText(PhaseLabel, _run.Text);
			if (_time >= end)
			{
				// Played through: it rests on the last moment.
				_playing = false;
				PlayButton.Text = PlayText;
				UpdateTimer();
			}
			DemoPictureBox.Invalidate();
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SystemParametersInfo(uint action, uint param, ref bool value, uint winIni);

		const uint SPI_GETCLIENTAREAANIMATION = 0x1042;

		/// <summary>Windows' Show animations setting; on where it cannot be read.</summary>
		static bool AnimationsOn()
		{
			var on = true;
			return !SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref on, 0) || on;
		}

		#endregion

		#region Drawing

		/// <summary>
		/// The pad with its mouse on the left, or in Screen a screen with its centre marked and the pointer on it, an arrow, and
		/// the stick on the right, in the theme's colours.
		/// </summary>
		void DemoPictureBox_Paint(object sender, PaintEventArgs e)
		{
			var g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			var size = DemoPictureBox.ClientSize;
			var side = size.Height;
			var unit = side / 24f;
			var accent = FormsTheme.GetColor("ColorAccent");
			using (var line = new Pen(SystemColors.ControlDark, Math.Max(1f, unit / 2)))
			using (var ink = new Pen(SystemColors.ControlText, Math.Max(1f, unit / 2)))
			using (var field = new SolidBrush(SystemColors.Window))
			using (var face = new SolidBrush(SystemColors.Control))
			using (var lit = new SolidBrush(accent))
			{
				var pad = new RectangleF(unit, unit, side - 2 * unit, side - 2 * unit);
				if (_run.Stick.Settings.Style == MouseStyle.Screen)
					DrawScreen(g, pad, unit, line, ink, field);
				else
					DrawMouse(g, pad, unit, line, ink, field, face, lit);
				// The arrow from the mouse to the stick.
				var y = side / 2f;
				var from = pad.Right + 2 * unit;
				var to = size.Width - side - unit;
				DrawArrow(g, ink, new PointF(from, y), new PointF(to, y), unit);
				// The stick: its circle, its centre, and its dot where the stick is.
				var stick = new RectangleF(size.Width - side + unit, unit, side - 2 * unit, side - 2 * unit);
				g.FillEllipse(field, stick);
				g.DrawEllipse(line, stick);
				var cx = stick.X + stick.Width / 2;
				var cy = stick.Y + stick.Height / 2;
				g.DrawLine(line, cx - unit, cy, cx + unit, cy);
				g.DrawLine(line, cx, cy - unit, cx, cy + unit);
				var dot = stick.Width * 0.18f;
				var reach = (stick.Width - dot) / 2;
				var dx = cx + (float)((_run.StickX - 32767.5) / 32767.5) * reach;
				var dy = cy + (float)((_run.StickY - 32767.5) / 32767.5) * reach;
				g.FillEllipse(lit, dx - dot / 2, dy - dot / 2, dot, dot);
				g.DrawEllipse(ink, dx - dot / 2, dy - dot / 2, dot, dot);
			}
		}

		/// <summary>A screen with its centre marked, and the pointer on it where the script has it: the stick's whole travel is half the screen's height.</summary>
		void DrawScreen(Graphics g, RectangleF area, float unit, Pen line, Pen ink, Brush field)
		{
			var height = area.Height * 0.7f;
			var screen = new RectangleF(area.X, area.Y + (area.Height - height) / 2, area.Width, height);
			g.FillRectangle(field, screen);
			g.DrawRectangle(ink, screen.X, screen.Y, screen.Width, screen.Height);
			// Its stand.
			g.DrawLine(ink, screen.X + screen.Width / 2, screen.Bottom, screen.X + screen.Width / 2, screen.Bottom + unit);
			g.DrawLine(ink, screen.X + screen.Width / 3, screen.Bottom + unit, screen.Right - screen.Width / 3, screen.Bottom + unit);
			var cx = screen.X + screen.Width / 2;
			var cy = screen.Y + screen.Height / 2;
			g.DrawLine(line, cx - 2 * unit, cy, cx + 2 * unit, cy);
			g.DrawLine(line, cx, cy - 2 * unit, cx, cy + 2 * unit);
			// The pointer, its tip where the pointer is.
			var px = cx + (float)_run.MouseX * screen.Height / 2;
			var py = cy + (float)_run.MouseY * screen.Height / 2;
			var s = unit * 0.6f;
			var arrow = new[] { new PointF(0, 0), new PointF(0, 8), new PointF(2, 6), new PointF(3.5f, 9), new PointF(5, 8.5f), new PointF(3.5f, 5.5f), new PointF(6, 5.5f) }
				.Select(p => new PointF(px + p.X * s, py + p.Y * s)).ToArray();
			using (var black = new SolidBrush(SystemColors.ControlText))
			using (var edge = new Pen(SystemColors.Window, Math.Max(1f, unit / 4)))
			{
				g.FillPolygon(black, arrow);
				g.DrawPolygon(edge, arrow);
			}
		}

		/// <summary>The pad, the mouse on it where the script has it with its button lit while held, and an arrow the way it moves.</summary>
		void DrawMouse(Graphics g, RectangleF pad, float unit, Pen line, Pen ink, Brush field, Brush face, Brush lit)
		{
			g.FillRectangle(field, pad);
			g.DrawRectangle(line, pad.X, pad.Y, pad.Width, pad.Height);
			var mouseWidth = pad.Width * 0.2f;
			var mouseHeight = mouseWidth * 1.5f;
			// It travels short of the pad's edges by the room its arrow takes, a gap and a line, so the arrow stays on the pad.
			var arrowGap = unit / 2;
			var arrowLength = 3 * unit;
			var arrowRoom = arrowGap + arrowLength;
			var travelX = (pad.Width - mouseWidth - 2 * arrowRoom) / 2;
			var travelY = (pad.Height - mouseHeight - 2 * arrowRoom) / 2;
			// Where the mouse started, so it is seen to rest away from there.
			var sx = pad.X + pad.Width / 2 + (float)_run.Script.StartX * travelX;
			var sy = pad.Y + pad.Height / 2 + (float)_run.Script.StartY * travelY;
			g.DrawLine(line, sx - unit, sy, sx + unit, sy);
			g.DrawLine(line, sx, sy - unit, sx, sy + unit);
			var mx = pad.X + pad.Width / 2 + (float)_run.MouseX * travelX - mouseWidth / 2;
			var my = pad.Y + pad.Height / 2 + (float)_run.MouseY * travelY - mouseHeight / 2;
			var mouse = new RectangleF(mx, my, mouseWidth, mouseHeight);
			g.FillEllipse(face, mouse);
			if (_run.Pressed)
			{
				// The button the stick uses, lit while held: the right one for button 2, the left one for any other.
				var right = _run.Stick.Settings.Button == 2;
				g.FillPie(lit, mouse.X, mouse.Y, mouse.Width, mouse.Width, right ? 270 : 180, 90);
			}
			g.DrawEllipse(ink, mouse);
			g.DrawLine(ink, mouse.X, mouse.Y + mouse.Width / 2, mouse.Right, mouse.Y + mouse.Width / 2);
			g.DrawLine(ink, mouse.X + mouse.Width / 2, mouse.Y, mouse.X + mouse.Width / 2, mouse.Y + mouse.Width / 2);
			// While the mouse moves, an arrow beside it the way it goes.
			double x0, y0, x1, y1;
			_run.Script.At(_time, out x0, out y0);
			_run.Script.At(_time + 0.05, out x1, out y1);
			var vx = (float)(x1 - x0);
			var vy = (float)(y1 - y0);
			var length = (float)Math.Sqrt(vx * vx + vy * vy);
			if (_playing && length > 1e-4)
			{
				vx /= length;
				vy /= length;
				var mcx = mouse.X + mouse.Width / 2;
				var mcy = mouse.Y + mouse.Height / 2;
				// From the mouse's edge the way it goes.
				var ax = vx / (mouse.Width / 2);
				var ay = vy / (mouse.Height / 2);
				var edge = (float)(1 / Math.Sqrt(ax * ax + ay * ay));
				var start = new PointF(mcx + vx * (edge + arrowGap), mcy + vy * (edge + arrowGap));
				var tip = new PointF(start.X + vx * arrowLength, start.Y + vy * arrowLength);
				DrawArrow(g, ink, start, tip, unit);
			}
		}

		/// <summary>A line from one point to another with its head at the second.</summary>
		static void DrawArrow(Graphics g, Pen pen, PointF from, PointF to, float unit)
		{
			g.DrawLine(pen, from, to);
			var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
			var head = 2 * unit;
			foreach (var side in new[] { -0.6, 0.6 })
				g.DrawLine(pen, to, new PointF(to.X - head * (float)Math.Cos(angle + side), to.Y - head * (float)Math.Sin(angle + side)));
		}

		#endregion
	}
}
