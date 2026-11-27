using JocysCom.ClassLibrary.Controls.UiTree;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
	/// the script's last moment. It moves only while it plays, on the page shown, in a window that is not minimised, and while
	/// the page's controls are enabled; disabled with them, it is drawn faded. Its stick
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
			EventHandler languageChanged = (s, e) => ShowWords();
			UiLanguage.Changed += languageChanged;
			Disposed += (s, e) =>
			{
				UiLanguage.Changed -= languageChanged;
				WatchForm(null);
			};
			UpdatePlayButton();
		}

		void UpdatePlayButton()
		{
			PlayButton.Text = _playing ? UiLanguage.T("MouseDemoUserControl.Stop", "Stop") : UiLanguage.T("MouseDemoUserControl.Play", "Play");
		}

		#region The scripts

		/// <summary>The demo's frames a second, as it is drawn and as its stick is read.</summary>
		public const double FramesPerSecond = 30;

		/// <summary>
		/// One step of a style's script: the mouse goes in a straight line to a place, -1 to 1 across and up and down with up
		/// below nought, over a time, its button held or not, and what happens then, in words.
		/// </summary>
		public sealed class Phase
		{
			public Phase(double seconds, double x, double y, bool pressed, Func<string> text)
			{
				Seconds = seconds;
				X = x;
				Y = y;
				Pressed = pressed;
				_text = text;
			}

			readonly Func<string> _text;
			public readonly double Seconds;
			public readonly double X, Y;
			public readonly bool Pressed;
			public string Text { get { return _text(); } }
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
				Extent = phases.Select(p => Math.Max(Math.Abs(p.X), Math.Abs(p.Y))).Concat(new[] { Math.Max(Math.Abs(x), Math.Abs(y)) }).Max();
			}

			public readonly double StartX, StartY;
			public readonly Phase[] Phases;
			/// <summary>The script's length.</summary>
			public readonly double Seconds;
			/// <summary>How far from the middle the mouse ever goes, across or up and down.</summary>
			public readonly double Extent;

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
			new Phase(1.0, 0, 0, false, () => UiLanguage.T("MouseDemoUserControl.MouseRests", "Mouse rests: stick at the centre")),
			new Phase(1.2, 0.6, 0, false, () => UiLanguage.T("MouseDemoUserControl.MouseMovesRight", "Mouse moves right: stick tilts right")),
			new Phase(1.5, 0.6, 0, false, () => UiLanguage.T("MouseDemoUserControl.StickReturns", "Mouse stops: stick returns to the centre")),
			new Phase(1.2, -0.2, 0, false, () => UiLanguage.T("MouseDemoUserControl.MouseMovesLeft", "Mouse moves left: stick tilts left")),
			new Phase(1.5, -0.2, 0, false, () => UiLanguage.T("MouseDemoUserControl.StickReturns", "Mouse stops: stick returns to the centre")));

		/// <summary>Position: the stick follows how far the mouse has moved from where it started, stays while the mouse rests, and comes back with it.</summary>
		static readonly DemoScript PositionScript = new DemoScript(0, 0,
			new Phase(1.0, 0, 0, false, () => UiLanguage.T("MouseDemoUserControl.MouseRests", "Mouse rests: stick at the centre")),
			new Phase(1.2, 0.6, -0.4, false, () => UiLanguage.T("MouseDemoUserControl.StickFollowsMouse", "Mouse moves: stick follows")),
			new Phase(1.5, 0.6, -0.4, false, () => UiLanguage.T("MouseDemoUserControl.StickStays", "Mouse stops: stick stays")),
			new Phase(1.35, 0, 0, false, () => UiLanguage.T("MouseDemoUserControl.StickFollowsMouseBack", "Mouse moves back: stick comes back")),
			new Phase(1.0, 0, 0, false, () => UiLanguage.T("MouseDemoUserControl.MouseRests", "Mouse rests: stick at the centre")));

		/// <summary>Wheel: held, the stick follows the mouse and stays; let go, it glides back to the centre over Back to centre's time.</summary>
		static readonly DemoScript WheelScript = new DemoScript(0, 0,
			new Phase(1.0, 0, 0, false, () => UiLanguage.T("MouseDemoUserControl.ButtonUp", "Button up: stick at the centre")),
			new Phase(1.0, 0, 0, true, () => UiLanguage.T("MouseDemoUserControl.ButtonHeld", "Button held")),
			new Phase(1.2, 0.6, 0, true, () => UiLanguage.T("MouseDemoUserControl.StickFollowsMouseWhileHeld", "Button held, mouse moves: stick follows")),
			new Phase(1.5, 0.6, 0, true, () => UiLanguage.T("MouseDemoUserControl.StickStaysWhileHeld", "Mouse stops: stick stays while held")),
			new Phase(2.0, 0.6, 0, false, () => UiLanguage.T("MouseDemoUserControl.ButtonReleased", "Button let go: stick glides back")));

		/// <summary>Screen: the stick is where the pointer is from the screen's centre, before the pointer moves, round the centre and through it.</summary>
		static readonly DemoScript ScreenScript = PointerScript();

		static DemoScript PointerScript()
		{
			Func<string> shows = () => UiLanguage.T("MouseDemoUserControl.StickShowsPointer", "Stick shows where the pointer is");
			var phases = new List<Phase> { new Phase(1.5, 0.5, 0, false, shows) };
			for (var i = 1; i <= 8; i++)
				phases.Add(new Phase(0.3, 0.5 * Math.Cos(i * Math.PI / 4), -0.5 * Math.Sin(i * Math.PI / 4), false, () => UiLanguage.T("MouseDemoUserControl.PointerCirclesCentre", "Pointer circles the centre: stick too")));
			phases.Add(new Phase(1.5, -0.5, 0, false, () => UiLanguage.T("MouseDemoUserControl.PointerThroughCentre", "Pointer through the centre: stick too")));
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
					// Three times the reach, so the demo's unhurried move tilts the stick well.
					return new MouseStickSettings { Style = style, SensitivityX = 300, SensitivityY = 300, SmoothingMs = 60 };
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
				return UiLanguage.T("MouseDemoUserControl.AlwaysActive", "Active is Always here, so this stick never glides back.");
			if (group.Style == MouseStyle.Wheel && group.Active == MouseActive.HeldStay)
				return UiLanguage.T("MouseDemoUserControl.ActiveKeepsStick", "Active keeps the stick where it was here, so it never glides back.");
			if (group.Style == MouseStyle.Screen && group.Active != MouseActive.Always)
				return UiLanguage.T("MouseDemoUserControl.PointerWithButton", "Turned on by a button here, it counts from the pointer, not the centre.");
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
			public string Text { get { return _phase?.Text ?? ""; } }

			Phase _phase;
			/// <summary>The stick, as an axis reads: 0 to 65535, the centre in the middle.</summary>
			public ushort StickX = 32768, StickY = 32768;
			/// <summary>
			/// The rodent's walk: its heading in radians on the screen, up at the start and then the way it last went; where its legs
			/// are in their cycle, which turns with the distance walked; its pace, 1 while it walks and easing to 0 when it stops, so
			/// legs, tail and ears settle; and how far its tail trails behind a turn, in radians.
			/// </summary>
			public double Heading = -Math.PI / 2, Stride, Pace, Lag;
			/// <summary>The way the rodent last went, which it turns to.</summary>
			double _way = -Math.PI / 2;
			/// <summary>The moment of the last step.</summary>
			double _seconds;

			/// <summary>Moves the mouse to where the script has it at a moment, and reads the stick.</summary>
			public void Step(double seconds)
			{
				var fromX = MouseX;
				var fromY = MouseY;
				var phase = Script.At(seconds, out MouseX, out MouseY);
				Pressed = phase.Pressed;
				_phase = phase;
				var button = Stick.Settings.Button;
				if (button > 0 && button <= _state.Buttons.Length)
					_state.Buttons[button - 1] = Pressed;
				Stick.BeginPass(_state);
				// The script's whole movement is the stick's whole travel in Position at 100 %.
				var ticks = 1 + (long)(seconds * Stopwatch.Frequency);
				StickX = Stick.Value(0, (int)Math.Round(MouseX * MouseStick.PositionCounts), 1, ticks);
				StickY = Stick.Value(1, (int)Math.Round(MouseY * MouseStick.PositionCounts), 1, ticks);
				Walk(MouseX - fromX, MouseY - fromY, seconds);
			}

			/// <summary>Turns the rodent smoothly the way it goes, steps its legs by the distance walked, and lets it all settle when it stops.</summary>
			void Walk(double dx, double dy, double seconds)
			{
				var time = seconds - _seconds;
				_seconds = seconds;
				if (time <= 0)
					return;
				// The distance in the rodent's own units, as it travels.
				var distance = Math.Sqrt(dx * dx + dy * dy) * RodentTravel(Script) / RodentSize;
				var walking = distance > 1e-6;
				if (walking)
				{
					_way = Math.Atan2(dy, dx);
					Stride += distance * 2 * Math.PI / RodentStride;
				}
				var turn = Math.IEEERemainder(_way - Heading, 2 * Math.PI) * Ease(time, 0.1);
				Heading += turn;
				Pace += ((walking ? 1 : 0) - Pace) * Ease(time, 0.12);
				// The tail stays behind as the body turns, and catches up.
				var lag = Math.Max(-1, Math.Min(1, -turn / time * 0.06));
				Lag += (lag - Lag) * Ease(time, 0.1);
			}

			/// <summary>The share of the way to where it is going that a value moves in a time, easing there over a time constant.</summary>
			static double Ease(double seconds, double constant)
			{
				return 1 - Math.Exp(-seconds / constant);
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
			ShowWords();
			DemoPictureBox.Invalidate();
		}

		void ShowWords()
		{
			UpdatePlayButton();
			ControlsHelper.SetText(PhaseLabel, _run?.Text ?? "");
			var group = _source?.ReadSettings();
			var note = NoteFor(group);
			ControlsHelper.SetText(NoteLabel, note);
			if (NoteLabel.Visible != (note.Length > 0))
				NoteLabel.Visible = note.Length > 0;
			if (group != null)
			{
				ControlsHelper.SetText(CaptionLabel, string.Format(UiLanguage.T("MouseDemoUserControl.StyleCaption", "{0}: {1}"),
					_source.StickName, MouseStickUserControl.StyleName(group.Style)));
				var what = MouseStickUserControl.StyleExplanation(group.Style);
				var culture = CultureInfo.GetCultureInfo(UiLanguage.Culture);
				ControlsHelper.SetText(DescriptionLabel, char.ToUpper(what[0], culture) + what.Substring(1) + ".");
			}
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

		/// <summary>Disabled with the page's controls, the demo is drawn faded and stands still.</summary>
		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			UpdateTimer();
			DemoPictureBox.Invalidate();
		}

		/// <summary>Whether the demo may move now: playing, enabled, on the page shown, in a window that is not minimised.</summary>
		bool CanRun()
		{
			return _playing && Enabled && Visible && _form != null && _form.WindowState != FormWindowState.Minimized;
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
				UpdatePlayButton();
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
		/// <remarks>What is lit, the stick's dot and the ear of a held button, is the theme's lighter green, ColorActive.</remarks>
		void DemoPictureBox_Paint(object sender, PaintEventArgs e)
		{
			if (Enabled)
			{
				DrawDemo(e.Graphics);
				return;
			}
			// Disabled with the page's controls: the picture faded, as the page's charts are.
			var box = DemoPictureBox.ClientSize;
			if (box.Width < 1 || box.Height < 1)
				return;
			using (var picture = new Bitmap(box.Width, box.Height))
			{
				using (var g = Graphics.FromImage(picture))
					DrawDemo(g);
				ControlsHelper.DrawImageWithOpacity(e.Graphics, picture, new Rectangle(Point.Empty, box), EngineHelper.DisabledOpacity);
			}
		}

		/// <summary>The demo as <see cref="DemoPictureBox_Paint"/> shows it.</summary>
		void DrawDemo(Graphics g)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			// As large as the box lets it be, from its left edge.
			var box = DemoPictureBox.ClientSize;
			var side = Math.Min(box.Height, box.Width / PictureWidth);
			var size = new SizeF(side * PictureWidth, side);
			float unit;
			var pad = PadOf(side, out unit);
			using (var line = new Pen(SystemColors.ControlDark, Math.Max(1f, unit / 2)))
			using (var ink = new Pen(SystemColors.ControlText, Math.Max(1f, unit / 2)))
			using (var field = new SolidBrush(SystemColors.Window))
			using (var lit = new SolidBrush(FormsTheme.GetColor("ColorActive")))
			{
				if (_run.Stick.Settings.Style == MouseStyle.Screen)
					DrawScreen(g, pad, unit, line, ink, field);
				else
					DrawMouse(g, pad, unit, line, ink, field, lit);
				// The stick half the pad's size, as a thumbstick is beside a mouse pad, on the right and level with the pad's middle.
				var diameter = pad.Width / 2;
				var stick = new RectangleF(size.Width - unit - diameter, (side - diameter) / 2, diameter, diameter);
				// The arrow from the mouse to the stick.
				var y = side / 2f;
				DrawArrow(g, ink, new PointF(pad.Right + 2 * unit, y), new PointF(stick.X - 2 * unit, y), unit);
				// The stick: its circle, its centre, and its dot where the stick is.
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

		/// <summary>The picture's height in its units, which every part of it is measured in.</summary>
		const float PictureUnits = 24;
		/// <summary>The picture's width as a share of its height: the pad, the arrow and the stick half the pad's size.</summary>
		const float PictureWidth = 2f;
		/// <summary>The gap between the mouse and the arrow beside it, and the arrow's length, in the picture's units.</summary>
		const float ArrowGap = 0.5f, ArrowLength = 3;

		/// <summary>The pad, or in Screen the screen's area, on the left of a picture so high: square, a unit in from its edges.</summary>
		static RectangleF PadOf(float side, out float unit)
		{
			unit = side / PictureUnits;
			return new RectangleF(unit, unit, side - 2 * unit, side - 2 * unit);
		}

		/// <summary>
		/// How far the mouse goes across and down the pad for the script's 1: as far as the script's farthest point lets it, turned
		/// any way, with its tail and the arrow ahead of it on the pad.
		/// </summary>
		SizeF Travel(float unit)
		{
			var travel = RodentTravel(_run.Script) * unit;
			return new SizeF(travel, travel);
		}

		/// <summary>Where a point of the script is on the pad.</summary>
		static PointF OnPad(RectangleF pad, SizeF travel, double x, double y)
		{
			return new PointF(pad.X + pad.Width / 2 + (float)x * travel.Width, pad.Y + pad.Height / 2 + (float)y * travel.Height);
		}

		/// <summary>The pad, the mouse on it where the script has it with the ear on its button's side lit while held, and an arrow the way it moves.</summary>
		void DrawMouse(Graphics g, RectangleF pad, float unit, Pen line, Pen ink, Brush field, Brush lit)
		{
			g.FillRectangle(field, pad);
			g.DrawRectangle(line, pad.X, pad.Y, pad.Width, pad.Height);
			var travel = Travel(unit);
			// Where the mouse started, so it is seen to rest away from there.
			var from = OnPad(pad, travel, _run.Script.StartX, _run.Script.StartY);
			g.DrawLine(line, from.X - unit, from.Y, from.X + unit, from.Y);
			g.DrawLine(line, from.X, from.Y - unit, from.X, from.Y + unit);
			var at = OnPad(pad, travel, _run.MouseX, _run.MouseY);
			DrawRodent(g, at, unit, lit);
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
				// From the mouse's nose the way it goes.
				var edge = RodentNose * RodentSize * unit;
				var gap = ArrowGap * unit;
				var start = new PointF(at.X + vx * (edge + gap), at.Y + vy * (edge + gap));
				var tip = new PointF(start.X + vx * ArrowLength * unit, start.Y + vy * ArrowLength * unit);
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

		#region The mouse

		/// <summary>The rodent's own unit, as a share of the picture's: small enough beside the pad to leave it room to walk.</summary>
		const float RodentSize = 0.7f;
		// The rodent, in its own units, facing up from the point the script moves: its nose this far ahead, its rump and the root
		// of its tail this far behind, its body half this wide, and its tail never farther from its root than its length.
		const float RodentNose = 4.15f, RodentRump = 3.35f, RodentHalfWidth = 2.3f, RodentTailRoot = 3f, RodentTail = 4.6f;
		/// <summary>The distance the rodent walks in each cycle of its legs, in its own units.</summary>
		const float RodentStride = 2.2f;
		/// <summary>
		/// How far the rodent reaches from where the script has it, in the picture's units, turned any way: its nose with the arrow
		/// ahead of it, or its tail, with half their lines, half the pad's edge and a gap.
		/// </summary>
		static readonly float RodentReach = Math.Max(RodentNose * RodentSize + ArrowGap + ArrowLength, (RodentTailRoot + RodentTail) * RodentSize) + 0.75f;
		static readonly Color RodentFur = Color.FromArgb(0xEC, 0xE9, 0xE5);
		static readonly Color RodentFurEdge = Color.FromArgb(0x8A, 0x82, 0x85);
		static readonly Color RodentPink = Color.FromArgb(0xF4, 0xBC, 0xC5);
		static readonly Color RodentDeepPink = Color.FromArgb(0xE3, 0x86, 0x96);
		static readonly Color RodentPinkEdge = Color.FromArgb(0xB8, 0x6A, 0x78);
		static readonly Color RodentEye = Color.FromArgb(0x2B, 0x26, 0x28);

		/// <summary>How far the rodent goes for the script's 1, in the picture's units: the script's farthest point brings it to the pad's edge.</summary>
		static float RodentTravel(DemoScript script)
		{
			return (float)(((PictureUnits - 2) / 2 - RodentReach) / script.Extent);
		}

		/// <summary>From the rodent's own units, facing up, to its place in the picture, turned to its heading.</summary>
		Matrix RodentPlace(PointF at, float unit)
		{
			var place = new Matrix();
			place.Translate(at.X, at.Y);
			place.Rotate((float)(_run.Heading * 180 / Math.PI) + 90);
			place.Scale(unit * RodentSize, unit * RodentSize);
			return place;
		}

		/// <summary>A shape the same on both sides, from the Bézier curves of its right side, from the middle line at its top to the middle line at its foot.</summary>
		static GraphicsPath Mirrored(params PointF[] right)
		{
			var path = new GraphicsPath();
			path.AddBeziers(right.Concat(right.Reverse().Skip(1).Select(p => new PointF(-p.X, p.Y))).ToArray());
			path.CloseFigure();
			return path;
		}

		/// <summary>A point of the tail's curve: a share of its length from its root, at an angle to straight behind.</summary>
		static PointF TailPoint(float share, double angle)
		{
			return new PointF(-share * RodentTail * (float)Math.Sin(angle), RodentTailRoot + share * RodentTail * (float)Math.Cos(angle));
		}

		/// <summary>The tail's outline along a Bézier curve, thinning from its root to its tip.</summary>
		static PointF[] TailOutline(PointF p0, PointF p1, PointF p2, PointF p3)
		{
			const int steps = 16;
			var outline = new PointF[2 * (steps + 1)];
			for (var i = 0; i <= steps; i++)
			{
				var t = (float)i / steps;
				var u = 1 - t;
				// The point on the curve, and the way the curve goes there.
				var x = u * u * u * p0.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X;
				var y = u * u * u * p0.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y;
				var dx = 3 * u * u * (p1.X - p0.X) + 6 * u * t * (p2.X - p1.X) + 3 * t * t * (p3.X - p2.X);
				var dy = 3 * u * u * (p1.Y - p0.Y) + 6 * u * t * (p2.Y - p1.Y) + 3 * t * t * (p3.Y - p2.Y);
				var half = (0.32f - 0.26f * t) / (float)Math.Sqrt(dx * dx + dy * dy);
				outline[i] = new PointF(x - dy * half, y + dx * half);
				outline[outline.Length - 1 - i] = new PointF(x + dy * half, y - dx * half);
			}
			return outline;
		}

		/// <summary>
		/// The rodent, seen from above where the script has it and turned to its heading: its paws stepping and its tail swaying with
		/// the distance walked, its tail trailing its turns and its ears twitching while it walks, all settling when it stops, and the
		/// ear on the side of the button the stick uses lit while held.
		/// </summary>
		void DrawRodent(Graphics g, PointF at, float unit, Brush lit)
		{
			var run = _run;
			var pace = (float)run.Pace;
			var state = g.Save();
			using (var place = RodentPlace(at, unit))
				g.MultiplyTransform(place);
			using (var furEdge = new Pen(RodentFurEdge, 0.3f) { LineJoin = LineJoin.Round })
			using (var pinkEdge = new Pen(RodentPinkEdge, 0.25f) { LineJoin = LineJoin.Round })
			using (var whisker = new Pen(Color.FromArgb(160, RodentFurEdge), 0.12f))
			using (var fur = new SolidBrush(RodentFur))
			using (var pink = new SolidBrush(RodentPink))
			using (var deepPink = new SolidBrush(RodentDeepPink))
			using (var eye = new SolidBrush(RodentEye))
			using (var glint = new SolidBrush(Color.White))
			{
				// The tail, under the body: a curve within its length of its root, swaying from side to side as it walks.
				var swing = run.Stride / 2;
				var sway = 0.45 * run.Pace;
				var tail = TailOutline(new PointF(0, RodentTailRoot),
					TailPoint(0.4f, 0.05 + 0.4 * (run.Lag + sway * Math.Sin(swing))),
					TailPoint(0.75f, 0.6 + 0.7 * (run.Lag + sway * Math.Sin(swing - 1.1))),
					TailPoint(1, -0.15 + run.Lag + sway * Math.Sin(swing - 2.2)));
				g.FillPolygon(pink, tail);
				g.DrawPolygon(pinkEdge, tail);
				// The paws, under the body, each diagonal pair stepping together: the left front with the right hind.
				for (var i = 0; i < 4; i++)
				{
					var side = i % 2 == 0 ? -1 : 1;
					var front = i < 2;
					var step = pace * (front ? 0.55f : 0.65f) * (float)Math.Sin(run.Stride + ((side < 0) == front ? 0 : Math.PI));
					var size = front ? new SizeF(0.85f, 1f) : new SizeF(0.95f, 1.45f);
					var x = side * (front ? 1.45f : 2.05f);
					var y = (front ? -0.95f : 2.35f) - step;
					var paw = new RectangleF(x - size.Width / 2, y - size.Height / 2, size.Width, size.Height);
					g.FillEllipse(pink, paw);
					g.DrawEllipse(pinkEdge, paw);
				}
				using (var body = Mirrored(new PointF(0, -1.9f), new PointF(1.35f, -1.9f), new PointF(RodentHalfWidth, -0.6f), new PointF(RodentHalfWidth, 1),
					new PointF(RodentHalfWidth, 2.5f), new PointF(1.4f, RodentRump), new PointF(0, RodentRump)))
				using (var head = Mirrored(new PointF(0, -3.95f), new PointF(0.4f, -3.95f), new PointF(1.45f, -3f), new PointF(1.45f, -2.1f),
					new PointF(1.45f, -1.2f), new PointF(1f, -0.7f), new PointF(0, -0.7f)))
				{
					g.FillPath(fur, body);
					g.DrawPath(furEdge, body);
					g.FillPath(fur, head);
					g.DrawPath(furEdge, head);
				}
				// The ears, twitching a little as it walks; the one on the side of the button the stick uses lit while held.
				for (var side = -1; side <= 1; side += 2)
				{
					var held = run.Pressed && (run.Stick.Settings.Button == 2) == (side > 0);
					var ear = g.Save();
					g.TranslateTransform(side * 0.75f, -1.35f);
					g.RotateTransform(side * (35 + 12 * pace * (float)Math.Sin(run.Stride / 2 + side)));
					var outside = new RectangleF(side * 0.75f - 0.8f, -0.9f, 1.6f, 1.8f);
					g.FillEllipse(held ? lit : pink, outside);
					if (!held)
						g.FillEllipse(deepPink, side * 0.85f - 0.45f, -0.55f, 0.9f, 1.1f);
					g.DrawEllipse(pinkEdge, outside);
					g.Restore(ear);
				}
				// The whiskers, the eyes and the nose.
				for (var side = -1; side <= 1; side += 2)
				{
					g.DrawLine(whisker, side * 0.35f, -3.55f, side * 2.25f, -4.15f);
					g.DrawLine(whisker, side * 0.4f, -3.45f, side * 2.45f, -3.5f);
					g.DrawLine(whisker, side * 0.4f, -3.35f, side * 2.15f, -2.9f);
					g.FillEllipse(eye, side * 0.68f - 0.31f, -3.1f, 0.62f, 0.7f);
					g.FillEllipse(glint, side * 0.68f - 0.05f, -2.98f, 0.2f, 0.2f);
				}
				g.FillEllipse(deepPink, -0.31f, -RodentNose, 0.62f, 0.5f);
				g.DrawEllipse(pinkEdge, -0.31f, -RodentNose, 0.62f, 0.5f);
			}
			g.Restore(state);
		}

		#endregion
	}
}
