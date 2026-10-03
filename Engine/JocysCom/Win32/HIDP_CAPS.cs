using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	[StructLayout(LayoutKind.Sequential)]
	public struct HIDP_CAPS
	{
		public short Usage;
		public short UsagePage;
		public short InputReportByteLength;
		public short OutputReportByteLength;
		public short FeatureReportByteLength;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
		public short[] Reserved;
		public short NumberLinkCollectionNodes;
		public short NumberInputButtonCaps;
		public short NumberInputValueCaps;
		public short NumberInputDataIndices;
		public short NumberOutputButtonCaps;
		public short NumberOutputValueCaps;
		public short NumberOutputDataIndices;
		public short NumberFeatureButtonCaps;
		public short NumberFeatureValueCaps;
		public short NumberFeatureDataIndices;

	}
}
