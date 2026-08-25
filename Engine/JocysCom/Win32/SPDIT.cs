namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Driver Type
	/// </summary>
	public enum SPDIT : uint
	{
		SPDIT_NODRIVER,
		/// <summary>
		/// Enumerate a class driver list. Used when DeviceInfoData is not specified.
		/// </summary>
		SPDIT_CLASSDRIVER,
		/// <summary>
		/// Enumerate a list of compatible drivers for the specified device. Used when DeviceInfoData is specified.
		/// </summary>
		SPDIT_COMPATDRIVER

	}
}
