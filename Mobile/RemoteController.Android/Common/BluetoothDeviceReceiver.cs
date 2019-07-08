using Android.Bluetooth;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace JocysCom.RemoteController
{
	public class BluetoothDeviceReceiver : BroadcastReceiver
	{

		public override void OnReceive(Context context, Intent intent)
		{
			var action = intent.Action;
			if (action != BluetoothDevice.ActionFound)
				return;
			// Get the device
			var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
			if (device.BondState != Bond.Bonded)
			{
				Console.WriteLine($"Found device with name: {device.Name} and MAC address: {device.Address}");
				var ev = Found;
				if (ev != null)
				{
					var e = new BluetoothDeviceReceiverEventArgs();
					e.Device = device;
					ev(this, e);
				}
			}
		}

		public event EventHandler<BluetoothDeviceReceiverEventArgs> Found;
	}
}
