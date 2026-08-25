using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{

	[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
	public struct SP_DRVINFO_DATA
	{
		public int cbSize;
		public uint DriverType;
		public readonly UIntPtr Reserved;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string Description;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string MfgName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string ProviderName;
		public System.Runtime.InteropServices.ComTypes.FILETIME DriverDate;
		public ulong DriverVersion;
		public void Initialize()
		{
			cbSize = Marshal.SizeOf(typeof(SP_DRVINFO_DATA));
		}

		public Version GetVersion()
		{
			int revision = (int)(DriverVersion & 0xffff);
			int build = (int)((DriverVersion >> 16) & 0xffff);
			int minor = (int)((DriverVersion >> 32) & 0xffff);
			int major = (int)(DriverVersion >> 48);
			return new Version(major, minor, build, revision);
		}

		public DateTime GetDate()
		{
			unchecked
			{
				var h = unchecked((long)DriverDate.dwHighDateTime << 32);
				var l = unchecked(DriverDate.dwLowDateTime & 0xFFFFFFFF);
				var dt = DateTime.FromFileTimeUtc(h | l);
				return dt;
			}
		}
	}
}
