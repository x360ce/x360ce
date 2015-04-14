using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using x360ce.App.Controls;
using x360ce.Engine.Win32;

namespace x360ce.App
{

	public class DeviceDetector
	{

		/// <summary>
		/// Handle of the window which receives messages from Windows. This will be a form.
		/// </summary>
		IntPtr _RecipientHandle;

		// Win32 constants.
		const int BROADCAST_QUERY_DENY = 0x424D5144;
		const int WM_DEVICECHANGE = 0x0219;
		const int DBT_DEVTYP_HANDLE = 6;
		const int DBT_DEVTYP_VOLUME = 0x00000002; // drive type is logical volume

		public delegate void DeviceDetectorEventHandler(Object sender, DeviceDetectorEventArgs e);

		/// <summary>
		/// Events signalized to the client app.
		/// Add handlers for these events in your form to be notified of removable device events 
		/// </summary>
		public event DeviceDetectorEventHandler DeviceChanged;
		/// <summary>
		/// Create hidden form for processing Windows messages about USB drives. You do not need to override WndProc in your form.
		/// </summary>
		public DeviceDetector(bool showForm)
		{
			var frm = new DeviceDetectorForm(this);
			if (showForm) frm.Show();
			_RecipientHandle = frm.Handle;
		}

		/// <summary>
		/// Message handler which must be called from client form. Processes Windows messages and calls event handlers. 
		/// </summary>
		/// <param name="m"></param>
		public void WndProc(ref Message m)
		{
			int devType;
			if (m.Msg == WM_DEVICECHANGE)
			{
				var changeType = (DBT)m.WParam.ToInt32();
				var volumeInfo = new DEV_BROADCAST_VOLUME();
                if (changeType == DBT.DBT_DEVICEARRIVAL || changeType == DBT.DBT_DEVICEREMOVECOMPLETE)
				{
					devType = Marshal.ReadInt32(m.LParam, 4);
					if (devType == DBT_DEVTYP_VOLUME)
					{
						volumeInfo = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));
					}
				}
				var e = new DeviceDetectorEventArgs(changeType, volumeInfo);
				if (DeviceChanged != null) DeviceChanged(this, e);
				switch (changeType)
				{
					// Device is about to be removed. Any application can cancel the removal.
                    case DBT.DBT_DEVICEQUERYREMOVE:
						devType = Marshal.ReadInt32(m.LParam, 4);
						if (devType == DBT_DEVTYP_HANDLE)
						{
							// If the client wants to cancel, let Windows know.
							if (e.Cancel) m.Result = (IntPtr)BROADCAST_QUERY_DENY;
						}
						break;
				}
			}
		}
	}
}
