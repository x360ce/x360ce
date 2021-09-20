using System.Drawing;

namespace JocysCom.ClassLibrary.Drawing
{
	public partial class Effects
	{

		public static byte[] ColorToBytes(Color c)
		{
			byte[] bytes = new byte[4];
			bytes[0] = c.A;
			bytes[1] = c.R;
			bytes[2] = c.G;
			bytes[3] = c.B;
			return bytes;
		}

		public static Color BytesToColor(byte[] bytes)
		{
			return Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
		}

		/// <summary>
		/// Make bitmap gray-scale
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Image GrayScale(Bitmap b)
		{
			int w = b.Width;
			int h = b.Height;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					Color p = b.GetPixel(x, y);
					// National Television System Committee (NTSC) conversion formula.
					byte c = (byte)(.299 * p.R + .587 * p.G + .114 * p.B);
					b.SetPixel(x, y, Color.FromArgb(p.A, c, c, c));
				}
			}
			return b;
		}

		public static Image Brightness(Bitmap b, int level)
		{
			if (level < -255 || level > 255)
				return null;
			int w = b.Width;
			int h = b.Height;
			int nVal = 0;
			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					byte[] p = ColorToBytes(b.GetPixel(x, y));
					for (int i = 1; i < 4; i++)
					{
						nVal = p[i] + level;
						if (nVal < 0)
							nVal = 0;
						if (nVal > 255)
							nVal = 255;
						p[i] = (byte)nVal;
					}
					b.SetPixel(x, y, BytesToColor(p));
				}
			}
			return b;
		}

		/// <summary>
		/// Make bitmap transparent.
		/// </summary>
		/// <param name="b"></param>
		/// <param name="alpha">256 max</param>
		/// <returns></returns>
		public static void Transparent(Bitmap b, int alpha)
		{
			var w = b.Width;
			var h = b.Height;
			int a;
			Color p;
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					p = b.GetPixel(x, y);
					a = (int)(p.A * (float)alpha / byte.MaxValue);
					if (a >= byte.MaxValue)
						a = byte.MaxValue;
					if (a <= byte.MinValue)
						a = byte.MinValue;
					b.SetPixel(x, y, Color.FromArgb(a, p.R, p.G, p.B));
				}
			}
		}

	}
}
