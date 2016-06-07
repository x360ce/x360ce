using System;
namespace JocysCom.ClassLibrary.Processes
{
	[Flags]
	public enum MouseKey: ushort
	{
		/// <summary>The CTRL key is down.</summary>
		MK_CONTROL = 0x0008,
		/// <summary>The left mouse button is down.</summary>
		MK_LBUTTON = 0x0001,
		/// <summary>The middle mouse button is down.</summary>
		MK_MBUTTON = 0x0010,
		/// <summary>The right mouse button is down.</summary>
		MK_RBUTTON = 0x0002,
		/// <summary>The SHIFT key is down.</summary>
		MK_SHIFT = 0x0004,
		/// <summary>The first X button is down.</summary>
		MK_XBUTTON1 = 0x0020,
		/// <summary>The second X button is down.</summary>
		MK_XBUTTON2 = 0x0040,
	}
}
