using System;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Device registry property codes
	/// </summary>
	public enum SPDRP : uint
	{
		/// <summary>DeviceDesc (R/W)</summary>
		SPDRP_DEVICEDESC = 0x0,
		/// <summary>HardwareID (R/W)</summary>
		SPDRP_HARDWAREID = 0x1,
		/// <summary>CompatibleIDs (R/W)</summary>
		SPDRP_COMPATIBLEIDS = 0x2,
		/// <summary>unused</summary>
		SPDRP_UNUSED0 = 0x3,
		/// <summary>Service (R/W)</summary>
		SPDRP_SERVICE = 0x4,
		/// <summary>unused</summary>
		SPDRP_UNUSED1 = 0x5,
		/// <summary>unused</summary>
		SPDRP_UNUSED2 = 0x6,
		/// <summary>Class (R--tied to ClassGUID)</summary>
		SPDRP_CLASS = 0x7,
		/// <summary>ClassGUID (R/W)</summary>
		SPDRP_CLASSGUID = 0x8,
		/// <summary>Driver (R/W)</summary>
		SPDRP_DRIVER = 0x9,
		/// <summary>ConfigFlags (R/W)</summary>
		SPDRP_CONFIGFLAGS = 0xA,
		/// <summary>Mfg (R/W)</summary>
		SPDRP_MFG = 0xB,
		/// <summary>FriendlyName (R/W)</summary>
		SPDRP_FRIENDLYNAME = 0xC,
		/// <summary>LocationInformation (R/W)</summary>
		SPDRP_LOCATION_INFORMATION = 0xD,
		/// <summary>PhysicalDeviceObjectName (R)</summary>
		SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0xE,
		/// <summary>Capabilities (R)</summary>
		SPDRP_CAPABILITIES = 0xF,
		/// <summary>UiNumber (R)</summary>
		SPDRP_UI_NUMBER = 0x10,
		/// <summary>UpperFilters (R/W)</summary>
		SPDRP_UPPERFILTERS = 0x11,
		/// <summary>LowerFilters (R/W)</summary>
		SPDRP_LOWERFILTERS = 0x12,
		/// <summary>BusTypeGUID (R)</summary>
		SPDRP_BUSTYPEGUID = 0x13,
		/// <summary>LegacyBusType (R)</summary>
		SPDRP_LEGACYBUSTYPE = 0x14,
		/// <summary>BusNumber (R)</summary>
		SPDRP_BUSNUMBER = 0x15,
		/// <summary>Enumerator Name (R)</summary>
		SPDRP_ENUMERATOR_NAME = 0x16,
		/// <summary>Security (R/W, binary form)</summary>
		SPDRP_SECURITY = 0x17,
		/// <summary>Security (W, SDS form)</summary>
		SPDRP_SECURITY_SDS = 0x18,
		/// <summary>Device Type (R/W)</summary>
		SPDRP_DEVTYPE = 0x19,
		/// <summary>Device is exclusive-access (R/W)</summary>
		SPDRP_EXCLUSIVE = 0x1A,
		/// <summary>Device Characteristics (R/W)</summary>
		SPDRP_CHARACTERISTICS = 0x1B,
		/// <summary>Device Address (R)</summary>
		SPDRP_ADDRESS = 0x1C,
		/// <summary>UiNumberDescFormat (R/W)</summary>
		SPDRP_UI_NUMBER_DESC_FORMAT = 0x1D,
		/// <summary>Device Power Data (R)</summary>
		SPDRP_DEVICE_POWER_DATA = 0x1E,
		/// <summary>Removal Policy (R)</summary>
		SPDRP_REMOVAL_POLICY = 0x1F,
		/// <summary>Hardware Removal Policy (R)</summary>
		SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x20,
		/// <summary>Removal Policy Override (RW)</summary>
		SPDRP_REMOVAL_POLICY_OVERRIDE = 0x21,
		/// <summary>Device Install State (R)</summary>
		SPDRP_INSTALL_STATE = 0x22,
		/// <summary>Device Location Paths (R)</summary>
		SPDRP_LOCATION_PATHS = 0x23,
	}

}
