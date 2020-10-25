using System.Diagnostics;
using System.Reflection;

namespace x360ce.App
{
	/// <summary>
	/// Global class to host all services except interface.
	/// </summary>
	public partial class Global
	{

		public static CloudClient CloudClient;

		public static void InitializeCloudClient()
		{
			CloudClient = new CloudClient();
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
		}

		public static void DisposeCloudClient()
		{
			CloudClient.Dispose();
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
		}

		#region Remote Server

		public static Service.RemoteService RemoteServer;

		public static void InitializeRemoteService()
		{
			RemoteServer = new Service.RemoteService();
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
			// Add event which will start and stop UDP server depending on options.
			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
		}

		public static void DisposeRemoteService()
		{
			SettingsManager.Options.PropertyChanged -= Options_PropertyChanged;
			Trace.TraceInformation("{0}", MethodBase.GetCurrentMethod().Name);
		}

		private static void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = SettingsManager.Options;
			switch (e.PropertyName)
			{
				case nameof(Options.RemoteEnabled):
					// If UDP server must be enabled then...
					if (o.RemoteEnabled)
						RemoteServer.StartServer();
					else
						RemoteServer.StopServer();
					break;
				default:
					break;
			}
		}

		#endregion

		#region Dinput Helper

		public static DInput.DInputHelper DHelper;

		public static void InitDHelperHelper()
		{
			// Initialize DInput Helper.
			DHelper = new DInput.DInputHelper();
		}


		#endregion

	}
}
