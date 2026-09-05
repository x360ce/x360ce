using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace x360ce.Setup
{
	public static class UiHelper
	{
		[DllImport("dwmapi.dll", PreserveSig = true)]
		public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
		public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
		public const int DWMWCP_ROUND = 2;

		public static void ApplyModernWindowTheme(IntPtr handle)
		{
			try
			{
				int cornerPref = DWMWCP_ROUND;
				DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
			}
			catch { }
		}

		public static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
		{
			var path = new GraphicsPath();
			if (radius <= 0)
			{
				path.AddRectangle(rect);
				return path;
			}

			int diameter = radius * 2;
			if (diameter > rect.Width) diameter = rect.Width;
			if (diameter > rect.Height) diameter = rect.Height;

			var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

			// Top-left
			path.AddArc(arc, 180, 90);

			// Top-right
			arc.X = rect.Right - diameter;
			path.AddArc(arc, 270, 90);

			// Bottom-right
			arc.Y = rect.Bottom - diameter;
			path.AddArc(arc, 0, 90);

			// Bottom-left
			arc.X = rect.Left;
			path.AddArc(arc, 90, 90);

			path.CloseFigure();
			return path;
		}

		public static Color InterpolateColor(Color c1, Color c2, float t)
		{
			t = Math.Max(0f, Math.Min(1f, t));
			int r = (int)(c1.R + (c2.R - c1.R) * t);
			int g = (int)(c1.G + (c2.G - c1.G) * t);
			int b = (int)(c1.B + (c2.B - c1.B) * t);
			return Color.FromArgb(r, g, b);
		}

		public static Color GetEffectiveBackColor(Control control)
		{
			Control cur = control?.Parent;
			while (cur != null)
			{
				if (cur is X360ceCard card)
					return card.CardBackColor;
				if (cur.BackColor != Color.Transparent && cur.BackColor.A == 255)
					return cur.BackColor;
				cur = cur.Parent;
			}
			return Color.FromArgb(243, 244, 246);
		}
	}

	/// <summary>
	/// Signature x360ce Top Banner matching BaseFormWithHeader.
	/// </summary>
	public class X360ceHeaderBanner : Panel
	{
		public string Subject { get; set; } = "x360ce Game Setup & Controller Optimizer";
		public string Description { get; set; } = "Useful Tip: Select your game folder below to automatically deploy emulator files and calibrate Player 1 & 2 gamepads.";
		public Image AppIconImage { get; set; }

		public X360ceHeaderBanner()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			Height = 84;
			Dock = DockStyle.Top;
			BackColor = SystemColors.Info; // Authentic warm cream banner like x360ce

			try
			{
				var asm = System.Reflection.Assembly.GetExecutingAssembly();
				using (var s = asm.GetManifestResourceStream("x360ce.Setup.app.ico"))
				{
					if (s != null)
					{
						using (var ico = new Icon(s, 48, 48))
						{
							AppIconImage = ico.ToBitmap();
						}
					}
				}
			}
			catch { }
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			// Background fill
			using (var brush = new SolidBrush(BackColor))
			{
				e.Graphics.FillRectangle(brush, ClientRectangle);
			}

			// Subtle bottom separator line
			using (var pen = new Pen(Color.FromArgb(218, 220, 224), 1f))
			{
				e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
			}

			// Draw Info Icon Circle (28x28)
			int iconX = 14;
			int iconY = 16;
			using (var brush = new SolidBrush(Color.FromArgb(37, 99, 235))) // Windows Info Blue
			{
				e.Graphics.FillEllipse(brush, iconX, iconY, 28, 28);
			}
			using (var font = new Font("Georgia", 13f, FontStyle.Bold))
			using (var brush = new SolidBrush(Color.White))
			{
				var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
				e.Graphics.DrawString("i", font, brush, new RectangleF(iconX, iconY, 28, 28), sf);
			}

			// Draw Subject
			float textLeft = 52;
			using (var font = new Font("Segoe UI", 10.5f, FontStyle.Bold))
			using (var brush = new SolidBrush(Color.FromArgb(30, 41, 59)))
			{
				var sf = new StringFormat { HotkeyPrefix = HotkeyPrefix.None };
				e.Graphics.DrawString(Subject, font, brush, new PointF(textLeft, 14), sf);
			}

			// Draw Right Controller Graphic if available
			int rightMargin = 16;
			if (AppIconImage != null)
			{
				int imgSize = 48;
				int imgX = Width - imgSize - 16;
				int imgY = (Height - imgSize) / 2;
				e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				e.Graphics.DrawImage(AppIconImage, new Rectangle(imgX, imgY, imgSize, imgSize));
				rightMargin = 72;
			}

			// Draw Description with ample height and measured width so 2 lines never crop
			using (var font = new Font("Segoe UI", 8.8f, FontStyle.Regular))
			using (var brush = new SolidBrush(Color.FromArgb(71, 85, 105)))
			{
				var sf = new StringFormat { HotkeyPrefix = HotkeyPrefix.None };
				e.Graphics.DrawString(Description, font, brush, new RectangleF(textLeft, 36, Width - textLeft - rightMargin, 42), sf);
			}

			base.OnPaint(e);
		}
	}

	/// <summary>
	/// Clean, rounded card container with high-DPI border and drop-highlight.
	/// </summary>
	public class X360ceCard : Panel
	{
		public int CornerRadius { get; set; } = 8;
		public Color CardBackColor { get; set; } = Color.White;
		public Color BorderColor { get; set; } = Color.FromArgb(203, 213, 225);
		public Color HighlightBorderColor { get; set; } = Color.FromArgb(37, 99, 235);
		public bool IsHighlighted { get; set; }
		public string CardTitle { get; set; }

		public X360ceCard()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			Padding = new Padding(14, 26, 14, 14);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			Color parentBg = UiHelper.GetEffectiveBackColor(this);
			using (var bgBrush = new SolidBrush(parentBg))
			{
				e.Graphics.FillRectangle(bgBrush, ClientRectangle);
			}

			var rect = new Rectangle(0, 0, Width - 1, Height - 1);
			using (var path = UiHelper.CreateRoundedRectangle(rect, CornerRadius))
			{
				using (var brush = new SolidBrush(CardBackColor))
				{
					e.Graphics.FillPath(brush, path);
				}

				var strokeColor = IsHighlighted ? HighlightBorderColor : BorderColor;
				using (var pen = new Pen(strokeColor, IsHighlighted ? 2f : 1f))
				{
					e.Graphics.DrawPath(pen, path);
				}
			}

			if (!string.IsNullOrEmpty(CardTitle))
			{
				using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
				using (var brush = new SolidBrush(IsHighlighted ? HighlightBorderColor : Color.FromArgb(15, 23, 42)))
				{
					var sf = new StringFormat { HotkeyPrefix = HotkeyPrefix.None };
					e.Graphics.DrawString(CardTitle, titleFont, brush, new PointF(14, 8), sf);
				}
			}

			base.OnPaint(e);
		}
	}

	/// <summary>
	/// Clean native-style rounded button with smooth hover animation and high-DPI text.
	/// </summary>
	public class X360ceButton : Button
	{
		public int CornerRadius { get; set; } = 6;
		public Color NormalColor { get; set; } = Color.FromArgb(37, 99, 235);
		public Color HoverColor { get; set; } = Color.FromArgb(29, 78, 216);
		public Color PressedColor { get; set; } = Color.FromArgb(30, 64, 175);
		public Color BorderColor { get; set; } = Color.FromArgb(29, 78, 216);

		private bool _isHovered;
		private bool _isPressed;
		private float _hoverProgress = 0f;
		private readonly Timer _animTimer;

		public X360ceButton()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			FlatStyle = FlatStyle.Flat;
			FlatAppearance.BorderSize = 0;
			Cursor = Cursors.Hand;
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
			ForeColor = Color.White;
			UseMnemonic = false;

			_animTimer = new Timer { Interval = 16 };
			_animTimer.Tick += (s, e) =>
			{
				float target = _isHovered ? 1f : 0f;
				if (Math.Abs(_hoverProgress - target) < 0.05f)
				{
					_hoverProgress = target;
					_animTimer.Stop();
				}
				else
				{
					_hoverProgress += (_isHovered ? 0.2f : -0.2f);
					_hoverProgress = Math.Max(0f, Math.Min(1f, _hoverProgress));
				}
				Invalidate();
			};
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_isHovered = true;
			_animTimer.Start();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_isHovered = false;
			_animTimer.Start();
		}

		protected override void OnMouseDown(MouseEventArgs mevent)
		{
			base.OnMouseDown(mevent);
			if (mevent.Button == MouseButtons.Left)
			{
				_isPressed = true;
				Invalidate();
			}
		}

		protected override void OnMouseUp(MouseEventArgs mevent)
		{
			base.OnMouseUp(mevent);
			_isPressed = false;
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs pevent)
		{
			pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			pevent.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			pevent.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			// Erase background with parent background color to eliminate dark/black sharp corner pixels
			Color parentBg = UiHelper.GetEffectiveBackColor(this);
			using (var bgBrush = new SolidBrush(parentBg))
			{
				pevent.Graphics.FillRectangle(bgBrush, ClientRectangle);
			}

			Color currentBg;
			if (!Enabled)
			{
				currentBg = Color.FromArgb(226, 232, 240);
			}
			else if (_isPressed)
			{
				currentBg = PressedColor;
			}
			else
			{
				currentBg = UiHelper.InterpolateColor(NormalColor, HoverColor, _hoverProgress);
			}

			var rect = new Rectangle(0, 0, Width - 1, Height - 1);
			if (_isPressed)
			{
				rect.Y += 1;
				rect.Height -= 1;
			}

			using (var path = UiHelper.CreateRoundedRectangle(rect, CornerRadius))
			{
				using (var brush = new SolidBrush(currentBg))
				{
					pevent.Graphics.FillPath(brush, path);
				}

				if (Enabled)
				{
					using (var pen = new Pen(BorderColor, 1f))
					{
						pevent.Graphics.DrawPath(pen, path);
					}
				}
			}

			var sf = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center,
				HotkeyPrefix = HotkeyPrefix.None // Prevents & from being eaten!
			};

			var textRect = new RectangleF(0, _isPressed ? 1 : 0, Width, Height);
			var textColor = Enabled ? ForeColor : Color.FromArgb(148, 163, 184);
			using (var brush = new SolidBrush(textColor))
			{
				pevent.Graphics.DrawString(Text, Font, brush, textRect, sf);
			}
		}
	}

	/// <summary>
	/// Authentic x360ce Controller Status Grid matching the main app's Devices table.
	/// </summary>
	public class X360ceDeviceGrid : Control
	{
		public string P1Name { get; set; } = "Twin USB Gamepad";
		public string P1HwId { get; set; } = "VID: 0x0810  PID: 0x0001";
		public bool P1Online { get; set; } = true;

		public string P2Name { get; set; } = "Twin USB Gamepad";
		public string P2HwId { get; set; } = "VID: 0x0810  PID: 0x0001";
		public bool P2Online { get; set; } = true;

		public X360ceDeviceGrid()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			Height = 80;
			Width = 736;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			Color parentBg = UiHelper.GetEffectiveBackColor(this);
			using (var bgBrush = new SolidBrush(parentBg))
			{
				e.Graphics.FillRectangle(bgBrush, ClientRectangle);
			}

			var outerRect = new Rectangle(0, 0, Width - 1, Height - 1);

			// Draw border with 6px rounded corners
			using (var path = UiHelper.CreateRoundedRectangle(outerRect, 6))
			{
				using (var brush = new SolidBrush(Color.White))
				{
					e.Graphics.FillPath(brush, path);
				}
				using (var pen = new Pen(Color.FromArgb(203, 213, 225), 1f))
				{
					e.Graphics.DrawPath(pen, path);
				}
			}

			// Header row background
			int headerHeight = 24;
			var headerRect = new Rectangle(1, 1, Width - 2, headerHeight);
			using (var brush = new SolidBrush(Color.FromArgb(241, 245, 249)))
			{
				e.Graphics.FillRectangle(brush, headerRect);
			}
			using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
			{
				e.Graphics.DrawLine(pen, 0, headerHeight + 1, Width, headerHeight + 1);
			}

			int pad = 14;
			int playerX = pad;
			int statusX = 82;
			int statusW = 85;

			int pollW = 200;
			int pollX = Width - pollW - pad;

			int hwIdW = 165;
			int hwIdX = pollX - hwIdW - 12;

			int nameX = statusX + statusW + 12;
			int nameW = Math.Max(110, hwIdX - nameX - 12);

			// Column headers with dynamic, ample spacing
			using (var font = new Font("Segoe UI", 8.2f, FontStyle.Bold))
			using (var brush = new SolidBrush(Color.FromArgb(71, 85, 105)))
			{
				e.Graphics.DrawString("PLAYER", font, brush, new PointF(playerX, 5));
				e.Graphics.DrawString("STATUS", font, brush, new PointF(statusX, 5));
				e.Graphics.DrawString("DEVICE NAME", font, brush, new PointF(nameX, 5));
				e.Graphics.DrawString("HARDWARE ID", font, brush, new PointF(hwIdX, 5));
				e.Graphics.DrawString("POLLING & CALIBRATION", font, brush, new PointF(pollX, 5));
			}

			// Row 1: Player 1
			int row1Y = 26;
			int rowHeight = 25;
			DrawDeviceRow(e.Graphics, 1, "Player 1", P1Online, P1Name, P1HwId, "1000 Hz • Auto-Calibrated", row1Y, false, playerX, statusX, nameX, nameW, hwIdX, hwIdW, pollX, pollW);

			// Divider line
			using (var pen = new Pen(Color.FromArgb(241, 245, 249), 1f))
			{
				e.Graphics.DrawLine(pen, 1, row1Y + rowHeight, Width - 2, row1Y + rowHeight);
			}

			// Row 2: Player 2
			int row2Y = row1Y + rowHeight + 1;
			DrawDeviceRow(e.Graphics, 2, "Player 2", P2Online, P2Name, P2HwId, "1000 Hz • Auto-Calibrated", row2Y, true, playerX, statusX, nameX, nameW, hwIdX, hwIdW, pollX, pollW);
		}

		private void DrawDeviceRow(Graphics g, int pNum, string player, bool online, string name, string hwId, string mapping, int y, bool isAlt,
			int playerX, int statusX, int nameX, int nameW, int hwIdX, int hwIdW, int pollX, int pollW)
		{
			if (isAlt)
			{
				using (var brush = new SolidBrush(Color.FromArgb(248, 250, 252)))
				{
					g.FillRectangle(brush, new Rectangle(1, y, Width - 2, 25));
				}
			}

			// Player Tag Pill
			var pillRect = new Rectangle(playerX, y + 3, 56, 18);
			using (var pPath = UiHelper.CreateRoundedRectangle(pillRect, 4))
			using (var brush = new SolidBrush(pNum == 1 ? Color.FromArgb(37, 99, 235) : Color.FromArgb(16, 185, 129)))
			{
				g.FillPath(brush, pPath);
			}
			using (var font = new Font("Segoe UI", 7.8f, FontStyle.Bold))
			using (var brush = new SolidBrush(Color.White))
			{
				var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
				g.DrawString(player, font, brush, pillRect, sf);
			}

			// Status LED dot + text
			int ledX = statusX;
			int ledY = y + 8;
			Color statusColor = online ? Color.FromArgb(22, 163, 74) : Color.FromArgb(217, 119, 6);
			using (var brush = new SolidBrush(statusColor))
			{
				g.FillEllipse(brush, ledX, ledY, 8, 8);
			}
			using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
			using (var brush = new SolidBrush(statusColor))
			{
				g.DrawString(online ? "Online" : "Linked", font, brush, new PointF(ledX + 12, y + 4));
			}

			var sfCell = new StringFormat
			{
				Trimming = StringTrimming.EllipsisCharacter,
				FormatFlags = StringFormatFlags.NoWrap,
				LineAlignment = StringAlignment.Center
			};

			// Device Name
			using (var font = new Font("Segoe UI", 8.8f, FontStyle.Regular))
			using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
			{
				g.DrawString(name, font, brush, new RectangleF(nameX, y + 4, nameW, 18), sfCell);
			}

			// Hardware ID
			using (var font = new Font("Segoe UI", 8.2f, FontStyle.Regular))
			using (var brush = new SolidBrush(Color.FromArgb(100, 116, 139)))
			{
				g.DrawString(hwId, font, brush, new RectangleF(hwIdX, y + 4, hwIdW, 18), sfCell);
			}

			// Mapping & Polling
			using (var font = new Font("Segoe UI", 8.2f, FontStyle.Bold))
			using (var brush = new SolidBrush(Color.FromArgb(16, 185, 129)))
			{
				g.DrawString(mapping, font, brush, new RectangleF(pollX, y + 4, pollW, 18), sfCell);
			}
		}
	}

	/// <summary>
	/// Smooth progress bar with rounded corners matching modern Windows styling.
	/// </summary>
	public class X360ceProgressBar : Control
	{
		public int CornerRadius { get; set; } = 4;
		public int Value { get; set; } = 0;
		public int Maximum { get; set; } = 100;
		public Color TrackColor { get; set; } = Color.FromArgb(226, 232, 240);
		public Color FillColor { get; set; } = Color.FromArgb(37, 99, 235);

		public X360ceProgressBar()
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			Height = 10;
		}

		public void SetProgress(int value)
		{
			Value = Math.Max(0, Math.Min(Maximum, value));
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			Color parentBg = UiHelper.GetEffectiveBackColor(this);
			using (var bgBrush = new SolidBrush(parentBg))
			{
				e.Graphics.FillRectangle(bgBrush, ClientRectangle);
			}

			var rect = new Rectangle(0, 0, Width - 1, Height - 1);

			using (var path = UiHelper.CreateRoundedRectangle(rect, CornerRadius))
			{
				using (var brush = new SolidBrush(TrackColor))
				{
					e.Graphics.FillPath(brush, path);
				}
			}

			if (Value > 0 && Maximum > 0 && Width > 8)
			{
				int fillWidth = (int)((float)Value / Maximum * (Width - 1));
				if (fillWidth > 4)
				{
					var fillRect = new Rectangle(0, 0, fillWidth, Height - 1);
					using (var path = UiHelper.CreateRoundedRectangle(fillRect, CornerRadius))
					using (var brush = new LinearGradientBrush(fillRect, FillColor, Color.FromArgb(16, 185, 129), LinearGradientMode.Horizontal))
					{
						e.Graphics.FillPath(brush, path);
					}
				}
			}
		}
	}
}
