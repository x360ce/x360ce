using System;
using System.Collections.Generic;
using System.Text;
using x360ce.App.Win32;

namespace x360ce.App
{
	/// <summary>
	/// Our class for passing in custom arguments to our event handlers 
	/// 
	/// </summary>
	public class DeviceDetectorEventArgs : EventArgs
	{
		public DeviceDetectorEventArgs(WM_DEVICECHANGE changeType, DEV_BROADCAST_VOLUME volumeInfo)
		{
			Cancel = false;
			_ChangeType = changeType;
			_VolumeInfo = volumeInfo;
		}

		private WM_DEVICECHANGE _ChangeType;
		public WM_DEVICECHANGE ChangeType
		{
			get { return _ChangeType; }
		}

		private DEV_BROADCAST_VOLUME _VolumeInfo;
		public DEV_BROADCAST_VOLUME VolumeInfo
		{
			get { return _VolumeInfo; }
		}

		/// <summary>
		/// Get/Set the value indicating that the event should be cancelled Only in QueryRemove handler.
		/// </summary>
		public bool Cancel;

	}
}
