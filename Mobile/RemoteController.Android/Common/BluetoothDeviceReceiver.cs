using Android.Bluetooth;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace JocysCom.RemoteController
{
	public class BluetoothDeviceReceiver : BroadcastReceiver
	{

		public BluetoothAdapter Adapter => BluetoothAdapter.DefaultAdapter;

		List<BluetoothDevice> FoundDevices = new List<BluetoothDevice>();

		public void StartScanning()
		{
			if (!Adapter.IsDiscovering)
				Adapter.StartDiscovery();
		}

		internal void CancelScanning()
		{
			if (Adapter.IsDiscovering)
				Adapter.CancelDiscovery();
		}

		public override void OnReceive(Context context, Intent intent)
		{
			var action = intent.Action;
			switch (action)
			{
				case BluetoothAdapter.ActionDiscoveryStarted:
					Debug.WriteLine("!!!!!!ActionDiscoveryStarted");
					FoundDevices.Clear();
					break;
				case BluetoothAdapter.ActionDiscoveryFinished:
					var bonded = Adapter.BondedDevices.ToList();
					var found = FoundDevices.Select(x => x.Address).ToArray();
					for (int i = 0; i < bonded.Count; i++)
					{
						var b = bonded[i];
						Debug.WriteLine("    Device: {0} - {1} - {2}", b.Address, b.Name, found.Contains(b.Address));
					}
					StartScanning();
					break;
				case BluetoothDevice.ActionAclConnected:
					// Get the device
					var device2 = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
					// MainActivity.GetInstance().UpdateAdapter(new DataItem(device.Name, device.Address));
					Debug.WriteLine("!!!!!!ActionAclConnected: {0} - {1}", device2.Address, device2.Name);
					break;
				case BluetoothDevice.ActionAclDisconnected:
					// Get the device
					var device3 = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
					// MainActivity.GetInstance().UpdateAdapter(new DataItem(device.Name, device.Address));
					Debug.WriteLine("!!!!!!ActionAclDisconnected: {0} - {1}", device3.Address, device3.Name);
					break;
				case BluetoothDevice.ActionFound:
					// MainActivity.GetInstance().UpdateAdapter(new DataItem(device.Name, device.Address));
					// Get the device
					var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
					FoundDevices.Add(device);
					Debug.WriteLine("!!!!!!ActionFound: {0} - {1} - {2}", device.Address, device.Name, device.BondState);
					if (device.BondState != Bond.Bonded)
					{
						var ev = Found;
						if (ev != null)
						{
							var e = new BluetoothDeviceReceiverEventArgs();
							e.Device = device;
							ev(this, e);
						}
					}
					break;
				default:
					break;
			}
		}

		public event EventHandler<BluetoothDeviceReceiverEventArgs> Found;
	}
}
