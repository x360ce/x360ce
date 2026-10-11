using System.Collections.Generic;

namespace x360ce.Engine
{
	/// <summary>
	/// Where a device whose HID description names none of its controls carries them in its report: the description it
	/// should have given. Its axes are X, Y, Z, X Rotation, Y Rotation and Z Rotation in that order, then a run of
	/// buttons one bit each, so it is read like any device that describes itself.
	/// </summary>
	public sealed class RawInputDescription
	{
		/// <summary>
		/// The devices whose HID description names none of their controls, with where their reports carry them. One row
		/// a device: a device that is not here and does not describe itself is not read.
		/// </summary>
		public static readonly RawInputDescription[] Known =
		{
			// Logitech G13: the stick, then 40 key bits, as the Linux G13 driver reads them.
			new RawInputDescription { VendorId = 0x046D, ProductId = 0xC21C, UsagePage = 0xFF00, ReportId = 1, AxisBits = new[] { 8, 16 }, AxisBitSize = 8, ButtonBit = 24, ButtonCount = 40 },
		};

		/// <summary>The row of <see cref="Known"/> for a device with that identity and top-level collection page, or null when it has none.</summary>
		public static RawInputDescription Find(int vendorId, int productId, int usagePage)
		{
			foreach (var d in Known)
				if (d.VendorId == vendorId && d.ProductId == productId && d.UsagePage == usagePage)
					return d;
			return null;
		}

		public int VendorId;
		public int ProductId;
		/// <summary>The usage page of the top-level collection the report comes from, which Raw Input is registered for whole.</summary>
		public int UsagePage;
		public int ReportId;
		/// <summary>The bit each axis starts at, X first; the report ID is bits 0 to 7.</summary>
		public int[] AxisBits;
		/// <summary>The size of each axis in bits, an unsigned value from 0.</summary>
		public int AxisBitSize;
		/// <summary>The bit the first button is at; the others follow it.</summary>
		public int ButtonBit;
		public int ButtonCount;

		/// <summary>The controls as the HID parser would have listed them, each with its place in the report.</summary>
		public RawInputControl[] Controls()
		{
			var controls = new List<RawInputControl>();
			for (var i = 0; i < AxisBits.Length; i++)
				controls.Add(new RawInputControl
				{
					DataIndex = controls.Count,
					UsagePage = 0x01,
					Usage = 0x30 + i,
					ReportId = ReportId,
					ReportCount = 1,
					BitSize = AxisBitSize,
					LogicalMax = (1 << AxisBitSize) - 1,
					BitOffset = AxisBits[i],
				});
			for (var i = 0; i < ButtonCount; i++)
				controls.Add(new RawInputControl
				{
					DataIndex = controls.Count,
					IsButton = true,
					UsagePage = 0x09,
					Usage = i + 1,
					ReportId = ReportId,
					ReportCount = 1,
					BitOffset = ButtonBit + i,
				});
			return controls.ToArray();
		}
	}
}
