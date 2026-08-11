using System;
using System.Diagnostics;

namespace x360ce.App.Issues
{
	public enum ViGEmServiceState
	{
		Missing,
		Stopped,
		StartPending,
		StopPending,
		Running,
		Paused,
		Unknown,
	}

	public enum ViGEmClientConnectionState
	{
		NotAttempted,
		Successful,
		BusNotFound,
		AccessDenied,
		VersionIncompatible,
		ClientUnavailable,
		Failed,
	}

	public sealed class ViGEmDriverInfo
	{
		public ViGEmDriverInfo(bool installed, Version version, string description)
		{
			Installed = installed;
			Version = version;
			Description = description;
		}

		public bool Installed { get; }
		public Version Version { get; }
		public string Description { get; }
	}

	public sealed class ViGEmClientProbeResult
	{
		public ViGEmClientProbeResult(ViGEmClientConnectionState state, string errorMessage = null)
		{
			State = state;
			ErrorMessage = errorMessage;
		}

		public ViGEmClientConnectionState State { get; }
		public string ErrorMessage { get; }
	}

	public interface IViGEmBusProbe
	{
		ViGEmDriverInfo GetDriverInfo();
		ViGEmServiceState GetServiceState();
		ViGEmClientProbeResult ConnectClient();
	}

	public sealed class ViGEmBusHealthResult
	{
		public bool Installed { get; internal set; }
		public Version DriverVersion { get; internal set; }
		public string DriverDescription { get; internal set; }
		public bool ServicePresent { get; internal set; }
		public ViGEmServiceState ServiceState { get; internal set; }
		public bool DriverRunning => ServiceState == ViGEmServiceState.Running;
		public ViGEmClientConnectionState ClientConnectionState { get; internal set; }
		public bool ApiConnectionSuccessful =>
			ClientConnectionState == ViGEmClientConnectionState.Successful;
		public bool VersionIncompatible =>
			ClientConnectionState == ViGEmClientConnectionState.VersionIncompatible;
		public bool IsUsable => ApiConnectionSuccessful;
		public bool ShouldOfferInstall =>
			!Installed && !ServicePresent && !ApiConnectionSuccessful;
		public string ErrorMessage { get; internal set; }
	}

	public sealed class ViGEmBusHealthDetector
	{
		readonly IViGEmBusProbe probe;

		public ViGEmBusHealthDetector(IViGEmBusProbe probe)
		{
			this.probe = probe ?? throw new ArgumentNullException(nameof(probe));
		}

		public ViGEmBusHealthResult Detect()
		{
			var result = new ViGEmBusHealthResult();
			try
			{
				var driver = probe.GetDriverInfo();
				if (driver != null)
				{
					result.Installed = driver.Installed;
					result.DriverVersion = driver.Version;
					result.DriverDescription = driver.Description;
				}
			}
			catch (Exception ex)
			{
				AppendError(result, "Driver detection", ex.Message);
			}

			try
			{
				result.ServiceState = probe.GetServiceState();
				result.ServicePresent = result.ServiceState != ViGEmServiceState.Missing;
			}
			catch (Exception ex)
			{
				result.ServiceState = ViGEmServiceState.Unknown;
				AppendError(result, "Service detection", ex.Message);
			}

			try
			{
				var client = probe.ConnectClient();
				if (client != null)
				{
					result.ClientConnectionState = client.State;
					if (!string.IsNullOrWhiteSpace(client.ErrorMessage))
						AppendError(result, "Client connection", client.ErrorMessage);
				}
			}
			catch (Exception ex)
			{
				result.ClientConnectionState = ViGEmClientConnectionState.Failed;
				AppendError(result, "Client connection", ex.Message);
			}
			return result;
		}

		static void AppendError(ViGEmBusHealthResult result, string stage, string message)
		{
			var detail = stage + ": " + message;
			result.ErrorMessage = string.IsNullOrEmpty(result.ErrorMessage)
				? detail
				: result.ErrorMessage + Environment.NewLine + detail;
		}
	}

	public static class ViGEmBusSupport
	{
		public const string DriverHelpUrl = "https://docs.nefarius.at/projects/ViGEm/How-to-Install/";

		public static bool OpenDriverHelp(out string errorMessage)
		{
			try
			{
				Process.Start(new ProcessStartInfo(DriverHelpUrl) { UseShellExecute = true });
				errorMessage = null;
				return true;
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return false;
			}
		}
	}
}
