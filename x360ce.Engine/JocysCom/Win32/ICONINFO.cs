using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{

	[StructLayout(LayoutKind.Sequential)]
	public struct ICONINFO
	{
		/// <summary>
		/// Specifies whether this structure defines an icon or a cursor.
		/// </summary>
		public bool fIcon;
		/// <summary>
		/// Specifies the x-coordinate of a cursor's hot spot.
		/// </summary>
		public Int32 xHotspot;
		/// <summary>
		/// Specifies the y-coordinate of the cursor's hot spot.
		/// </summary>
		public Int32 yHotspot;
		/// <summary>
		/// (HBITMAP) Specifies the icon bitmask bitmap.
		/// </summary>
		public IntPtr hbmMask;
		/// <summary>
		/// (HBITMAP) Handle to the icon color bitmap.
		/// </summary>
		public IntPtr hbmColor;
	}

}
