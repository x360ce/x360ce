using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Bluetooth;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;
using System.Collections.Generic;
using System.Linq;

namespace JocysCom.RemoteController.Droid
{
	[Activity(Label = "JocysCom.RemoteController", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
			InitReceiver();
			var app = new App();
			LoadApplication(app);

		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			// Make sure we're not doing discovery anymore
			_receiver.CancelScanning();
			// Unregister broadcast listeners
			UnregisterBluetoothReceiver();
		}

		protected override void OnPause()
		{
			base.OnPause();
			// Make sure we're not doing discovery anymore
			_receiver.CancelScanning();
			// Unregister broadcast listeners
			UnregisterBluetoothReceiver();
		}

		protected override void OnResume()
		{
			base.OnResume();
			_receiver.StartScanning();
			// Register broadcast listeners
			RegisterBluetoothReceiver();
		}


		public BluetoothDeviceReceiver BluetoothReceiver { get; set; }

		private static readonly string[] LocationPermissions =
	 {
			Manifest.Permission.AccessCoarseLocation,
			Manifest.Permission.AccessFineLocation
		};

		void InitReceiver()
		{
			var coarseLocationPermissionGranted =
		 ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation);
			var fineLocationPermissionGranted =
				ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation);

			if (coarseLocationPermissionGranted != Permission.Denied ||
				fineLocationPermissionGranted == Permission.Denied)
				ActivityCompat.RequestPermissions(this, LocationPermissions, LocationPermissionsRequestCode);

			//var discoverableIntent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
			//discoverableIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, 300);
			//StartActivity(discoverableIntent);

			// Register for broadcasts when a device is discovered
			_receiver = new BluetoothDeviceReceiver();

			//var callback = BluetoothGattCallback


			RegisterBluetoothReceiver();

			PopulateListView();
		}


		private void RegisterBluetoothReceiver()
		{
			if (_isReceiveredRegistered)
				return;
			RegisterReceiver(_receiver, new IntentFilter(BluetoothDevice.ActionFound));
			RegisterReceiver(_receiver, new IntentFilter(BluetoothDevice.ActionAclConnected));
			RegisterReceiver(_receiver, new IntentFilter(BluetoothDevice.ActionAclDisconnected));
			RegisterReceiver(_receiver, new IntentFilter(BluetoothDevice.ActionAclDisconnectRequested));
			RegisterReceiver(_receiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryStarted));
			RegisterReceiver(_receiver, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));
			_isReceiveredRegistered = true;
		}

		private void UnregisterBluetoothReceiver()
		{
			if (!_isReceiveredRegistered) return;

			UnregisterReceiver(_receiver);
			_isReceiveredRegistered = false;
		}

		private const int LocationPermissionsRequestCode = 1000;
		private BluetoothDeviceReceiver _receiver;
		private bool _isReceiveredRegistered;

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		private void PopulateListView()
		{
			var item = new List<BluetoothDevice>();
			var paired = _receiver.Adapter.BondedDevices.ToList();
			foreach (var device in paired)
			{
				System.Diagnostics.Debug.WriteLine("Paired: {0} - {1} - {2}", device.Address, device.Name, device.BondState);
			}
			item.AddRange(paired);
			_receiver.StartScanning();
		}

	}
}
