using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace JocysCom.ClassLibrary.Drawing
{
	public partial class Effects
	{
		private Random random;

		public Effects()
		{
			random = new Random();
		}

		public byte[] ColorToBytes(Color c)
		{
			byte[] bytes = new byte[4];
			bytes[0] = c.A;
			bytes[1] = c.R;
			bytes[2] = c.G;
			bytes[3] = c.B;
			return bytes;
		}

		public Color BytesToColor(byte[] bytes)
		{
			return Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
		}

	}
}
