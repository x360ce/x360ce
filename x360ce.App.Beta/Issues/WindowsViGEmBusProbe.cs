using Microsoft.Win32;
using Nefarius.ViGEm.Client;
using System;
using System.ServiceProcess;
using x360ce.App.DInput;

namespace x360ce.App.Issues
{
	public sealed class WindowsViGEmBusProbe : IViGEmBusProbe
	{
		const string ServiceName = "ViGEmBus";
		const string ServiceKey = @"SYSTEM\CurrentControlSet\Services\ViGEmBus";

		public ViGEmDriverInfo GetDriverInfo()
		{
			var driver = VirtualDriverInstaller.GetViGemBusDriverInfo();
			if (driver.DriverVersion == 0)
				return new ViGEmDriverInfo(false, null, null);
			return new ViGEmDriverInfo(true, driver.GetVersion(), driver.Description);
		}

		public ViGEmServiceState GetServiceState()
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
			using (var key = baseKey.OpenSubKey(ServiceKey))
			{
				if (key == null)
					return ViGEmServiceState.Missing;
			}

			using (var service = new ServiceController(ServiceName))
			{
				service.Refresh();
				switch (service.Status)
				{
					case ServiceControllerStatus.Stopped:
						return ViGEmServiceState.Stopped;
					case ServiceControllerStatus.StartPending:
						return ViGEmServiceState.StartPending;
					case ServiceControllerStatus.StopPending:
						return ViGEmServiceState.StopPending;
					case ServiceControllerStatus.Running:
						return ViGEmServiceState.Running;
					case ServiceControllerStatus.Paused:
					case ServiceControllerStatus.PausePending:
					case ServiceControllerStatus.ContinuePending:
						return ViGEmServiceState.Paused;
					default:
						return ViGEmServiceState.Unknown;
				}
			}
		}

		public ViGEmClientProbeResult ConnectClient() => ViGEmClient.ProbeConnection();
	}
}
