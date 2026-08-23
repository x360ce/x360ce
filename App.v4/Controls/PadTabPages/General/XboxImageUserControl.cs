using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The controller picture with its navigation glyphs.
	/// </summary>
	/// <remarks>
	/// Everything is drawn onto one surface rather than composed from child controls. The
	/// positions come from <see cref="ImageInfos"/>, which the rest of the pad already uses, so
	/// the picture and the mapping list cannot drift apart.
	/// </remarks>
	public class XboxImageUserControl : UserControl
	{

		/// <summary>The artwork was laid out on this canvas; everything scales from it.</summary>
		public const int CanvasWidth = 256;
		public const int CanvasHeight = 289;
		const int TopImageHeight = 105;
		const int ImageGap = 8;

		/// <summary>Size of a mapping glyph and of an axis indicator, in canvas units.</summary>
		public const int GlyphSize = 18;
		const int IndicatorSize = 10;

		// Opacities the interface has always used: a shown glyph, and a recording glyph on the
		// dark half of its blink, which stays faintly visible so the target does not vanish.
		const float ShownOpacity = 0.8f;
		const float RecordIdleOpacity = 0.2f;

		public XboxImageUserControl()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
				| ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			Size = new Size(CanvasWidth, CanvasHeight);
			BackColor = SystemColors.Control;
			if (ControlsHelper.IsDesignMode(this))
				return;
			_Top = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerTop.png"));
			_Front = new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerFront.png"));
			_TopDisabled = AppHelper.GetDisabledImage(_Top);
			_FrontDisabled = AppHelper.GetDisabledImage(_Front);
		}

		Bitmap _Top, _Front, _TopDisabled, _FrontDisabled;

		PadControlImager _Imager;
		ImageInfos _Infos;
		MapTo _MappedTo;
		bool _Enabled = true;

		readonly Dictionary<MapCode, GlyphState> _States = new Dictionary<MapCode, GlyphState>();
		readonly Dictionary<string, Rectangle> _Groups = new Dictionary<string, Rectangle>();

		struct GlyphState
		{
			public NavImageType Type;
			public bool Show;
		}

		/// <summary>Raised to begin recording the mapping the user clicked.</summary>
		public Action<SettingsMapItem> StartRecording;

		/// <summary>Raised to end recording; returns true when something was captured.</summary>
		public Func<bool> StopRecording;

		public string MappingDone = "Mapping Done";

		#region Setup

		public void InitializeImages(ImageInfos imageInfos, PadControlImager imager, MapTo mappedTo)
		{
			_Infos = imageInfos;
			_Imager = imager;
			_MappedTo = mappedTo;
			_States.Clear();
			foreach (var info in imageInfos)
				_States[info.Code] = new GlyphState { Type = NavImageType.Normal, Show = false };
			_GlyphPoints = BuildGlyphPoints(imageInfos);
			BuildGroups();
			Invalidate();
		}

		/// <summary>
		/// Work out the area each hover group covers from the glyphs that belong to it.
		/// </summary>
		/// <remarks>
		/// Derived rather than written down, so moving a glyph moves the area that reveals it.
		/// </remarks>
		void BuildGroups()
		{
			_Groups.Clear();
			AddGroup(nameof(SettingsConverter.LeftThumbCodes), SettingsConverter.LeftThumbCodes);
			AddGroup(nameof(SettingsConverter.RightThumbCodes), SettingsConverter.RightThumbCodes);
			AddGroup(nameof(SettingsConverter.DPadCodes), SettingsConverter.DPadCodes);
			AddGroup(nameof(SettingsConverter.MainButtonCodes), SettingsConverter.MainButtonCodes);
			AddGroup(nameof(SettingsConverter.MenuButtonCodes), SettingsConverter.MenuButtonCodes);
			AddGroup(nameof(SettingsConverter.TriggerButtonCodes), SettingsConverter.TriggerButtonCodes);
			AddGroup(nameof(SettingsConverter.ShoulderButtonCodes), SettingsConverter.ShoulderButtonCodes);
		}

		void AddGroup(string name, List<MapCode> codes)
		{
			var points = _Infos.Where(x => codes.Contains(x.Code)).Select(GlyphPoint).ToArray();
			if (points.Length == 0)
				return;
			var left = points.Min(p => p.X) - GlyphSize;
			var top = points.Min(p => p.Y) - GlyphSize;
			var right = points.Max(p => p.X) + GlyphSize;
			var bottom = points.Max(p => p.Y) + GlyphSize;
			_Groups[name] = Rectangle.FromLTRB(left, top, right, bottom);
		}

		/// <summary>Raw position of a mapping on the canvas. X and Y in the table are centres.</summary>
		static Point CanvasPoint(ImageInfo info)
		{
			var y = info.Image == 1 ? info.Y : TopImageHeight + ImageGap + info.Y;
			return new Point((int)Math.Round(info.X), (int)Math.Round(y));
		}

		/// <summary>Where a glyph is drawn, which is not always where the mapping sits.</summary>
		/// <remarks>
		/// The table positions were laid out for the small marks the older interface drew, and on
		/// the D-pad and thumbsticks they are closer together than a glyph is wide, so drawing
		/// there overlaps them. A direction that lands too near its group centre is pushed out
		/// along the same line until the glyphs clear each other. Groups already spaced wider than
		/// a glyph, such as the face buttons, are left exactly where they are.
		/// </remarks>
		Point GlyphPoint(ImageInfo info)
		{
			Point resolved;
			return _GlyphPoints.TryGetValue(info.Code, out resolved) ? resolved : CanvasPoint(info);
		}

		/// <summary>Centre to centre distance that keeps neighbouring glyphs apart.</summary>
		public const int MinimumGlyphSpacing = GlyphSize + 1;

		Dictionary<MapCode, Point> _GlyphPoints = new Dictionary<MapCode, Point>();

		/// <summary>
		/// Where every glyph is drawn, given the mapping table.
		/// </summary>
		/// <remarks>
		/// Pure, so the spacing can be checked without building a controller picture.
		/// </remarks>
		public static Dictionary<MapCode, Point> BuildGlyphPoints(ImageInfos infos)
		{
			var points = new Dictionary<MapCode, Point>();
			if (infos == null)
				return points;
			foreach (var info in infos)
				points[info.Code] = CanvasPoint(info);
			foreach (var codes in new[] {
				SettingsConverter.LeftThumbCodes, SettingsConverter.RightThumbCodes,
				SettingsConverter.DPadCodes, SettingsConverter.MainButtonCodes })
				SpreadGroup(infos, codes, points);
			return points;
		}

		static void SpreadGroup(ImageInfos infos, List<MapCode> codes, Dictionary<MapCode, Point> points)
		{
			var members = infos.Where(x => codes.Contains(x.Code)).ToArray();
			// The centre is the mapping with no direction in its name, such as DPad itself.
			var centreInfo = members.FirstOrDefault(x => NavImages.GetDirection(x.Code).Length == 0);
			if (centreInfo == null)
				return;
			var centre = CanvasPoint(centreInfo);
			foreach (var info in members)
			{
				var point = CanvasPoint(info);
				var dx = point.X - centre.X;
				var dy = point.Y - centre.Y;
				var distance = Math.Sqrt(dx * dx + dy * dy);
				if (distance < 0.5 || distance >= MinimumGlyphSpacing)
					continue;
				var factor = MinimumGlyphSpacing / distance;
				points[info.Code] = new Point(
					centre.X + (int)Math.Round(dx * factor),
					centre.Y + (int)Math.Round(dy * factor));
			}
		}

		#endregion

		#region State the pad sets

		/// <summary>Show or hide a mapping glyph, in the given state.</summary>
		public void SetImage(MapCode code, NavImageType type, bool show)
		{
			code = NavImages.GetNameCode(code);
			var name = NavImages.GetName(code, type);
			var target = _States.ContainsKey(code) ? _States[code] : new GlyphState();
			// Nothing to repaint when neither the artwork nor its visibility moved.
			if (target.Type == type && target.Show == show)
				return;
			_States[code] = new GlyphState { Type = type, Show = show };
			if (NavImages.Exists(name))
				Invalidate();
		}

		/// <summary>Grey the controller out when no device is mapped.</summary>
		public void SetEnabled(bool enabled)
		{
			if (_Enabled == enabled)
				return;
			_Enabled = enabled;
			Invalidate();
		}

		/// <summary>Thumbstick position, each axis from -1 (left, down) to 1 (right, up).</summary>
		public void SetThumbPosition(bool isLeft, float x, float y)
		{
			var value = new PointF(Clamp(x), Clamp(y));
			if (isLeft)
				_LeftThumb = value;
			else
				_RightThumb = value;
			Invalidate();
		}

		/// <summary>Trigger travel, from 0 (released) to 1 (fully pressed).</summary>
		public void SetTriggerLevel(bool isLeft, float level)
		{
			level = level < 0f ? 0f : level > 1f ? 1f : level;
			if (isLeft)
				_LeftTrigger = level;
			else
				_RightTrigger = level;
			Invalidate();
		}

		static float Clamp(float value)
		{
			return value < -1f ? -1f : value > 1f ? 1f : value;
		}

		PointF _LeftThumb, _RightThumb;
		float _LeftTrigger, _RightTrigger;

		#endregion

		#region Help text

		/// <summary>Show a short instruction under the controller; it clears itself.</summary>
		public void SetHelpText(string text = null)
		{
			_HelpText = text ?? "";
			Invalidate();
			if (string.IsNullOrEmpty(_HelpText))
				return;
			ControlsHelper.BeginInvoke(() =>
			{
				_HelpText = "";
				Invalidate();
			}, 4000);
		}

		string _HelpText = "";

		#endregion

		#region Painting

		/// <summary>Canvas units to control pixels, keeping the artwork's proportions.</summary>
		float Scale
		{
			get
			{
				var byWidth = Width / (float)CanvasWidth;
				var byHeight = Height / (float)CanvasHeight;
				return Math.Max(0.01f, Math.Min(byWidth, byHeight));
			}
		}

		Rectangle ToScreen(Point centre, int sizeInCanvasUnits)
		{
			var scale = Scale;
			var size = Math.Max(1, (int)Math.Round(sizeInCanvasUnits * scale));
			var x = (int)Math.Round(centre.X * scale) - size / 2;
			var y = (int)Math.Round(centre.Y * scale) - size / 2;
			return new Rectangle(x, y, size, size);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (_Top == null || _Front == null)
				return;
			var scale = Scale;
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

			var top = _Enabled ? _Top : _TopDisabled;
			var front = _Enabled ? _Front : _FrontDisabled;
			e.Graphics.DrawImage(top, new Rectangle(0, 0,
				(int)Math.Round(CanvasWidth * scale), (int)Math.Round(TopImageHeight * scale)));
			e.Graphics.DrawImage(front, new Rectangle(0,
				(int)Math.Round((TopImageHeight + ImageGap) * scale),
				(int)Math.Round(CanvasWidth * scale),
				(int)Math.Round((CanvasHeight - TopImageHeight - ImageGap) * scale)));

			if (_Infos != null)
				foreach (var info in _Infos)
					DrawGlyph(e.Graphics, info);

			if (_Enabled)
				DrawIndicators(e.Graphics);

			DrawHelpText(e.Graphics);
		}

		void DrawGlyph(Graphics g, ImageInfo info)
		{
			// An axis shares its glyph with a direction; only the direction draws it.
			if (NavImages.GetNameCode(info.Code) != info.Code)
				return;
			GlyphState state;
			if (!_States.TryGetValue(info.Code, out state))
				return;
			var opacity = state.Show
				? ShownOpacity
				: state.Type == NavImageType.Record ? RecordIdleOpacity : 0f;
			if (opacity <= 0f)
				return;
			var bounds = ToScreen(GlyphPoint(info), GlyphSize);
			var glyph = NavImages.Get(info.Code, state.Type, bounds.Width);
			ControlsHelper.DrawImageWithOpacity(g, glyph, bounds, opacity);
		}

		void DrawIndicators(Graphics g)
		{
			if (_Infos == null)
				return;
			DrawThumb(g, MapCode.LeftThumbButton, _LeftThumb);
			DrawThumb(g, MapCode.RightThumbButton, _RightThumb);
			DrawTrigger(g, MapCode.LeftTrigger, _LeftTrigger);
			DrawTrigger(g, MapCode.RightTrigger, _RightTrigger);
		}

		/// <summary>Travel of a thumbstick indicator from its centre, in canvas units.</summary>
		public const int ThumbTravel = 14;

		/// <summary>
		/// Where a thumbstick mark sits, each axis from -1 to 1.
		/// </summary>
		/// <remarks>Pure, so the travel can be checked without drawing anything.</remarks>
		public static Point ThumbMarkPoint(Point centre, PointF position)
		{
			// Screen coordinates run down the way, so up on the stick is negative here.
			return new Point(
				centre.X + (int)Math.Round(Clamp(position.X) * ThumbTravel),
				centre.Y - (int)Math.Round(Clamp(position.Y) * ThumbTravel));
		}

		void DrawThumb(Graphics g, MapCode code, PointF position)
		{
			var info = _Infos.FirstOrDefault(x => x.Code == code);
			if (info == null)
				return;
			var bounds = ToScreen(ThumbMarkPoint(CanvasPoint(info), position), IndicatorSize);
			ControlsHelper.DrawImageWithOpacity(g,
				NavImages.Get("NavAxisActive", bounds.Width), bounds, ShownOpacity);
		}

		/// <summary>Where a trigger bar rests, below its arrow, in canvas units.</summary>
		public const int TriggerRestOffset = 24;

		/// <summary>How far a trigger bar rises when the trigger is fully pressed.</summary>
		public const int TriggerTravel = 26;

		/// <summary>
		/// Where a trigger bar sits for a given amount of travel, from 0 released to 1 pressed.
		/// </summary>
		/// <remarks>Pure, so the travel can be checked without drawing anything.</remarks>
		public static Point TriggerBarPoint(Point buttonCentre, float level)
		{
			level = level < 0f ? 0f : level > 1f ? 1f : level;
			// Rest is below the arrow; pressing lifts the bar towards the top of the shoulder.
			return new Point(buttonCentre.X,
				buttonCentre.Y + TriggerRestOffset - (int)Math.Round(level * TriggerTravel));
		}

		/// <summary>
		/// Draw a trigger bar at the height its trigger is pressed to.
		/// </summary>
		/// <remarks>
		/// The bar is always on screen and slides, which is what makes it read as travel. Hiding
		/// it while the trigger is released turns the same thing into a blink.
		/// </remarks>
		void DrawTrigger(Graphics g, MapCode code, float level)
		{
			var info = _Infos.FirstOrDefault(x => x.Code == code);
			if (info == null)
				return;
			var bounds = ToScreen(TriggerBarPoint(CanvasPoint(info), level), IndicatorSize);
			ControlsHelper.DrawImageWithOpacity(g,
				NavImages.Get("NavTriggerActive", bounds.Width), bounds, ShownOpacity);
		}

		void DrawHelpText(Graphics g)
		{
			if (string.IsNullOrEmpty(_HelpText))
				return;
			using (var brush = new SolidBrush(Color.Green))
			using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far })
				g.DrawString(_HelpText, Font, brush, new RectangleF(0, 0, Width, Height), format);
		}

		#endregion

		#region Pointer

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (_Imager == null || ControlsHelper.IsDesignMode(this))
				return;
			var scale = Scale;
			var point = new Point((int)Math.Round(e.X / scale), (int)Math.Round(e.Y / scale));
			var before = HoverSignature();
			_Imager.ShowLeftThumbButtons = InGroup(nameof(SettingsConverter.LeftThumbCodes), point);
			_Imager.ShowRightThumbButtons = InGroup(nameof(SettingsConverter.RightThumbCodes), point);
			_Imager.ShowDPadButtons = InGroup(nameof(SettingsConverter.DPadCodes), point);
			_Imager.ShowMainButtons = InGroup(nameof(SettingsConverter.MainButtonCodes), point);
			_Imager.ShowMenuButtons = InGroup(nameof(SettingsConverter.MenuButtonCodes), point);
			_Imager.ShowTriggerButtons = InGroup(nameof(SettingsConverter.TriggerButtonCodes), point);
			_Imager.ShowShoulderButtons = InGroup(nameof(SettingsConverter.ShoulderButtonCodes), point);
			if (HoverSignature() != before)
				Invalidate();
		}

		string HoverSignature()
		{
			if (_Imager == null)
				return "";
			return string.Concat(
				_Imager.ShowLeftThumbButtons, _Imager.ShowRightThumbButtons, _Imager.ShowDPadButtons,
				_Imager.ShowMainButtons, _Imager.ShowMenuButtons, _Imager.ShowTriggerButtons,
				_Imager.ShowShoulderButtons);
		}

		bool InGroup(string name, Point canvasPoint)
		{
			Rectangle bounds;
			return _Groups.TryGetValue(name, out bounds) && bounds.Contains(canvasPoint);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (_Imager == null)
				return;
			_Imager.ShowLeftThumbButtons = false;
			_Imager.ShowRightThumbButtons = false;
			_Imager.ShowDPadButtons = false;
			_Imager.ShowMainButtons = false;
			_Imager.ShowMenuButtons = false;
			_Imager.ShowTriggerButtons = false;
			_Imager.ShowShoulderButtons = false;
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button != MouseButtons.Left || _Infos == null || _Imager == null)
				return;
			var info = HitTest(e.Location);
			if (info == null)
				return;
			var map = SettingsManager.Current.SettingsMap
				.FirstOrDefault(x => x.MapTo == _MappedTo && x.Code == info.Code);
			if (map == null)
				return;
			var record = true;
			// Clicking the mapping already being recorded stops rather than restarts it.
			if (_Imager.Recorder.Recording)
			{
				var current = _Imager.Recorder.CurrentMap;
				if (StopRecording != null)
					StopRecording();
				record = map != current;
			}
			if (record && StartRecording != null)
				StartRecording(map);
		}

		/// <summary>Mapping under the pointer, or null. Nearest wins where glyphs overlap.</summary>
		public ImageInfo HitTest(Point location)
		{
			ImageInfo best = null;
			var bestDistance = double.MaxValue;
			foreach (var info in _Infos)
			{
				var bounds = ToScreen(GlyphPoint(info), GlyphSize);
				if (!bounds.Contains(location))
					continue;
				var dx = location.X - (bounds.X + bounds.Width / 2.0);
				var dy = location.Y - (bounds.Y + bounds.Height / 2.0);
				var distance = dx * dx + dy * dy;
				if (distance >= bestDistance)
					continue;
				bestDistance = distance;
				best = info;
			}
			return best;
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_Top != null) _Top.Dispose();
				if (_Front != null) _Front.Dispose();
				if (_TopDisabled != null) _TopDisabled.Dispose();
				if (_FrontDisabled != null) _FrontDisabled.Dispose();
			}
			base.Dispose(disposing);
		}

	}
}
