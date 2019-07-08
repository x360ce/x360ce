using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Bluetooth;

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

			var app = new App();
			BluetoothReceiver = new BluetoothDeviceReceiver();
			BluetoothReceiver.Found += BluetoothReceiver_Found; ;
			RegisterReceiver(BluetoothReceiver, new IntentFilter(BluetoothDevice.ActionFound));

			LoadApplication(app);
			//UnregisterReceiver(BluetoothReceiver);

		}

		public BluetoothDeviceReceiver BluetoothReceiver { get; set; }



		private void BluetoothReceiver_Found(object sender, BluetoothDeviceReceiverEventArgs e)
		{
			Console.WriteLine("Device: {0}", e.Device.Name);
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

    }
}
