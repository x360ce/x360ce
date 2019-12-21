using System.Drawing;

namespace JocysCom.ClassLibrary.Drawing
{
	public partial class Effects
	{
		/// <summary>
		/// Make bitmap grayscale
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public Image GrayScale(Bitmap b)
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
			return b;
		}
	}
}
