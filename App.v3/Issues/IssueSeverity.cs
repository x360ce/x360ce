namespace x360ce.App.Issues
{
	public enum IssueSeverity
	{
		None = 0,
		// Information.
		Low = 1,
		// Can be issues.
		Important = 2,
		// Game won't run.
		Moderate = 3,
		// Close application.
		Critical = 4,
	}
}
