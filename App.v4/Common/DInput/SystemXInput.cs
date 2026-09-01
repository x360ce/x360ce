using System;
using System.Runtime.InteropServices;

namespace x360ce.App.DInput
{
	/// <summary>
	/// Windows' own XInput, asked about the places it has given out.
	/// </summary>
	/// <remarks>
	/// Deliberately separate from the XInput wrapper in this solution. That one exists to load
	/// whichever library the emulator wants a game to see, including this program's own - which is
	/// the opposite of what is wanted here. The question is which places Windows has actually handed
	/// out, and only the library Windows ships can answer it. Asking through the wrapper answers
	/// about the emulation instead, and would agree with itself no matter what was true.
	///
	/// The same distinction matters for sending vibration back to a real controller: it has to reach
	/// the real device, not the one being emulated.
	/// </remarks>
	public static class SystemXInput
	{
		[DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
		static extern int GetState14(int index, out RawState state);

		[DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
		static extern int GetState910(int index, out RawState state);

		[DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
		static extern int SetState14(int index, ref RawVibration vibration);

		[DllImport("xinput9_1_0.dll", EntryPoint = "XInputSetState")]
		static extern int SetState910(int index, ref RawVibration vibration);

		[StructLayout(LayoutKind.Sequential)]
		struct RawState
		{
			public uint PacketNumber;
			public ushort Buttons;
			public byte LeftTrigger;
			public byte RightTrigger;
			public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct RawVibration
		{
			public ushort LeftMotorSpeed;
			public ushort RightMotorSpeed;
		}

		/// <summary>Whether Windows reports a controller in this place. Places count from zero.</summary>
		public static bool IsConnected(int place)
		{
			if (place < 0 || place > 3)
				return false;
			RawState state;
			// Windows 8 and later ship 1_4. The older name is kept for machines that do not, and a
			// machine with neither has no XInput at all, which is answered as no controller.
			try { return GetState14(place, out state) == 0; }
			catch (DllNotFoundException) { }
			catch (EntryPointNotFoundException) { }
			try { return GetState910(place, out state) == 0; }
			catch (DllNotFoundException) { }
			catch (EntryPointNotFoundException) { }
			return false;
		}

		/// <summary>Sends vibration to a real controller in this place. Places count from zero.</summary>
		/// <remarks>
		/// An Xbox controller offers its motors through XInput and nowhere else - its DirectInput face
		/// declares no force feedback at all - so this is the only way to pass a game's rumble back to
		/// the device somebody is holding.
		/// </remarks>
		public static bool SetVibration(int place, ushort leftMotor, ushort rightMotor)
		{
			if (place < 0 || place > 3)
				return false;
			var vibration = new RawVibration { LeftMotorSpeed = leftMotor, RightMotorSpeed = rightMotor };
			try { return SetState14(place, ref vibration) == 0; }
			catch (DllNotFoundException) { }
			catch (EntryPointNotFoundException) { }
			try { return SetState910(place, ref vibration) == 0; }
			catch (DllNotFoundException) { }
			catch (EntryPointNotFoundException) { }
			return false;
		}
	}
}
