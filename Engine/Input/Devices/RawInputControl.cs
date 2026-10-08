namespace x360ce.Engine
{
	/// <summary>One control of a HID device as the HID parser describes it: one data index.</summary>
	public class RawInputControl
	{
		/// <summary>The index HidP_GetData reports the control by.</summary>
		public int DataIndex { get; set; }

		/// <summary>True for a button, which is on or off; false for a value.</summary>
		public bool IsButton { get; set; }

		public int UsagePage { get; set; }

		public int Usage { get; set; }

		/// <summary>The link collection that holds the control; 0 is the top-level collection.</summary>
		public int LinkCollection { get; set; }

		/// <summary>The ID of the report that carries the control; 0 when the device numbers no reports.</summary>
		public int ReportId { get; set; }

		/// <summary>The size of the value in bits; 0 for a button.</summary>
		public int BitSize { get; set; }

		/// <summary>The fields the control has in its report. More than one is a usage value array, which HidP_GetData does not return.</summary>
		public int ReportCount { get; set; }

		/// <summary>The smallest value the device declares; 0 for a button.</summary>
		public int LogicalMin { get; set; }

		/// <summary>The largest value the device declares; 0 for a button. Less than <see cref="LogicalMin"/> when the device declares an unsigned range as signed numbers.</summary>
		public int LogicalMax { get; set; }

		/// <summary>The bit the control starts at in its report, the report ID's eight bits first; -1 for a control the HID parser reads.</summary>
		public int BitOffset { get; set; } = -1;
	}
}
