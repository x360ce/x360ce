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

	}
}
