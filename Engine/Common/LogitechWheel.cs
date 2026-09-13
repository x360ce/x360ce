using JocysCom.ClassLibrary.Win32;
using System;
using System.IO;

namespace x360ce.Engine
{
	/// <summary>Sets the steering range of a Logitech wheel, which powers up at 200 degrees.</summary>
	/// <remarks>
	/// The G25, G27, Driving Force GT and G29 turn 200 degrees lock to lock until something tells
	/// them otherwise. On Windows that something is Logitech's own software, and a machine without
	/// it keeps a 900 degree wheel at 200. The wheel takes the range as a seven byte report on its
	/// HID interface, the same one the Linux driver sends, and takes it while DirectInput holds the
	/// device, because the HID interface is not what DirectInput holds.
	/// </remarks>
	public static class LogitechWheel
	{
		public const int VendorId = 0x046D;

		/// <summary>The narrowest and widest range these wheels take.</summary>
		public const int MinRange = 40;
		public const int MaxRange = 900;

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

		/// <summary>The report that sets the range, with the report identifier Windows wants in front.</summary>
		/// <param name="degrees">Lock to lock. Held within what the wheel takes.</param>
		/// <param name="reportLength">The output report length the interface declares, report identifier included.</param>
		public static byte[] RangeReport(int degrees, int reportLength)
		{
			var range = Math.Max(MinRange, Math.Min(MaxRange, degrees));
			var report = new byte[Math.Max(reportLength, 8)];
			report[1] = 0xF8;
			report[2] = 0x81;
			report[3] = (byte)(range & 0xFF);
			report[4] = (byte)((range >> 8) & 0xFF);
			return report;
		}

		/// <summary>Sends the range to the wheel on its HID interface.</summary>
		/// <returns>False when the interface could not be opened or would not take the report.</returns>
		public static bool SetRange(string hidDevicePath, int degrees)
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
					var length = OutputReportLength(handle);
					var report = RangeReport(degrees, length);
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
				NativeMethods.HidD_FreePreparsedData(ref preparsed);
			}
		}
	}
}
