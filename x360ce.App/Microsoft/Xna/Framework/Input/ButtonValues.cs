namespace Microsoft.Xna.Framework.Input
{
	using System;

	[Flags]
	internal enum ButtonValues : ushort
	{
		A = 0x1000,
		B = 0x2000,
		Back = 0x20,
		BigButton = 0x800,
		Down = 2,
		Left = 4,
		LeftShoulder = 0x100,
		LeftThumb = 0x40,
		Right = 8,
		RightShoulder = 0x200,
		RightThumb = 0x80,
		Start = 0x10,
		Up = 1,
		X = 0x4000,
		Y = 0x8000
	}
}

