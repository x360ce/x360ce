namespace x360ce.Plugins
{
	/// <summary>A plugin that reads live vehicle telemetry from a running game and feeds x360ce's steering wheel spring and telemetry-consuming devices.</summary>
	/// <remarks>
	/// x360ce finds every public class that implements this and has a public constructor without parameters, makes one
	/// instance lazily while wheel telemetry is enabled and either a wheel's centering spring is active or a ticked
	/// plugin device implements <see cref="IPluginTelemetryConsumer"/>, and disposes it (if it implements
	/// <see cref="System.IDisposable"/>) when telemetry is switched off, no wheel or consumer needs it, or x360ce closes.
	///
	/// <see cref="TryReadTelemetry"/> is called on x360ce's engine thread up to a thousand times a second and must return
	/// at once with no allocations and no locks shared with the interface thread. Any socket or shared memory I/O belongs
	/// on a background thread owned by the plugin.
	/// </remarks>
	public interface IPluginWheelTelemetry
	{
		/// <summary>The telemetry provider's name, shown on the Options Plugins page and in plugin documentation.</summary>
		string Name { get; }

		/// <summary>Copies the newest vehicle telemetry into <paramref name="telemetry"/>.</summary>
		/// <param name="telemetry">Filled with the latest sample when this returns true.</param>
		/// <returns>True while a game is actively sending telemetry; false when idle or paused.</returns>
		bool TryReadTelemetry(ref WheelTelemetry telemetry);
	}

	/// <summary>Optional interface for a plugin device that consumes live game telemetry while the device is active.</summary>
	public interface IPluginTelemetryConsumer
	{
		/// <summary>Called on the engine thread with the newest sample while the device is read; no allocations, no locks.</summary>
		void TakeTelemetry(ref WheelTelemetry telemetry);
	}

	/// <summary>Optional configuration and live status interface for a UDP/shared-memory telemetry plugin.</summary>
	public interface IPluginWheelTelemetrySettings
	{
		/// <summary>Updates the plugin's listening settings. Called off the hot path when options change.</summary>
		/// <param name="port">x360ce's dedicated conflict-free loopback UDP port (default 37150).</param>
		/// <param name="listenStandardPorts">True to also bind standard unicast game ports (5300, 20777, 4123, 5606); false by default so other telemetry apps are never blocked.</param>
		/// <param name="forwardPort">Optional local UDP port to relay received datagrams to (for example, SimHub), or 0 for none.</param>
		void Configure(int port, bool listenStandardPorts, int forwardPort);

		/// <summary>Updates optional UDP forwarding targets (comma-separated <c>port</c> or <c>ip:port</c>, e.g. <c>127.0.0.1:20778</c>).</summary>
		void ConfigureForwardTargets(string targets);

		/// <summary>Plain-text status of listeners and any port conflicts, for the Wheel Telemetry tab and plugin diagnostics.</summary>
		string PortStatus { get; }

		/// <summary>Incoming telemetry updates per second.</summary>
		int PacketRateHz { get; }
	}

	/// <summary>Host settings and live status bridge passed to a wheel telemetry plugin's dedicated tab control.</summary>
	/// <remarks>
	/// A wheel telemetry plugin can ship a public Windows Forms control in the same assembly whose constructor takes
	/// <see cref="IPluginWheelTelemetryHost"/>. x360ce hosts that control as a dedicated "Wheel Telemetry" tab on steering
	/// wheel controller pages while the plugin is enabled, and disposes it immediately when disabled or unloaded.
	/// </remarks>
	public interface IPluginWheelTelemetryHost
	{
		/// <summary>Whether live game telemetry is turned on.</summary>
		bool Enabled { get; set; }

		/// <summary>Whether standard game UDP ports (5300, 20777, 4123, 5606) are also bound; false by default so other apps keep their ports.</summary>
		bool ListenStandardPorts { get; set; }

		/// <summary>x360ce's dedicated conflict-free loopback UDP port (default 37150).</summary>
		int Port { get; set; }

		/// <summary>Whether received UDP telemetry datagrams are relayed to <see cref="ForwardIp"/>:<see cref="ForwardPort"/>.</summary>
		bool ForwardEnabled { get; set; }

		/// <summary>Destination IP address to relay received UDP telemetry datagrams to (default 127.0.0.1).</summary>
		string ForwardIp { get; set; }

		/// <summary>Destination UDP port to relay received telemetry datagrams to, or 0 for off.</summary>
		int ForwardPort { get; set; }

		/// <summary>Aligning torque / counter-steer strength scale in percent (0 to 200, default 100).</summary>
		int AlignStrength { get; set; }

		/// <summary>Front-tyre understeer lightening scale in percent (0 to 100, default 100).</summary>
		int UndersteerStrength { get; set; }

		/// <summary>Curb and road bump impulse scale in percent (0 to 100, default 100).</summary>
		int CurbStrength { get; set; }

		/// <summary>Telemetry surface damping scale in percent (0 to 200, default 100).</summary>
		int DampingStrength { get; set; }

		/// <summary>Zero-telemetry rumble-coupled understeer lightening in percent (0 to 60, default 0).</summary>
		int RumbleUndersteer { get; set; }

		/// <summary>Where the wheel's road and impact feel comes from while telemetry drives the spring (default <see cref="WheelRumbleSource.Telemetry"/>, recommended for a wheel).</summary>
		WheelRumbleSource RumbleSource { get; set; }

		/// <summary>How much of the game's rumble plays while the gear changes, in percent (0 to 100, default 0).</summary>
		int GearChangeRumble { get; set; }

		/// <summary>Steering velocity lead compensation in milliseconds (0 to 80, default 0).</summary>
		int SteeringLeadMs { get; set; }

		/// <summary>Restores telemetry tuning and port settings to their defaults.</summary>
		void ResetDefaults();

		/// <summary>Detected telemetry source/protocol name for the active wheel or monitor, or null while waiting.</summary>
		string Source { get; }

		/// <summary>Current vehicle speed in km/h from live telemetry, or null while waiting.</summary>
		int? SpeedKmh { get; }

		/// <summary>Newest full telemetry sample received by the engine, or <see cref="WheelTelemetry.Neutral"/> while idle.</summary>
		WheelTelemetry LiveTelemetry { get; }

		/// <summary>Freshness of <see cref="LiveTelemetry"/> from 0.0 (idle/stale) to 1.0 (live).</summary>
		double Freshness { get; }

		/// <summary>Incoming telemetry updates per second.</summary>
		int PacketRateHz { get; }

		/// <summary>Plain-text status of listeners and any port conflicts.</summary>
		string PortStatus { get; }

		/// <summary>Raised on the UI thread when one of the host settings changes outside this control.</summary>
		event System.EventHandler SettingsChanged;
	}

	/// <summary>Optional interface a wheel telemetry plugin's UI control can implement for periodic live status refreshes.</summary>
	public interface IPluginWheelTelemetryControl
	{
		/// <summary>Refreshes live status labels from the host. Called on the UI thread while the tab is visible.</summary>
		void RefreshStatus();
	}
}
