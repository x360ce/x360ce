using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Contains information about a logical volume.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DEV_BROADCAST_VOLUME
	{
		public int dbcv_size;
		public DBCH_DEVICETYPE dbcv_devicetype;
		public int dbcv_reserved;
		public int dbcv_unitmask;
		/// <summary>
		/// Get driver letter.
		/// </summary>
		public char DriveLetter
		{
			get
			{
				int count = 0; while (dbcv_unitmask > 1) { dbcv_unitmask >>= 1; count++; }
				return "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[count];
			}
		}
	}

}
