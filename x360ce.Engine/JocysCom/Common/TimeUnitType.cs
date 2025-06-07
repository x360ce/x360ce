using System.ComponentModel;

namespace JocysCom.ClassLibrary
{
	/// <summary>Specifies time units for interval and date-time sequence calculations, as used by TimeUnitHelper.</summary>
	public enum TimeUnitType
	{
		/// <summary>None</summary>
		[Description("")]
		None,
		/// <summary>1 millisecond</summary>
		[Description("Milliseconds")]
		Millisecond,
		/// <summary>1 second</summary>
		[Description("Seconds")]
		Second,
		/// <summary>1 minute</summary>
		[Description("Minutes")]
		Minute,
		/// <summary>1 hour</summary>
		[Description("Hours")]
		Hour,
		/// <summary>1 day</summary>
		[Description("Days")]
		Day,
		/// <summary>1 week</summary>
		[Description("Weeks")]
		Week,
		/// <summary>1 month</summary>
		[Description("Months")]
		Month,
		/// <summary>1 year</summary>
		[Description("Years")]
		Year,
		/// <summary>2 weeks</summary>
		[Description("Fortnights")]
		Fortnight,
		/// <summary>3 months</summary>
		[Description("Quarters")]
		Quarter,
		/// <summary>6 months</summary>
		[Description("Semesters")]
		Semester,
		/// <summary>2 years</summary>
		[Description("Biennials")]
		Biennial,
		/// <summary>3 years</summary>
		[Description("Triennials")]
		Triennial,
		/// <summary>10 years</summary>
		[Description("Decades")]
		Decade,
		/// <summary>100 years</summary>
		[Description("Centuries")]
		Century,
	}
}
