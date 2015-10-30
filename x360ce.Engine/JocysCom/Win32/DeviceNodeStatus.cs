using System;

namespace JocysCom.ClassLibrary.Win32
{

	/// <summary>
	/// Device Instance status flags, returned by call to CM_Get_DevInst_Status
	/// DN_-prefixed bit flags defined in Cfg.h.
	/// c:\Program Files\Microsoft SDKs\Windows\v7.1\Include\cfg.h
	/// </summary>
	[Flags]
	public enum DeviceNodeStatus: uint
	{
		/// <summary>Was enumerated by ROOT <summary>
		DN_ROOT_ENUMERATED = 0x00000001,
		/// <summary>Has Register_Device_Driver </summary>
		DN_DRIVER_LOADED = 0x00000002,
		/// <summary>Has Register_Enumerator </summary>
		DN_ENUM_LOADED = 0x00000004,
		/// <summary>Is currently configured </summary>
		DN_STARTED = 0x00000008,
		/// <summary>Manually installed </summary>
		DN_MANUAL = 0x00000010,
		/// <summary>May need reenumeration </summary>
		DN_NEED_TO_ENUM = 0x00000020,
		/// <summary>Has received a config </summary>
		DN_NOT_FIRST_TIME = 0x00000040,
		/// <summary>Enum generates hardware ID </summary>
		DN_HARDWARE_ENUM = 0x00000080,
		/// <summary>Lied about can reconfig once </summary>
		DN_LIAR = 0x00000100,
		/// <summary>Not CM_Create_DevInst lately </summary>
		DN_HAS_MARK = 0x00000200,
		/// <summary>Need device installer </summary>
		DN_HAS_PROBLEM = 0x00000400,
		/// <summary>Is filtered </summary>
		DN_FILTERED = 0x00000800,
		/// <summary>Has been moved </summary>
		DN_MOVED = 0x00001000,
		/// <summary>Can be disabled </summary>
		DN_DISABLEABLE = 0x00002000,
		/// <summary>Can be removed </summary>
		DN_REMOVABLE = 0x00004000,
		/// <summary>Has a private problem </summary>
		DN_PRIVATE_PROBLEM = 0x00008000,
		/// <summary>Multi function parent </summary>
		DN_MF_PARENT = 0x00010000,
		/// <summary>Multi function child </summary>
		DN_MF_CHILD = 0x00020000,
		/// <summary>DevInst is being removed </summary>
		DN_WILL_BE_REMOVED = 0x00040000,
		/// <summary>S: Has received a config enumerate </summary>
		DN_NOT_FIRST_TIMEE = 0x00080000,
		/// <summary>S: When child is stopped, free resources </summary>
		DN_STOP_FREE_RES = 0x00100000,
		/// <summary>S: Don't skip during rebalance </summary>
		DN_REBAL_CANDIDATE = 0x00200000,
		/// <summary>S: This devnode's log_confs do not have same resources </summary>
		DN_BAD_PARTIAL = 0x00400000,
		/// <summary>S: This devnode's is an NT enumerator </summary>
		DN_NT_ENUMERATOR = 0x00800000,
		/// <summary>S: This devnode's is an NT driver </summary>
		DN_NT_DRIVER = 0x01000000,
		/// <summary>S: Devnode need lock resume processing </summary>
		DN_NEEDS_LOCKING = 0x02000000,
		/// <summary>S: Devnode can be the wakeup device </summary>
		DN_ARM_WAKEUP = 0x04000000,
		/// <summary>S: APM aware enumerator </summary>
		DN_APM_ENUMERATOR = 0x08000000,
		/// <summary>S: APM aware driver </summary>
		DN_APM_DRIVER = 0x10000000,
		/// <summary>S: Silent install </summary>
		DN_SILENT_INSTALL = 0x20000000,
		/// <summary>S: No show in device manager </summary>
		DN_NO_SHOW_IN_DM = 0x40000000,
		/// <summary>S: Had a problem during preassignment of boot log conf </summary>
		DN_BOOT_LOG_PROB = 0x80000000,
	}
}
