using System.Runtime.InteropServices;
namespace JocysCom.ClassLibrary.Processes
{
	/// <summary>
	/// Split integer bytes into smaller numbers.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public class TestUnion
	{
		public TestUnion(int value) { Number = value; }
		public TestUnion(uint value) { UNumber = value; }

		[FieldOffset(0)]
		public int Number;
		[FieldOffset(0)]
		public uint UNumber;
		[FieldOffset(0)]
		public short Low;
		[FieldOffset(0)]
		public ushort ULow;
		[FieldOffset(2)]
		public short High;
		[FieldOffset(2)]
		public ushort UHigh;
	}
}
