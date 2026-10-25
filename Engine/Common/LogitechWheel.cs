using JocysCom.ClassLibrary.Win32;
using System;
using System.IO;

namespace x360ce.Engine
{
	/// <summary>Sets the steering range of a Logitech wheel, which powers up at 200 degrees, and switches its own centering.</summary>
	/// <remarks>
	/// The G25, G27, Driving Force GT and G29 turn 200 degrees lock to lock until something tells
	/// them otherwise. On Windows that something is Logitech's own software, and a machine without
	/// it keeps a 900 degree wheel at 200. The wheel takes the range as a seven byte report on its
	/// HID interface, the same one the Linux driver sends, and takes it while DirectInput holds the
	/// device, because the HID interface is not what DirectInput holds.
	///
	/// The wheel switches its own centering off when an effect is made on it, and DirectInput's own
	/// setting does not switch it back while the device is held. The report that does is taken while
	/// effects play, so the wheel centres itself under a game's rumble.
	/// </remarks>
	public static class LogitechWheel
	{
		public const int VendorId = 0x046D;

		/// <summary>The narrowest and widest range these wheels take.</summary>
		public const int MinRange = 40;
		public const int MaxRange = 900;

		/// <summary>The range these wheels turn until something sends them another.</summary>
		public const int PowerUpRange = 200;

		/// <summary>Whether the wheel takes the range report, by the identifiers Windows reports for it.</summary>
		public static bool SupportsRange(int vendorId, int productId)
		{
			if (vendorId != VendorId)
				return false;
			switch (productId)
			{
				case 0xC299: // G25
				case 0xC29A: // Driving Force GT
				case 0xC29B: // G27
				case 0xC24F: // G29
					return true;
				default:
					return false;
			}
		}

		/// <summary>Whether the wheel takes the reports that switch its own centering, by the identifiers Windows reports for it.</summary>
		/// <remarks>Every wheel the Linux driver switches with these reports. The G920 and G923 speak another protocol.</remarks>
		public static bool SupportsCentering(int vendorId, int productId)
		{
			if (vendorId != VendorId)
				return false;
			switch (productId)
			{
				case 0xC293: // WingMan Formula Force GP
				case 0xC294: // Driving Force, and other wheels in their compatibility mode
				case 0xC295: // MOMO
				case 0xC298: // Driving Force Pro
				case 0xC299: // G25
				case 0xC29A: // Driving Force GT
				case 0xC29B: // G27
				case 0xC29C: // Speed Force Wireless
				case 0xC24F: // G29
				case 0xCA03: // MOMO 2
					return true;
				default:
					return false;
			}
		}

		/// <summary>The report that sets the range, with the report identifier Windows wants in front.</summary>
		/// <param name="degrees">Lock to lock. Held within what the wheel takes.</param>
		/// <param name="reportLength">The output report length the interface declares, report identifier included.</param>
		public static byte[] RangeReport(int degrees, int reportLength)
		{
			return Report(RangeCommand(degrees), reportLength);
		}

		/// <summary>The report that switches the wheel's own centering on or off, with the report identifier in front.</summary>
		/// <param name="reportLength">The output report length the interface declares, report identifier included.</param>
		public static byte[] CenteringReport(bool on, int reportLength)
		{
			return Report(CenteringCommand(on), reportLength);
		}

		static byte[] RangeCommand(int degrees)
		{
			var range = Math.Max(MinRange, Math.Min(MaxRange, degrees));
			return new byte[] { 0xF8, 0x81, (byte)(range & 0xFF), (byte)((range >> 8) & 0xFF) };
		}

		static byte[] CenteringCommand(bool on)
		{
			return new byte[] { on ? (byte)0x14 : (byte)0xF5 };
		}

		static byte[] Report(byte[] command, int reportLength)
		{
			var report = new byte[Math.Max(reportLength, 8)];
			Array.Copy(command, 0, report, 1, command.Length);
			return report;
		}

		/// <summary>Sends the range to the wheel on its HID interface.</summary>
		/// <returns>False when the interface could not be opened or would not take the report.</returns>
		public static bool SetRange(string hidDevicePath, int degrees)
		{
			return Send(hidDevicePath, RangeCommand(degrees));
		}

		/// <summary>Switches the wheel's own centering on or off on its HID interface.</summary>
		/// <returns>False when the interface could not be opened or would not take the report.</returns>
		public static bool SetCentering(string hidDevicePath, bool on)
		{
			return Send(hidDevicePath, CenteringCommand(on));
		}

		static bool Send(string hidDevicePath, byte[] command)
		{
			if (string.IsNullOrEmpty(hidDevicePath))
				return false;
			// Generic read and write, which is what the HID class driver expects a client to ask for.
			var genericReadWrite = unchecked((FileAccess)0xC0000000);
			try
			{
				using (var handle = NativeMethods.CreateFile(hidDevicePath, genericReadWrite,
					FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
				{
					if (handle.IsInvalid)
						return false;
					var report = Report(command, OutputReportLength(handle));
					using (var stream = new FileStream(handle, FileAccess.Write, report.Length, false))
					{
						stream.Write(report, 0, report.Length);
						stream.Flush();
					}
					return true;
				}
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}

		/// <summary>The output report length the interface declares, or nought when it will not say.</summary>
		static int OutputReportLength(Microsoft.Win32.SafeHandles.SafeFileHandle handle)
		{
			var preparsed = IntPtr.Zero;
			if (!NativeMethods.HidD_GetPreparsedData(handle, ref preparsed))
				return 0;
			try
			{
				var caps = new HIDP_CAPS();
				NativeMethods.HidP_GetCaps(preparsed, ref caps);
				return caps.OutputReportByteLength;
			}
			finally
			{
				NativeMethods.HidD_FreePreparsedData(preparsed);
			}
		}
	}
}
