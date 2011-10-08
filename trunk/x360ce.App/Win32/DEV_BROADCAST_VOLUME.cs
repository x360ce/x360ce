using System.Runtime.InteropServices;

namespace x360ce.App.Win32
{
	/// <summary>
	/// Contains information about a logical volume.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DEV_BROADCAST_VOLUME
	{
		public int dbcv_size;
		public int dbcv_devicetype;
		public int dbcv_reserved;
		public int dbcv_unitmask;
	}

}
