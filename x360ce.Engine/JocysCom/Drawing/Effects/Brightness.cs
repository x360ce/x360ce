using System.Drawing;

namespace JocysCom.ClassLibrary.Drawing
{
	public partial class Effects
	{
		public Image Brightness(Bitmap b, int level)
		{
			if (level < -255 || level > 255) return null;
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
						if (nVal < 0) nVal = 0;
						if (nVal > 255) nVal = 255;
						p[i] = (byte)nVal;
					}
					b.SetPixel(x, y, BytesToColor(p));
				}
			}
			return b;
		}


	}
}
