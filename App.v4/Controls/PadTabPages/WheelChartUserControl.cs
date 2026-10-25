using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>The centering spring's force at every angle of the wheel, and the wheel's angle and force now.</summary>
	/// <remarks>
	/// Drawn as the thumb stick charts are: a picture with the red curve, made again only when a setting, the wheel
	/// range, the live speed or the measured friction changes, with the wheel's angle and force painted over it on
	/// every refresh. The curve is the engine's own (<see cref="SpringCurve"/>), with its steepness cap and the speed, so
	/// the chart shows the force as it is played. Across is the steering angle from one stop to the other; up is a push
	/// to the right, down a push to the left.
	/// </remarks>
	public partial class WheelChartUserControl : UserControl
	{
		public WheelChartUserControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			InitPaintObjects();
			FormsTheme.ThemeChanged += FormsTheme_ThemeChanged;
			UpdateRangeLabels();
			UpdateValues();
		}

		/// <summary>The curve the chart draws, built from the settings below.</summary>
		readonly SpringCurve curve = new SpringCurve();

		// The settings the curve is drawn from.
		int strength;
		int centre = int.Parse(SettingName.DefaultForceSpringCentre);
		int peak = int.Parse(SettingName.DefaultForceSpringPeak);
		int lightening = int.Parse(SettingName.DefaultForceSpringLightening);
		int range;
		int speed = int.Parse(SettingName.DefaultForceSpringSpeed);

		/// <summary>The car's speed from a live source, which replaces the Speed setting while it is not null.</summary>
		int? speedOverride;

		/// <summary>The wheel's friction, in percent, drawn as the grey band and added to the curve, as the engine adds it; nought for none.</summary>
		int friction;

		/// <summary>The steering axis now, 0 to <see cref="SpringCalibration.AxisMax"/>, or -1 while no wheel is read.</summary>
		int position = -1;

		/// <summary>The angle and force the labels show, so they are written only when either changes.</summary>
		int shownAngle = int.MinValue;
		int shownForce = int.MinValue;

		/// <summary>The picture of the curve, made again when it is marked out of date or the chart changes size.</summary>
		Bitmap picture;
		bool pictureOutOfDate = true;
		PointF[] curvePoints = new PointF[0];

		Pen curvePen;
		Pen gridPen;
		Pen crossPen;
		SolidBrush bandBrush;
		SolidBrush dotBrush;
		SolidBrush noteBrush;
		Font noteFont;

		/// <summary>The steering range the degrees are counted against.</summary>
		int RangeDegrees { get { return range > 0 ? range : ForceFeedbackState.DefaultRangeDegrees; } }

		/// <summary>Sets the spring's settings the curve is drawn from. Draws it again only when one of them changed.</summary>
		/// <param name="strength">The car's strongest force, at Peak, in percent of the wheel's force, added to the friction.</param>
		/// <param name="centreDegrees">The angle of half the strength, at 100 km/h.</param>
		/// <param name="peakDegrees">The angle of the strongest force, at 100 km/h, where nought is the stop.</param>
		/// <param name="lighteningPercent">How much of the strongest force is left at the stop.</param>
		/// <param name="rangeDegrees">The wheel's range lock to lock, or nought when it is left as it is.</param>
		/// <param name="speed">The car's speed in km/h from the Speed setting.</param>
		public void SetCurve(int strength, int centreDegrees, int peakDegrees, int lighteningPercent, int rangeDegrees, int speed)
		{
			if (strength == this.strength && centreDegrees == centre && peakDegrees == peak
				&& lighteningPercent == lightening && rangeDegrees == range && speed == this.speed)
				return;
			var rangeChanged = rangeDegrees != range;
			this.strength = strength;
			centre = centreDegrees;
			peak = peakDegrees;
			lightening = lighteningPercent;
			range = rangeDegrees;
			this.speed = speed;
			if (rangeChanged)
				UpdateRangeLabels();
			DrawAgain();
		}

		/// <summary>Puts the wheel on the chart. Called on every refresh of the page; it allocates nothing unless what is shown changes.</summary>
		/// <param name="position">The steering axis, 0 to <see cref="SpringCalibration.AxisMax"/>, or null while no wheel is read.</param>
		/// <param name="speedOverride">The car's speed from a live source, or null to use the Speed setting.</param>
		/// <param name="frictionPercent">The wheel's friction, or nought while not known.</param>
		public void UpdateWheel(int? position, int? speedOverride, int frictionPercent)
		{
			if (speedOverride != this.speedOverride || frictionPercent != friction)
			{
				this.speedOverride = speedOverride;
				friction = frictionPercent;
				this.position = position ?? -1;
				DrawAgain();
				return;
			}
			var p = position ?? -1;
			if (p == this.position)
				return;
			this.position = p;
			UpdateValues();
			MainPictureBox.Invalidate();
		}

		/// <summary>Marks the picture out of date and shows the values the new curve gives.</summary>
		void DrawAgain()
		{
			pictureOutOfDate = true;
			UpdateValues();
			MainPictureBox.Invalidate();
		}

		/// <summary>Writes the wheel's angle and force above the chart, when either changed.</summary>
		void UpdateValues()
		{
			curve.Set(strength, centre, peak, lightening, range, speedOverride ?? speed, strength == 0 ? 0 : friction);
			var known = position >= 0;
			var angle = known ? AngleOf(position) : int.MinValue;
			var force = known ? curve.Force(position) : int.MinValue;
			if (angle == shownAngle && force == shownForce)
				return;
			shownAngle = angle;
			shownForce = force;
			AngleValueLabel.Text = known ? string.Format("{0}°", angle) : "-";
			ForceValueLabel.Text = !known
				? "-"
				: force == 0
					? "0 %"
					: string.Format("{0} % to the {1}", Math.Abs(force), force > 0 ? "right" : "left");
		}

		/// <summary>The steering angle of a wheel position, in whole degrees from the centre, negative to the left.</summary>
		int AngleOf(int axis)
		{
			return (int)Math.Round((axis - SpringCalibration.Center) * (RangeDegrees / 2.0) / SpringCalibration.Center);
		}

		/// <summary>Names the two stops and the centre under the chart.</summary>
		void UpdateRangeLabels()
		{
			var half = RangeDegrees / 2;
			LeftStopLabel.Text = string.Format("-{0}°", half);
			RightStopLabel.Text = string.Format("+{0}°", half);
		}

		void InitPaintObjects()
		{
			DisposePaintObjects();
			curvePen = new Pen(Color.Red, 1.5f);
			curvePen.LineJoin = LineJoin.Round;
			gridPen = new Pen(AppHelper.ChartLineColor, 1f);
			// The crosshair and the force are blue and the angle green, as the thumb charts show their two values.
			var forceColor = FormsTheme.GetColor("ColorBrand", Color.Blue);
			var angleColor = FormsTheme.GetColor("ForegroundSuccess", Color.Green);
			crossPen = new Pen(forceColor, 1f);
			ForceValueLabel.ForeColor = forceColor;
			AngleValueLabel.ForeColor = angleColor;
			bandBrush = new SolidBrush(Color.FromArgb(48, SystemColors.GrayText));
			// The dot marks an angle and a force at once, so it is the text colour rather than either value's.
			dotBrush = new SolidBrush(SystemColors.WindowText);
			noteBrush = new SolidBrush(SystemColors.GrayText);
			noteFont = new Font(Font.FontFamily, Font.Size * 0.85f, FontStyle.Regular, Font.Unit);
		}

		void DisposePaintObjects()
		{
			if (curvePen != null) curvePen.Dispose();
			if (gridPen != null) gridPen.Dispose();
			if (crossPen != null) crossPen.Dispose();
			if (bandBrush != null) bandBrush.Dispose();
			if (dotBrush != null) dotBrush.Dispose();
			if (noteBrush != null) noteBrush.Dispose();
			if (noteFont != null) noteFont.Dispose();
		}

		/// <summary>The colours are the theme's, so they are taken again in a new one.</summary>
		void FormsTheme_ThemeChanged(object sender, EventArgs e)
		{
			InitPaintObjects();
			pictureOutOfDate = true;
			MainPictureBox.Invalidate();
		}

		/// <summary>The small notes in the picture follow the page's font, which arrives once the chart is on the page.</summary>
		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (curvePen == null)
				return;
			InitPaintObjects();
			pictureOutOfDate = true;
			MainPictureBox.Invalidate();
		}

		float XOf(int axis, int width)
		{
			return (float)axis / SpringCalibration.AxisMax * (width - 1);
		}

		float YOf(int forcePercent, int height)
		{
			var middle = (height - 1) / 2f;
			return middle - forcePercent / 100f * middle;
		}

		/// <summary>Draws the picture behind the wheel's dot: the friction band, the frame and centre lines, which way the force pushes, and the red curve.</summary>
		void DrawPicture(int w, int h)
		{
			curve.Set(strength, centre, peak, lightening, range, speedOverride ?? speed, strength == 0 ? 0 : friction);
			if (picture != null)
				picture.Dispose();
			// Opaque, so the notes are drawn as clear text: text drawn onto a picture with see-through pixels comes out smudged.
			picture = new Bitmap(w, h, PixelFormat.Format24bppRgb);
			using (var g = Graphics.FromImage(picture))
			{
				g.Clear(SystemColors.Window);
				if (friction > 0)
				{
					var top = YOf(friction, h);
					g.FillRectangle(bandBrush, 0f, top, w, YOf(-friction, h) - top);
				}
				var middleX = (float)Math.Floor((w - 1) / 2f);
				var middleY = (float)Math.Floor((h - 1) / 2f);
				g.DrawRectangle(gridPen, 0, 0, w - 1, h - 1);
				g.DrawLine(gridPen, middleX, 0f, middleX, h);
				g.DrawLine(gridPen, 0f, middleY, w, middleY);
				g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
				var pushesLeft = "pushes left";
				var size = g.MeasureString(pushesLeft, noteFont);
				g.DrawString("pushes right", noteFont, noteBrush, 4f, 2f);
				g.DrawString(pushesLeft, noteFont, noteBrush, w - size.Width - 4f, h - size.Height - 2f);
				g.SmoothingMode = SmoothingMode.HighQuality;
				var points = CurvePoints(w, h);
				if (points.Length > 1)
					g.DrawLines(curvePen, points);
			}
			pictureOutOfDate = false;
		}

		/// <summary>The red line: the force the engine plays at every column of the picture.</summary>
		/// <remarks>
		/// The engine plays whole percents, so the force stays the same over a few columns and then steps. The line goes
		/// through the middle of each such run, and through both edges, so it passes every force played without drawing
		/// the steps.
		/// </remarks>
		PointF[] CurvePoints(int w, int h)
		{
			if (curvePoints.Length < w + 1)
				curvePoints = new PointF[w + 1];
			var count = 0;
			var runStart = 0;
			var runForce = ForceAtColumn(0, w);
			curvePoints[count++] = new PointF(0f, YOf(runForce, h));
			for (var x = 1; x < w; x++)
			{
				var force = ForceAtColumn(x, w);
				if (force == runForce)
					continue;
				curvePoints[count++] = new PointF((runStart + x - 1) / 2f, YOf(runForce, h));
				runStart = x;
				runForce = force;
			}
			curvePoints[count++] = new PointF(w - 1, YOf(runForce, h));
			var points = new PointF[count];
			Array.Copy(curvePoints, points, count);
			return points;
		}

		/// <summary>The force the curve plays at a column of the picture, in percent.</summary>
		int ForceAtColumn(int x, int width)
		{
			return curve.Force((int)Math.Round((double)x / Math.Max(1, width - 1) * SpringCalibration.AxisMax));
		}

		void MainPictureBox_Paint(object sender, PaintEventArgs e)
		{
			var g = e.Graphics;
			AppHelper.PaintChartBox(g, MainPictureBox, Enabled);
			var chart = AppHelper.ChartBounds(MainPictureBox);
			var w = chart.Width;
			var h = chart.Height;
			if (!Enabled || w < 2 || h < 2 || curvePen == null)
				return;
			if (pictureOutOfDate || picture == null || picture.Width != w || picture.Height != h)
				DrawPicture(w, h);
			g.DrawImageUnscaled(picture, chart.Location);
			if (position < 0)
				return;
			// The crosshair and the dot are drawn in the chart's own place.
			g.TranslateTransform(chart.X, chart.Y);
			g.SetClip(new Rectangle(0, 0, w, h));
			var x = (float)Math.Floor(XOf(position, w));
			var y = (float)Math.Floor(YOf(curve.Force(position), h));
			g.DrawLine(crossPen, x, 0f, x, h);
			g.DrawLine(crossPen, 0f, y, w, y);
			g.SmoothingMode = SmoothingMode.HighQuality;
			const float radius = 3f;
			g.FillEllipse(dotBrush, x - radius + 0.5f, y - radius + 0.5f, radius * 2f, radius * 2f);
		}

		void MainPictureBox_Resize(object sender, EventArgs e)
		{
			MainPictureBox.Invalidate();
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
				DisposePaintObjects();
				if (picture != null)
					picture.Dispose();
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
