namespace JocysCom.ClassLibrary.Win32
{
	public enum DBCH_DEVICETYPE : int
	{
		/// <summary>Class of devices. This structure is a DEV_BROADCAST_DEVICEINTERFACE structure.</summary>
		DBT_DEVTYP_DEVICEINTERFACE = 0x00000005,
		/// <summary>File system handle. This structure is a DEV_BROADCAST_HANDLE structure.</summary>
		DBT_DEVTYP_HANDLE = 0x00000006,
		/// <summary>OEM- or IHV-defined device type. This structure is a DEV_BROADCAST_OEM structure.</summary>
		DBT_DEVTYP_OEM = 0x00000000,
		/// <summary>Port device (serial or parallel). This structure is a DEV_BROADCAST_PORT structure.</summary>
		DBT_DEVTYP_PORT = 0x00000003,
		/// <summary>Logical volume. This structure is a DEV_BROADCAST_VOLUME structure.</summary>
		DBT_DEVTYP_VOLUME = 0x00000002,
	}
}
