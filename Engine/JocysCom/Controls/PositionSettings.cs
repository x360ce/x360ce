using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{
	/*
	// Subscribe to the DisplaySettingsChanged event
	SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

	private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
	{
		// Adjust window position if it's outside the virtual screen bounds
		SavePosition(this);
		LoadPosition(this);
	}
	*/

	/// <summary>
	/// Save and restore the position and state of a window. Supports multiple screens and DPI scaling.
	/// If the window parts are outside of the visible working area, moves the window into the visible working area.
	/// </summary>
	/// <remarks>
	/// Split across three files so the same rules serve both interface toolkits:
	///
	/// - this file  - what is remembered, and the screen arithmetic. No toolkit types at all.
	/// - .Xaml.cs   - Window, Visual, Grid. Needs WPF.
	/// - .Forms.cs  - Form. Needs Windows Forms.
	///
	/// A project takes this file plus the one for the toolkit it uses. Geometry is kept in
	/// RectangleF rather than a WPF rectangle so that taking the Windows Forms pair pulls in no
	/// WPF assemblies, which is the whole point of the split.
	///
	/// Each toolkit measures in its own units - WPF in device-independent units, Windows Forms in
	/// pixels - so each hands in screen bounds already expressed in the units its own window uses,
	/// and the arithmetic here never has to know which it was given.
	/// </remarks>
	public partial class PositionSettings
	{
		public double Left { get; set; }
		public double Top { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public string ScreenName { get; set; }
		public bool IsEnabled { get; set; }

		/// <summary>Whether the window was left normal, minimised or maximised.</summary>
		/// <remarks>
		/// Its own three values rather than either toolkit's, because the two name the same three
		/// states in different types. Each toolkit part converts, and what is stored stays the same
		/// whichever one wrote it.
		/// </remarks>
		public PositionState State { get; set; }

		public event EventHandler PositionLoaded;

		public void RaisePositionLoaded()
			=> PositionLoaded?.Invoke(this, EventArgs.Empty);

		#region What is remembered

		/// <summary>Records a window's place. Bounds are in the units the caller's toolkit uses.</summary>
		protected void SaveBounds(RectangleF bounds, PositionState state, IList<ScreenArea> screens)
		{
			State = state;
			Left = bounds.Left;
			Top = bounds.Top;
			Width = bounds.Width;
			Height = bounds.Height;
			var on = screens.FirstOrDefault(x => x.Bounds.IntersectsWith(bounds));
			ScreenName = (on ?? screens.FirstOrDefault())?.Name;
			// Enable settings for loading.
			IsEnabled = true;
		}

		/// <summary>
		/// Works out where the window should go, moving it onto a visible screen if the place it
		/// was left no longer exists or is no longer reachable.
		/// </summary>
		/// <param name="screens">Every screen, in the same units as the window's own bounds.</param>
		/// <param name="min">Smallest the window may be, or empty for no limit.</param>
		/// <param name="max">Largest the window may be, or empty for no limit.</param>
		protected RectangleF LoadBounds(IList<ScreenArea> screens, SizeF min, SizeF max)
		{
			var width = Width;
			var height = Height;
			// Clamp the width and height to the specified minimum and maximum values of the form.
			if (min.Width > 0) width = Math.Max(width, min.Width);
			if (min.Height > 0) height = Math.Max(height, min.Height);
			if (max.Width > 0) width = Math.Min(width, max.Width);
			if (max.Height > 0) height = Math.Min(height, max.Height);
			var bounds = new RectangleF((float)Left, (float)Top, (float)width, (float)height);
			if (screens.Count == 0)
				return bounds;

			// Move the window into the multi-monitor virtual screen area if it's outside of it.
			var all = Union(screens.Select(x => x.Bounds));
			if (bounds.Left < all.Left)
				bounds.X = all.Left;
			if (bounds.Top < all.Top)
				bounds.Y = all.Top;
			if (bounds.Right > all.Right)
				bounds.X = all.Right - bounds.Width;
			if (bounds.Bottom > all.Bottom)
				bounds.Y = all.Bottom - bounds.Height;

			// Move the window into the screen working area (the part not taken by the taskbar) if
			// it is outside of it.
			var screen = screens.FirstOrDefault(x => x.Bounds.IntersectsWith(bounds));
			if (screen != null)
			{
				var work = screen.WorkingArea;
				// Make sure the width and height fits within the working area.
				bounds.Width = Math.Min(bounds.Width, work.Width);
				bounds.Height = Math.Min(bounds.Height, work.Height);
				if (!IsFullyVisible(bounds, screens))
				{
					var top = Math.Max(work.Top, Math.Min(bounds.Top, work.Bottom - bounds.Height));
					bounds = new RectangleF(bounds.Left, top, bounds.Width, bounds.Height);
				}
				if (!IsFullyVisible(bounds, screens))
				{
					var left = Math.Max(work.Left, Math.Min(bounds.Left, work.Right - bounds.Width));
					bounds = new RectangleF(left, bounds.Top, bounds.Width, bounds.Height);
				}
			}
			return bounds;
		}

		static bool IsFullyVisible(RectangleF bounds, IList<ScreenArea> screens)
		{
			var union = Union(screens.Select(x => x.WorkingArea));
			var seen = RectangleF.Intersect(union, bounds);
			return seen.Width * seen.Height == bounds.Width * bounds.Height;
		}

		static RectangleF Union(IEnumerable<RectangleF> rectangles)
		{
			var union = RectangleF.Empty;
			foreach (var rectangle in rectangles)
				union = union.IsEmpty ? rectangle : RectangleF.Union(union, rectangle);
			return union;
		}

		#endregion

		#region Screens

		/// <summary>One screen, in whichever units the caller's toolkit measures windows in.</summary>
		public class ScreenArea
		{
			public string Name;
			public RectangleF Bounds;
			public RectangleF WorkingArea;
		}

		/// <summary>Every screen in pixels, which is what Windows Forms windows are measured in.</summary>
		public static IList<ScreenArea> GetScreensInPixels()
		{
			return Screen.AllScreens.Select(x => new ScreenArea
			{
				Name = x.DeviceName,
				Bounds = x.Bounds,
				WorkingArea = x.WorkingArea,
			}).ToList();
		}

		/// <summary>
		/// Every screen in device-independent units, which is what WPF windows are measured in.
		/// </summary>
		public static IList<ScreenArea> GetScreensInDiu()
		{
			return Screen.AllScreens.Select(x =>
			{
				var scale = NativeMethods.GetScaleOf(x);
				return new ScreenArea
				{
					Name = x.DeviceName,
					Bounds = Scale(x.Bounds, scale),
					WorkingArea = Scale(x.WorkingArea, scale),
				};
			}).ToList();
		}

		static RectangleF Scale(Rectangle r, PointF scale)
		{
			return new RectangleF(r.Left / scale.X, r.Top / scale.Y, r.Width / scale.X, r.Height / scale.Y);
		}

		#endregion

		#region Scaling

		/// <summary>
		/// The scaling in force at a point on the desktop, as a multiple of the usual 96 dots to
		/// the inch. Used to turn pixels into the units a toolkit lays windows out in.
		/// </summary>
		public static PointF GetScalingFactorsAtPoint(double x, double y)
		{
			var monitor = NativeMethods.MonitorFromPoint(
				new NativeMethods.POINT { x = (int)x, y = (int)y },
				NativeMethods.MONITOR_DEFAULTTONEAREST);
			if (monitor == IntPtr.Zero)
				return new PointF(1f, 1f);
			uint dpiX, dpiY;
			if (NativeMethods.GetDpiForMonitor(monitor, NativeMethods.DpiType.Effective, out dpiX, out dpiY) != 0)
				return new PointF(1f, 1f);
			return new PointF(dpiX / 96f, dpiY / 96f);
		}

		#endregion

		static internal class NativeMethods
		{
			[DllImport("user32.dll")]
			internal static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

			[DllImport("Shcore.dll")]
			internal static extern int GetDpiForMonitor(IntPtr hMonitor, DpiType dpiType, out uint dpiX, out uint dpiY);

			[DllImport("user32.dll", SetLastError = true)]
			internal static extern IntPtr MonitorFromRect(ref RECT lprcMonitor, uint dwFlags);

			[DllImport("shcore.dll")]
			public static extern int GetProcessDpiAwareness(IntPtr hprocess, out ProcessDpiAwareness value);

			internal const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

			[StructLayout(LayoutKind.Sequential)]
			internal struct POINT
			{
				public int x;
				public int y;
			}

			[StructLayout(LayoutKind.Sequential)]
			internal struct RECT
			{
				public int Left;
				public int Top;
				public int Right;
				public int Bottom;
			}

			internal enum DpiType
			{
				Effective = 0,
				Angular = 1,
				Raw = 2,
			}

			public enum ProcessDpiAwareness
			{
				Process_DPI_Unaware = 0,
				Process_System_DPI_Aware = 1,
				Process_Per_Monitor_DPI_Aware = 2
			}

			/// <summary>How much a screen is scaled by, as a multiple of 96 dots to the inch.</summary>
			internal static PointF GetScaleOf(Screen screen)
			{
				var bounds = screen.Bounds;
				var rect = new RECT
				{
					Left = bounds.Left,
					Top = bounds.Top,
					Right = bounds.Right,
					Bottom = bounds.Bottom,
				};
				var monitor = MonitorFromRect(ref rect, MONITOR_DEFAULTTONEAREST);
				if (monitor == IntPtr.Zero)
					return new PointF(1f, 1f);
				uint dpiX, dpiY;
				if (GetDpiForMonitor(monitor, DpiType.Effective, out dpiX, out dpiY) != 0)
					return new PointF(1f, 1f);
				return new PointF(dpiX / 96f, dpiY / 96f);
			}
		}
	}

	/// <summary>How a window was left, named the same way whichever toolkit drew it.</summary>
	public enum PositionState
	{
		Normal = 0,
		Minimized = 1,
		Maximized = 2,
	}
}
