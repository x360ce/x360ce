using System.Windows;
using System.Linq;
using System;
using System.Windows.Media;
using System.IO;
using System.Data;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class ControlsHelper
	{

		/// <summary>
		/// Set form TopMost if one of the application forms is top most.
		/// </summary>
		/// <param name="win"></param>
		public static void CheckTopMost(Window win)
		{
			// If this form is not set as TopMost but one of the application forms is on TopMost then...
			// Make this dialog form TopMost too or user won't be able to access it.
			if (!win.Topmost && System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Any(x => x.TopMost))
				win.Topmost = true;
		}

		public static void AutoSizeByOpenForms(Window win, int addSize = -64)
		{
			var form = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().FirstOrDefault();
			if (form == null)
				return;
			win.Width = form.Width + addSize;
			win.Height = form.Height + addSize;
			win.Top = form.Top - addSize / 2;
			win.Left = form.Left - addSize / 2;
		}

		/// <summary>
		/// Convert Bitmap to image source.
		/// </summary>
		///	<remarks>
		///	Requires NuGet Package on .NET Core: System.Drawing.Common or...
		///	set property <UseWindowsForms>true</UseWindowsForms> inside the project.
		///	</remarks>
		public static ImageSource GetImageSource(System.Drawing.Bitmap bitmap)
		{
			var bi = new System.Windows.Media.Imaging.BitmapImage();
			var ms = new MemoryStream();
			bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
			bi.BeginInit();
			bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
			bi.StreamSource = ms;
			bi.EndInit();
			ms.Dispose();
			return bi;
		}

		#region Center Window

		/// <summary>
		/// Center window on Owner window.
		/// </summary>
		/// <param name="window"></param>
		public static void CenterWindowOnApplication(Window window)
		{
			// Get WFF window first.
			var win = window.Owner;
			System.Drawing.Rectangle? r = null;
			var isNormal = false;
			if (win != null)
			{
				r = new System.Drawing.Rectangle((int)win.Left, (int)win.Top, (int)win.Width, (int)win.Height);
				isNormal = win.WindowState == WindowState.Normal;
			}
			else
			{
				// Try to get top windows form.
				var form = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().FirstOrDefault();
				if (form != null)
				{
					double l;
					double t;
					double w;
					double h;
					TransformToUnits(form.Left, form.Top, out l, out t);
					TransformToUnits(form.Width, form.Height, out w, out h);
					r = new System.Drawing.Rectangle((int)l, (int)t, (int)w, (int)h);
					isNormal = form.WindowState == System.Windows.Forms.FormWindowState.Normal;
				}
			}
			if (r.HasValue)
			{
				if (isNormal)
				{
					window.Left = r.Value.X + ((r.Value.Width - window.ActualWidth) / 2);
					window.Top = r.Value.Y + ((r.Value.Height - window.ActualHeight) / 2);
				}
				else
				{
					// Get the form screen.
					var screen = System.Windows.Forms.Screen.FromRectangle(r.Value);
					double screenWidth = screen.WorkingArea.Width;
					double screenHeight = screen.WorkingArea.Height;
					window.Left = (screenWidth / 2) - (window.Width / 2);
					window.Top = (screenHeight / 2) - (window.Height / 2);
				}
			}
		}

		/// <summary>
		/// Transforms device independent units (1/96 of an inch) to pixels.
		/// </summary>
		private static void TransformToPixels(double unitX, double unitY, out int pixelX, out int pixelY)
		{
			using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
			{
				pixelX = (int)((g.DpiX / 96) * unitX);
				pixelY = (int)((g.DpiY / 96) * unitY);
			}
		}

		/// <summary>
		/// Transforms device pixels to independent units (1/96 of an inch).
		/// </summary>
		private static void TransformToUnits(int pixelX, int pixelY, out double unitX, out double unitY)
		{
			using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
			{
				unitX = (double)pixelX / (g.DpiX / 96);
				unitY = (double)pixelY / (g.DpiX / 96);
			}
		}

		public static bool GetMainFormTopMost()
		{
			var win = System.Windows.Application.Current?.MainWindow;
			if (win != null)
				return win.Topmost;
			var form = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().FirstOrDefault();
			if (form != null)
				return form.TopMost;
			return false;
		}

		#endregion


	}
}
