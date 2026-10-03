namespace JocysCom.ClassLibrary.Win32
{
	public enum TOKEN_ELEVATION_TYPE
	{
		/// <summary>
		/// UAC is disabled or the process is started by a standard user (not a member of the Administrators group).
		/// </summary>
		TokenElevationTypeDefault = 1,
		/// <summary>
		/// UAC is enabled and user is elevated.
		/// </summary>
		TokenElevationTypeFull = 2,
		/// <summary>
		/// UAC is enabled and user is not elevated.
		/// </summary>
		TokenElevationTypeLimited = 3
	}
}
