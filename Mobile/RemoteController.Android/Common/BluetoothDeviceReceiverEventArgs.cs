using Android.Bluetooth;
using System;
using System.Collections.Generic;
using System.Text;

namespace JocysCom.RemoteController
{
	public class BluetoothDeviceReceiverEventArgs: EventArgs
	{
		public BluetoothDevice Device { get; set; }
	}
}
