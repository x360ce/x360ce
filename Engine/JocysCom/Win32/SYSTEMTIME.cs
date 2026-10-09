using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public struct SYSTEMTIME
	{
		public short wYear;
		public short wMonth;
		public short wDayOfWeek;
		public short wDay;
		public short wHour;
		public short wMinute;
		public short wSecond;
		public short wMilliseconds;
	}
}
