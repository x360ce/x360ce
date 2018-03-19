namespace JocysCom.ClassLibrary.Controls.IssueControl
{
	public enum IssueSeverity
	{
		None = 0,
		// Information.
		Low = 1,
        // Some features disabled.
		Important = 2,
		// Main features won't run.
		Moderate = 3,
		// Close application.
		Critical = 4,
	}
}
