using System;

namespace JocysCom.ClassLibrary.IO
{
	/// <summary>
	/// Provides data for device change events raised by <see cref="DeviceDetector"/>.
	/// </summary>
	/// <remarks>
	/// Encapsulates the broadcast change type (<see cref="Win32.DBT"/>), optional device type (<see cref="Win32.DBCH_DEVICETYPE"/>), and raw device info.
	/// Set <see cref="Cancel"/> to true in QueryRemove events to cancel device removal.
	/// </remarks>
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

		/// <summary>When set to true during a QueryRemove event, cancels the pending device removal.</summary>
		public bool Cancel;

	}
}
