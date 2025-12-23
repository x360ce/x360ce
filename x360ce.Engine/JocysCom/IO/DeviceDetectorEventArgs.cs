using System;

namespace JocysCom.ClassLibrary.IO
{
	/// <summary>
	/// Our class for passing in custom arguments to our event handlers 
	/// 
	/// </summary>
	public class DeviceDetectorEventArgs : EventArgs
	{
		public DeviceDetectorEventArgs(Win32.DBT changeType, Win32.DBCH_DEVICETYPE? deviceType, object deviceInfo)
		{
			Cancel = false;
			_ChangeType = changeType;
			_DeviceInfo = deviceInfo;
			_DeviceType = deviceType;
		}

		Win32.DBCH_DEVICETYPE? _DeviceType;
		public Win32.DBCH_DEVICETYPE? DeviceType
		{
			get { return _DeviceType; }
		}

		Win32.DBT _ChangeType;
		public Win32.DBT ChangeType
		{
			get { return _ChangeType; }
		}

		object _DeviceInfo;
		public object DeviceInfo
		{
			get { return _DeviceInfo; }
		}

		/// <summary>
		/// Get/Set the value indicating that the event should be cancelled Only in QueryRemove handler.
		/// </summary>
		public bool Cancel;

	}
}
