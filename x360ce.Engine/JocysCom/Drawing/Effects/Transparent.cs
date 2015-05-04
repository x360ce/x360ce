using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

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

	}
}
