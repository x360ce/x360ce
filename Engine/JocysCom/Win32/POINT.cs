using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{

	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int x;
		public int y;

		public POINT(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public static implicit operator System.Drawing.Point(POINT p)
		{
			return new System.Drawing.Point(p.x, p.y);
		}

		public static implicit operator POINT(System.Drawing.Point p)
		{
			return new POINT(p.X, p.Y);
		}
	}

}
