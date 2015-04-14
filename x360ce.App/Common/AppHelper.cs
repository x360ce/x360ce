using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using x360ce.Engine;
using x360ce.Engine.Win32;

namespace x360ce.App
{
	public class AppHelper
	{
		#region DLL Functions

		static void Elevate()
		{
			// If this is Vista/7 and is not elevated then elevate.
			if (WinAPI.IsVista && !WinAPI.IsElevated()) WinAPI.RunElevated();
		}

		public static bool WriteFile(string resourceName, string destinationFileName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var sr = assembly.GetManifestResourceStream(resourceName);
			FileStream sw = null;
			try
			{
				sw = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write);
			}
			catch (Exception)
			{
				Elevate();
				return false;
			}
			var buffer = new byte[1024];
			while (true)
			{
				var count = sr.Read(buffer, 0, buffer.Length);
				if (count == 0) break;
				sw.Write(buffer, 0, count);
			}
			sr.Close();
			sw.Close();
			return true;
		}

		public static bool CopyFile(string sourceFileName, string destFileName)
		{
			try
			{
				File.Copy(sourceFileName, destFileName, true);
			}
			catch (Exception)
			{
				Elevate();
				return false;
			}
			return true;
		}



		/// <summary></summary>
		/// <returns>True if file exists.</returns>
		public static bool CreateDllFile(bool create, string file)
		{
			if (create)
			{
				// If file don't exist exists then...
				var present = EngineHelper.GetDefaultDll();
				if (present == null)
				{
					var xFile = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(XInputMask.XInput13_x86);
					MainForm.Current.CreateFile(EngineHelper.GetXInputResoureceName(), xFile);
				}
				else if (!System.IO.File.Exists(file))
				{
					present.CopyTo(file, true);
				}
			}
			else
			{
				if (System.IO.File.Exists(file))
				{
					try
					{
						System.IO.File.Delete(file);
					}
					catch (Exception) { }
				}
			}
			return System.IO.File.Exists(file);
		}

		#endregion

		#region Colors

		/// <summary>
		/// Make bitmap gray scale
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public static void GrayScale(Bitmap b)
		{
			int w = b.Width;
			int h = b.Height;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					Color p = b.GetPixel(x, y);
					byte c = (byte)(.299 * p.R + .587 * p.G + .114 * p.B);
					b.SetPixel(x, y, Color.FromArgb(p.A, c, c, c));
				}
			}
		}

		/// <summary>
		/// Make bitmap gray scale
		/// </summary>
		/// <param name="b"></param>
		/// <param name="alpha">256 max</param>
		/// <returns></returns>
		public static void Transparent(Bitmap b, int alpha)
		{
			int w = b.Width;
			int h = b.Height;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					Color p = b.GetPixel(x, y);
					int a = (int)((float)p.A * (float)alpha / byte.MaxValue);
					if (a >= byte.MaxValue) a = byte.MaxValue;
					if (a <= byte.MinValue) a = byte.MinValue;
					b.SetPixel(x, y, Color.FromArgb(a, p.R, p.G, p.B));
				}
			}
		}

		#endregion

		// Use special function or comparison fails.
		public static bool IsSameDevice(Device device, Guid instanceGuid)
		{
			return instanceGuid.Equals(device == null ? Guid.Empty : device.Information.InstanceGuid);
		}

	}
}
