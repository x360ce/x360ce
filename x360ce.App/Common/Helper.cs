using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Drawing;
using Microsoft.DirectX.DirectInput;

namespace x360ce.App
{
	public class Helper
	{

		public static Stream GetResource(string name)
		{
			var assembly = Assembly.GetExecutingAssembly();
			foreach (var key in assembly.GetManifestResourceNames())
			{
				if (key.Contains(name)) return assembly.GetManifestResourceStream(key);
			}
			return null;
		}

		/// <summary>
		/// Make bitmap grayscale
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
		/// Make bitmap grayscale
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

		// Use special function or comparison fails.
		public static bool IsSameDevice(Device device, Guid instanceGuid)
		{
			return instanceGuid.Equals(device == null ? Guid.Empty : device.DeviceInformation.InstanceGuid);
		}

	}
}
