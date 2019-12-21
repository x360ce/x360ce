using System.Drawing;

namespace JocysCom.ClassLibrary.Drawing
{
	partial class Effects
	{

		/// <summary>
		/// Make bitmap transparent.
		/// </summary>
		/// <param name="b"></param>
		/// <param name="alpha">256 max</param>
		/// <returns></returns>
		public void Transparent(Bitmap b, int alpha)
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
					if (a >= byte.MaxValue) a = byte.MaxValue;
					if (a <= byte.MinValue) a = byte.MinValue;
					b.SetPixel(x, y, Color.FromArgb(a, p.R, p.G, p.B));
				}
			}
		}

	}
}
