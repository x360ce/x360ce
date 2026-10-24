using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Threading;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class AxisMapUserControl : UserControl
	{
		public AxisMapUserControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			InitPaintObjects();
			FormsTheme.ThemeChanged += FormsTheme_ThemeChanged;
			// Initialize in constructor and not on "Load" event or it will reset AntiDeadZone value
			// inside DeadZoneControlsLink(...).
			updateTimer = new QueueTimer(500, 0);
			updateTimer.DoWork += updateTimer_DoWork;
			// TargetType is still unset here, so the links start at the wider thumb range
			// and UpdateTargetType() narrows it once the designer assigns the target.
			var maxValue = (int)short.MaxValue;
			deadzoneLink = new DeadZoneControlsLink(DeadZoneTrackBar, DeadZoneNumericUpDown, DeadZoneTextBox, maxValue);
			deadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			antiDeadzoneLink = new DeadZoneControlsLink(AntiDeadZoneTrackBar, AntiDeadZoneNumericUpDown, AntiDeadZoneTextBox, maxValue);
			antiDeadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			PresetComboBox.Items.AddRange(Presets);
			PresetComboBox.SelectedIndex = 0;
			RefreshBackgroundImageAsync();
		}

		void deadzoneLink_ValueChanged(object sender, EventArgs e)
		{
			RefreshBackgroundImageAsync();
		}

		DeadZoneControlsLink deadzoneLink;
		DeadZoneControlsLink antiDeadzoneLink;

		QueueTimer updateTimer;

		/// <summary>Formula that decides this row's value, or null when the dead zones decide it.</summary>
		/// <remarks>
		/// A row driven by a formula ignores its dead zone, anti dead zone and sensitivity, so a line
		/// drawn from those three describes something the row no longer does. The line is drawn from
		/// the formula instead, through the same code the device loop runs, so the two cannot describe
		/// different things.
		/// </remarks>
		MapExpression _Expression;
		MapReference? _SweptSource;
		readonly float[] _ExpressionValues = new float[MapExpression.MaxReferences];

		/// <summary>Sets the formula this row follows, or clears it.</summary>
		public void SetExpression(MapExpression expression)
		{
			// Compared by the text so that the picture is not rebuilt on every pass of the device loop.
			var oldText = _Expression == null ? null : _Expression.Text;
			var newText = expression == null ? null : expression.Text;
			if (oldText == newText)
				return;
			_Expression = expression;
			_SweptSource = MapExpressionUnits.GetSweptSource(expression);
			RefreshBackgroundImageAsync();
		}

		[Category("Appearance"), DefaultValue(0)]
		public string HeaderText
		{
			get { return _HeaderText; }
			set
			{
				_HeaderText = value;
				UpdateHeader();
			}
		}
		string _HeaderText;

		/// <summary>
		/// Greys out the dead zones and the sensitivity, which only a stick, pedal or slider row uses: a button row sends
		/// full travel and a formula row works out its own value. The chart stays, so a formula's curve is still drawn.
		/// </summary>
		public void SetTuningEnabled(bool enabled)
		{
			if (_TuningEnabled == enabled)
				return;
			_TuningEnabled = enabled;
			var controls = new Control[]
			{
				DeadZoneLabel, DeadZoneTrackBar, DeadZoneNumericUpDown, DeadZoneTextBox,
				AntiDeadZoneLabel, AntiDeadZoneTrackBar, AntiDeadZoneNumericUpDown, AntiDeadZoneTextBox,
				SensitivityLabel, SensitivityTrackBar, SensitivityNumericUpDown, SensitivityTextBox, SensitivityCheckBox,
				PresetComboBox,
			};
			foreach (var control in controls)
				control.Enabled = enabled;
			UpdateHeader();
		}
		bool _TuningEnabled = true;

		/// <summary>The header, which says why the settings are greyed out while they are.</summary>
		void UpdateHeader()
		{
			if (_HeaderText == null)
				_HeaderText = MainGroupBox.Text;
			MainGroupBox.Text = _TuningEnabled ? _HeaderText : _HeaderText + " (settings only for a mapped axis or slider)";
		}

		TargetType _TargetType;

		[Category("Appearance"), DefaultValue(TargetType.None)]
		public TargetType TargetType
		{
			get { return _TargetType; }
			set
			{
				_TargetType = value;
				UpdateTargetType();
			}
		}

		/// <summary>True when the target is a thumb axis, false for a trigger.</summary>
		bool IsThumb =>
			TargetType == TargetType.LeftThumbX ||
			TargetType == TargetType.LeftThumbY ||
			TargetType == TargetType.RightThumbX ||
			TargetType == TargetType.RightThumbY;

		/// <summary>
		/// Apply the value range the target uses: a trigger is [0;255], a thumb is
		/// [-32768;32767]. The designer assigns TargetType after the constructor has
		/// run, so the range cannot be settled there.
		/// </summary>
		void UpdateTargetType()
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			var maxValue = IsThumb ? short.MaxValue : byte.MaxValue;
			DeadZoneNumericUpDown.Maximum = maxValue;
			AntiDeadZoneNumericUpDown.Maximum = maxValue;
			RefreshBackgroundImageAsync();
		}

		Bitmap LastBackgroundImage = null;

		void RefreshBackgroundImageAsync()
		{
			if (updateTimer == null)
				return;
			var param = (int)SensitivityTrackBar.Value;
			updateTimer.DoActionNow(param);
			SensitivityLabel.Text = SensitivityCheckBox.Checked
				? "Sensitivity - Make more sensitive in the center:"
				: "Sensitivity - Make less sensitive in the center:";
		}

		void updateTimer_DoWork(object sender, QueueTimerEventArgs e)
		{
			CreateBacgroundPicture();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				FormsTheme.ThemeChanged -= FormsTheme_ThemeChanged;
				if (deadzoneLink != null)
					deadzoneLink.Dispose();
				if (antiDeadzoneLink != null)
					antiDeadzoneLink.Dispose();
				if (updateTimer != null)
					updateTimer.Dispose();
				if (components != null)
					components.Dispose();
				if (dInputLine != null)
					dInputLine.Dispose();
				if (nInputLine != null)
					nInputLine.Dispose();
				if (xInputPoint != null)
					xInputPoint.Dispose();
				if (xInputLine != null)
					xInputLine.Dispose();
				if (xInputPath != null)
					xInputPath.Dispose();
			}
			base.Dispose(disposing);
		}

		private void ThumbUserControl_EnabledChanged(object sender, EventArgs e)
		{
			MainPictureBox.Invalidate();
		}

		/// <summary>The chart's picture is made on another thread and handed back through this control's window, so it is made once the window exists.</summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			RefreshBackgroundImageAsync();
		}

		/// <summary>The chart's picture is as large as its box, so it is made again whenever the box's size changes.</summary>
		private void MainPictureBox_SizeChanged(object sender, EventArgs e)
		{
			RefreshBackgroundImageAsync();
		}

		// Half and Invert values are only in creating xinput path - red line.
		bool _invert;
		bool _half;

		int _dInput;
		int _xInput;

		public void DrawPoint(int dInput, int xInput, bool invert, bool half)
		{
			DInputValueLabel.Text = dInput.ToString();
			XInputValueLabel.Text = xInput.ToString();
			// The red path is part of the background image and is
			// built from these two values, so the image has to be rebuilt when the map
			// supplies different ones. Only on change: this runs on every state update.
			var pathChanged = _invert != invert || _half != half;
			_invert = invert;
			_half = half;
			_dInput = dInput;
			_xInput = xInput;
			if (pathChanged)
				RefreshBackgroundImageAsync();
			MainPictureBox.Refresh();
		}

		public void InitPaintObjects()
		{
			xInputPath = new Pen(Color.Red, 1f);
			// Dead zones turn the path into near-right angles. Round the joins so those
			// corners do not grow mitre spikes.
			xInputPath.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
			// The dot is the text colour: it marks a DInput and an XInput value at once, so it
			// belongs to neither axis colour. The thin lines carry the per-axis colour.
			xInputPoint = new SolidBrush(SystemColors.WindowText);
			ApplyValueColors();
			nInputLine = new Pen(AppHelper.ChartLineColor, 1f);
		}

		/// <summary>
		/// Colours the values and their marker lines: each line takes the colour of the value it belongs to, so XI is blue
		/// and DI is green, in the theme's own blue and green.
		/// </summary>
		void ApplyValueColors()
		{
			var xColor = FormsTheme.GetColor("ColorBrand", Color.Blue);
			var dColor = FormsTheme.GetColor("ForegroundSuccess", Color.Green);
			XInputValueLabel.ForeColor = xColor;
			DInputValueLabel.ForeColor = dColor;
			if (xInputLine != null)
				xInputLine.Dispose();
			if (dInputLine != null)
				dInputLine.Dispose();
			xInputLine = new Pen(xColor, 1f);
			dInputLine = new Pen(dColor, 1f);
		}

		/// <summary>The value colours are the theme's, so they are taken again in a new one.</summary>
		void FormsTheme_ThemeChanged(object sender, EventArgs e)
		{
			ApplyValueColors();
			MainPictureBox.Invalidate();
		}

		Pen xInputPath;
		SolidBrush xInputPoint;
		Pen xInputLine;
		Pen dInputLine;
		Pen nInputLine;

		private void CreateBacgroundPicture()
		{
			int deadZone = 0;
			int antiDeadZone = 0;
			int sensitivity = 0;
			Invoke(new Action(() =>
			{
				deadZone = (int)DeadZoneNumericUpDown.Value;
				antiDeadZone = (int)AntiDeadZoneNumericUpDown.Value;
				sensitivity = (int)SensitivityNumericUpDown.Value;
			}));
			var chart = AppHelper.ChartBounds(MainPictureBox);
			var w = chart.Width;
			var h = chart.Height;
			var bmp = new Bitmap(w, h);
			var g = Graphics.FromImage(bmp);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			var wF = (float)w;
			var hF = (float)h;
			//DrawDLineLine(g, 74.5f);
			//DrawXLineLine(g, 74.5f);
			//DrawDot(g, 74.5f, 74.5f, 0.5f, xInputPath);
			// The chart's frame, its middle lines and both diagonals, in a faint grey.
			g.DrawRectangle(nInputLine, 0, 0, w - 1, h - 1);
			g.DrawLine(nInputLine, 0f, (h - 1) / 2f, w - 1f, (h - 1) / 2f);
			g.DrawLine(nInputLine, (w - 1) / 2f, 0f, (w - 1) / 2f, h - 1f);
			g.DrawLine(nInputLine, 0f, (float)h - 1f, (float)w - 1f, 0f);
			g.DrawLine(nInputLine, 0f, 0f, (float)w - 1f, (float)h - 1f);
			// The mapping is not continuous: dead zone and anti-dead zone make it jump,
			// so stamped dots leave gaps. Collect the samples and stroke them as one
			// path, which bridges every jump with a segment of the same line.
			var path = new List<PointF>();
			for (float i = 0; i <= wF; i += 0.5f)
			{
				var min = IsThumb ? -32768f : 0f;
				var max = IsThumb ? 32767f : 255f;
				// Convert Image X position [0;w] to DInput position [0;65535].
				var dInputValue = ConvertHelper.ConvertRangeF(0f, wF, ushort.MinValue, ushort.MaxValue, i);
				float result;
				float formula;
				if (_SweptSource.HasValue && MapExpressionUnits.TrySweep(
					_Expression, _SweptSource.Value, (int)dInputValue, IsThumb, _ExpressionValues, out formula))
				{
					// Turned into the destination's own units by the same pair the device loop uses, so
					// the line and the controller picture agree about what the formula produces.
					result = IsThumb
						? MapExpressionUnits.ToThumb(formula)
						: MapExpressionUnits.ToTrigger(formula);
				}
				else
				{
					result = ConvertHelper.GetThumbValue(dInputValue, deadZone, antiDeadZone, sensitivity, _invert, _half, IsThumb);
				}
				// Convert XInput Y position [min;max] to image size [0;h].
				var y = ConvertHelper.ConvertRangeF(min, max, 0f, hF, result);
				// An inverted axis is mirrored left to right, so its DInput minimum sits
				// on the right and the path rises like every other chart. Image Y grows
				// downward, so flip the value.
				path.Add(new PointF(_invert ? wF - i : i, hF - y));
			}
			if (path.Count > 1)
				g.DrawLines(xInputPath, path.ToArray());
			Invoke(new Action(() =>
			{
				LastBackgroundImage = bmp;
				MainPictureBox.Invalidate();
			}));
		}

		void DrawDLineLine(Graphics g, float x)
		{
			var w = g.VisibleClipBounds.Width;
			var h = g.VisibleClipBounds.Height;
			// Snap to pixels.
			x = (float)Math.Floor(x);
			// Make sure last line is not snapped outside.
			if (x == w)
				x -= 1f;
			g.DrawLine(dInputLine, x, 0, x, h);
		}

		void DrawXLineLine(Graphics g, float y)
		{
			var w = g.VisibleClipBounds.Width;
			var h = g.VisibleClipBounds.Height;
			y = h - y;
			// Snap to pixels.
			y = (float)Math.Floor(y);
			// Make sure last line is not snapped outside.
			if (y == h)
				y -= 1f;
			g.DrawLine(xInputLine, 0, y, w, y);
		}

		void DrawDot(Graphics g, float x, float y, float radius, Brush brush, bool snap = false)
		{
			var w = g.VisibleClipBounds.Width;
			var h = g.VisibleClipBounds.Height;
			// Half pixel.
			var p = 0.5f;
			// If snap all.
			if (snap)
			{
				// Snap to pixels.
				x = (float)Math.Floor(x);
				// Make sure last line is not snapped outside.
				if (x == w)
					x -= 1f;
				x += p;
			}
			else
			{
				var wm = (w / 2f);
				var hm = (h / 2f);
				// Snap X to start, center and end.
				if (x < 1f)
					x = p;
				if (x >= wm - p && x <= wm + p)
					x = wm;
				if (x > w - 1f)
					x = w - p;
				// Snap Y to top, middle and bottom.
				if (y < 1f)
					y = p;
				if (y >= hm - p && y <= hm + p)
					y = hm;
				if (y > h - 1f)
					y = h - p;
			}
			//g.FillRectangle(brush, x - radius - 0.5f, h - y - radius - 0.5f, radius * 2f, radius * 2f);
			g.FillEllipse(brush, x - radius - 0.5f, h - y - radius - 0.5f, radius * 2f, radius * 2f);
		}

		private void LinearPictureBox_Paint(object sender, PaintEventArgs e)
		{
			var g = e.Graphics;
			AppHelper.PaintChartBox(g, MainPictureBox, Enabled);
			var image = LastBackgroundImage;
			if (image == null || !Enabled) return;
			var chart = AppHelper.ChartBounds(MainPictureBox);
			g.DrawImageUnscaled(image, chart.Location);
			// The lines and the dot are drawn in the chart's own place.
			g.TranslateTransform(chart.X, chart.Y);
			g.SetClip(new Rectangle(0, 0, image.Width, image.Height));
			var w = (float)image.Width;
			var h = (float)image.Height;
			// Convert DInput to image position.
			var di = ConvertHelper.ConvertRangeF(0f, ushort.MaxValue, 0f, w, _dInput);

			var min = IsThumb ? -32768f : 0f;
			var max = IsThumb ? 32767f : 255f;
			// Convert XInput to image position.
			var xi = ConvertHelper.ConvertRangeF(min, max, 0f, h, _xInput);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			// Mirror an inverted axis left to right so it matches the red path.
			var dx = _invert ? w - di : di;
			DrawDLineLine(g, dx);
			DrawXLineLine(g, xi);
			// Draw dot.
			var radius = 2.5f;
			DrawDot(g, dx, xi, radius, xInputPoint, true);
		}

		#region Sensitivity Controls

		object SensitivityLock = new object();

		private void SensitivityTrackBar_ValueChanged(object sender, EventArgs e)
		{
			var control = (TrackBar)sender;
			lock (SensitivityLock)
			{
				SensitivityNumericUpDown.ValueChanged -= new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
				SensitivityCheckBox.CheckedChanged -= new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				var percent = (int)control.Value;
				var invert = SensitivityCheckBox.Checked;
				var value = invert ? -percent : percent;
				var percentString = string.Format("{0} %", percent);
				// Update percent TextBox.
				if (SensitivityTextBox.Text != percentString) SensitivityTextBox.Text = percentString;
				// Update NumericUpDown.
				if (SensitivityNumericUpDown.Value != percent) SensitivityNumericUpDown.Value = value;
				// Update BheckBox.
				if (SensitivityCheckBox.Checked != invert) SensitivityCheckBox.Checked = invert;
				SensitivityCheckBox.CheckedChanged += new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				SensitivityNumericUpDown.ValueChanged += new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
			}
			RefreshBackgroundImageAsync();
		}

		private void SensitivityNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var control = (NumericUpDown)sender;
			lock (SensitivityLock)
			{
				SensitivityTrackBar.ValueChanged -= new System.EventHandler(SensitivityTrackBar_ValueChanged);
				SensitivityCheckBox.CheckedChanged -= new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				var value = (int)control.Value;
				var invert = value < 0;
				var percent = invert ? -value : value;
				var percentString = string.Format("{0} %", percent);
				// Update percent TextBox.
				if (SensitivityTextBox.Text != percentString) SensitivityTextBox.Text = percentString;
				// Update TrackBar.
				if (SensitivityTrackBar.Value != value) SensitivityTrackBar.Value = percent;
				// Update CheckBox.
				if (SensitivityCheckBox.Checked != invert) SensitivityCheckBox.Checked = invert;
				SensitivityCheckBox.CheckedChanged += new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				SensitivityTrackBar.ValueChanged += new System.EventHandler(SensitivityTrackBar_ValueChanged);
			}
			RefreshBackgroundImageAsync();
		}

		private void SensitivityCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var control = (CheckBox)sender;
			lock (SensitivityLock)
			{
				SensitivityNumericUpDown.ValueChanged -= new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
				SensitivityTrackBar.ValueChanged -= new System.EventHandler(SensitivityTrackBar_ValueChanged);
				SensitivityNumericUpDown.Value = -SensitivityNumericUpDown.Value;
				SensitivityTrackBar.ValueChanged += new System.EventHandler(SensitivityTrackBar_ValueChanged);
				SensitivityNumericUpDown.ValueChanged += new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
			}
			RefreshBackgroundImageAsync();
		}

		#endregion

		#region Presets

		/// <summary>A ready-made dead zone, anti-dead zone and sensitivity, each in percent.</summary>
		sealed class Preset
		{
			public Preset(string name, int deadZone, int antiDeadZone, int sensitivity)
			{
				Name = name;
				DeadZone = deadZone;
				AntiDeadZone = antiDeadZone;
				Sensitivity = sensitivity;
			}

			public readonly string Name;
			public readonly int DeadZone;
			public readonly int AntiDeadZone;
			public readonly int Sensitivity;

			public override string ToString() => Name;
		}

		/// <summary>The list's own name, then the presets: picking one applies it, and the list shows its name again.</summary>
		static readonly object[] Presets =
		{
			"Apply Preset",
			new Preset("Clear", 0, 0, 0),
			new Preset("5% DeadZone, 100% Controller Anti-DeadZone", 5, 100, 0),
			new Preset("1% DeadZone, 100% Controller Anti-DeadZone", 1, 100, 0),
			new Preset("100% Controller Anti-DeadZone", 0, 100, 0),
			new Preset("80% Controller Anti-DeadZone", 0, 80, 0),
			new Preset("60% Controller Anti-DeadZone", 0, 60, 0),
			new Preset("40% Controller Anti-DeadZone", 0, 40, 0),
			new Preset("20% Controller Anti-DeadZone", 0, 20, 0),
		};

		private void PresetComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var preset = PresetComboBox.SelectedItem as Preset;
			if (preset == null)
				return;
			decimal xDeadZone = 0;
			switch (TargetType)
			{
				case TargetType.LeftTrigger:
				case TargetType.RightTrigger:
					xDeadZone = Controller.XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
					break;
				case TargetType.LeftThumbX:
				case TargetType.LeftThumbY:
					xDeadZone = Controller.XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE;
					break;
				case TargetType.RightThumbX:
				case TargetType.RightThumbY:
					xDeadZone = Controller.XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE;
					break;
				default:
					break;
			}
			// Move focus away from below controls, so that their value can be changed.
			ActiveControl = SensitivityCheckBox;
			DeadZoneTrackBar.Value = preset.DeadZone;
			AntiDeadZoneNumericUpDown.Value = xDeadZone * preset.AntiDeadZone / 100m;
			SensitivityTrackBar.Value = preset.Sensitivity;
			// The list shows its name again once the list has closed, so the same preset can be picked again.
			if (IsHandleCreated)
				BeginInvoke((Action)(() => PresetComboBox.SelectedIndex = 0));
		}

		#endregion

	}
}

