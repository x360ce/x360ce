using System.Runtime.InteropServices;
namespace JocysCom.ClassLibrary.Processes
{
	[StructLayout(LayoutKind.Explicit)]
	public struct TestUnion
	{
		[FieldOffset(0)]
		public uint UNumber;
		[FieldOffset(0)]
		public ushort ULow;
		[FieldOffset(2)]
		public ushort UHigh;
		[FieldOffset(0)]
		public int Number;
		[FieldOffset(0)]
		public short Low;
		[FieldOffset(2)]
		public short High;
	}
}
