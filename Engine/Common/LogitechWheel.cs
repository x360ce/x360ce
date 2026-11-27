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

		/// <summary>Whether the wheel has the 5-pair RGB/RPM shift LED strip addressed by the Logitech HID 0xF8 0x12 report.</summary>
		public static bool SupportsLeds(int vendorId, int productId)
		{
			if (vendorId != VendorId)
				return false;
			switch (productId)
			{
				case 0xC29B: // G27
				case 0xC24F: // G29
				case 0xC266: // G923 (PlayStation)
					return true;
				default:
					return false;
			}
		}

		/// <summary>The report that sets the 5-pair RPM/RGB LED strip (bits 0..4 from outer green to inner red), with the report identifier in front.</summary>
		/// <param name="mask">Bitmask 0x00 (all off) to 0x1F (all 5 pairs lit).</param>
		/// <param name="reportLength">The output report length the interface declares, report identifier included.</param>
		public static byte[] LedReport(byte mask, int reportLength)
		{
			return Report(LedCommand(mask), reportLength);
		}

		/// <summary>Converts a normalized engine RPM fraction (0..1) into the 5-bit symmetric shift-light bitmask (flashes at redline &gt;= 97 %).</summary>
		public static byte RpmToLedMask(float rpmFraction, long nowMs)
		{
			if (float.IsNaN(rpmFraction) || float.IsInfinity(rpmFraction) || rpmFraction < 0.50f)
				return 0x00;
			if (rpmFraction < 0.62f) return 0x01;
			if (rpmFraction < 0.74f) return 0x03;
			if (rpmFraction < 0.84f) return 0x07;
			if (rpmFraction < 0.92f) return 0x0F;
			if (rpmFraction < 0.97f) return 0x1F;
			// Redline shift flash (~6 Hz).
			return ((nowMs / 85) & 1) == 0 ? (byte)0x1F : (byte)0x00;
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

		static byte[] LedCommand(byte mask)
		{
			return new byte[] { 0xF8, 0x12, (byte)(mask & 0x1F) };
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

		/// <summary>Sets the wheel's 5-pair RGB/RPM LED strip on its HID interface (bits 0..4).</summary>
		/// <returns>False when the interface could not be opened or would not take the report.</returns>
		public static bool SetLeds(string hidDevicePath, byte mask)
		{
			return Send(hidDevicePath, LedCommand(mask));
		}

		/// <summary>Why the last report sent on this thread failed: the Windows error or HRESULT, -1 for no interface path, or 0 when it was taken.</summary>
		/// <remarks>Per thread, so the engine and a thread-pool send of the LEDs each read their own.</remarks>
		[ThreadStatic]
		public static int LastError;

		static bool Send(string hidDevicePath, byte[] command)
		{
			if (string.IsNullOrEmpty(hidDevicePath))
			{
				LastError = -1;
				return false;
			}
			// Generic read and write, which is what the HID class driver expects a client to ask for.
			var genericReadWrite = unchecked((FileAccess)0xC0000000);
			try
			{
				using (var handle = NativeMethods.CreateFile(hidDevicePath, genericReadWrite,
					FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
				{
					if (handle.IsInvalid)
					{
						LastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
						return false;
					}
					var report = Report(command, OutputReportLength(handle));
					using (var stream = new FileStream(handle, FileAccess.Write, report.Length, false))
					{
						stream.Write(report, 0, report.Length);
						stream.Flush();
					}
					LastError = 0;
					return true;
				}
			}
			catch (IOException ex)
			{
				LastError = ex.HResult;
				return false;
			}
			catch (UnauthorizedAccessException ex)
			{
				LastError = ex.HResult;
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
