using System;
using System.Collections.Concurrent;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Plugins;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		/// <summary>The plugin instance behind each plugin device read, by the device's instance GUID.</summary>
		/// <remarks>
		/// Made on a device's first read and kept, so a read makes nothing. Added and removed only on the engine thread;
		/// the interface looks one up to show the device's status and its plugin's control.
		/// </remarks>
		readonly ConcurrentDictionary<Guid, PluginDeviceReader> _pluginReaders = new ConcurrentDictionary<Guid, PluginDeviceReader>();

		/// <summary>The plugin instance the engine reads for a device, or null while it reads none.</summary>
		public IPluginDevice GetPluginDevice(Guid instanceGuid)
		{
			PluginDeviceReader reader;
			return _pluginReaders.TryGetValue(instanceGuid, out reader) ? reader.Device : null;
		}

		/// <summary>Reads a plugin device: gives the plugin the device's settings, force feedback, and live telemetry if consumed, asks it, and swaps its state in as a Raw Input device's is.</summary>
		/// <remarks>
		/// The plugin is called on this thread, so its Read and TakeTelemetry must return at once with no allocations or locks.
		/// A plugin that throws switches its kind of device off until the program starts again: the manager says so on the
		/// Issues tab and in the Plugins list, and the next device refresh takes its devices offline. Until then the device keeps what it showed last.
		/// </remarks>
		/// <param name="ud">The plugin device.</param>
		/// <param name="routing">Where each device's force comes from, for this pass.</param>
		/// <param name="o">Current application options, read once per pass.</param>
		/// <param name="telemetryPolled">Whether live telemetry has already been read during this engine pass.</param>
		/// <param name="passTelemetry">Engine telemetry snapshot shared across all wheels and consumers in this pass.</param>
		/// <param name="passRawTelemetry">Raw <see cref="WheelTelemetry"/> sample shared across all consumers in this pass.</param>
		/// <param name="anyTelemetryConsumerActive">Set to true whenever a ticked plugin device implements <see cref="IPluginTelemetryConsumer"/>.</param>
		void ReadPluginState(
			UserDevice ud,
			DeviceRouting routing,
			Options o,
			ref bool telemetryPolled,
			ref WheelTelemetrySample passTelemetry,
			ref WheelTelemetry passRawTelemetry,
			ref bool anyTelemetryConsumerActive)
		{
			var manager = PluginManager.Current;
			PluginDeviceReader reader;
			if (!_pluginReaders.TryGetValue(ud.InstanceGuid, out reader))
			{
				var type = PluginDevices.FindType(ud, manager);
				if (type == null || type.Faulted)
					return;
				try
				{
					reader = new PluginDeviceReader(type, type.NewInstance());
				}
				catch (Exception ex)
				{
					manager.Fault(type, ex);
					return;
				}
				_pluginReaders[ud.InstanceGuid] = reader;
			}
			if (!reader.Type.File.Enabled || reader.Type.File.State != PluginFileState.Loaded)
			{
				ReleasePluginReader(ud);
				return;
			}
			if (reader.Type.Faulted)
				return;
			try
			{
				reader.ApplySettings(PluginSettings.Current, ud.InstanceGuid);
				// The strongest of each motor over the tabs whose Force feedback enabled switch is on, as their games asked
				// for it, before the tabs' strengths; nought on no such tab. Looked up only for a plugin that takes force:
				// a dictionary read and at most four compares, which take no lock and make nothing.
				if (reader.Force != null)
				{
					byte large = 0;
					byte small = 0;
					DeviceForce route;
					if (routing.TryGetForce(ud.InstanceGuid, out route))
						MergeForces(route.ForcePads, 0, _lastLargeMotor, _lastSmallMotor, out large, out small);
					// 255 * 257 is 65535.
					reader.SetForce(large * 257, small * 257);
				}
				if (reader.TelemetryConsumer != null)
				{
					anyTelemetryConsumerActive = true;
					var deliver = WheelTelemetry.Neutral;
					if (o.WheelTelemetryEnabled)
					{
						if (!telemetryPolled)
						{
							telemetryPolled = true;
							passTelemetry = ReadWheelTelemetry(o, out passRawTelemetry);
						}
						if (passTelemetry.Active && WheelTelemetryMath.Freshness(passTelemetry.TimestampMs, WheelTelemetryMath.MonotonicNowMs()) > 0.0)
							deliver = passRawTelemetry;
					}
					reader.TelemetryConsumer.TakeTelemetry(ref deliver);
				}
				reader.Device.Read(reader.State);
				if (reader.Identity != null)
				{
					var hw = reader.Identity.HardwareIdentity;
					if (hw != null && !ReferenceEquals(hw, reader.AppliedIdentity))
					{
						reader.AppliedIdentity = hw;
						var targetUd = ud;
						var targetType = reader.Type;
						System.Threading.ThreadPool.QueueUserWorkItem(_ => PluginDevices.ApplyHardwareIdentity(targetUd, targetType, hw));
					}
				}
			}
			catch (Exception ex)
			{
				if (manager != null)
					manager.Fault(reader.Type, ex);
				return;
			}
			var newState = ud.OldSourceState ?? new SourceState();
			reader.CopyTo(newState);
			LogChanges(ud, ud.SourceState, newState, watch.ElapsedTicks);
			ud.OldSourceState = ud.SourceState;
			ud.OldSourceStateTime = ud.SourceStateTime;
			ud.SourceState = newState;
			ud.SourceStateTime = watch.ElapsedTicks;
		}

		/// <summary>Lets go of the plugin instance behind a device no pass reads any more.</summary>
		void ReleasePluginReader(UserDevice ud)
		{
			PluginDeviceReader reader;
			if (!_pluginReaders.TryRemove(ud.InstanceGuid, out reader))
				return;
			DisposePluginReader(reader);
		}

		/// <summary>Lets go of every plugin instance, as the engine thread ends.</summary>
		void ReleasePluginReaders()
		{
			foreach (var reader in _pluginReaders.Values)
				DisposePluginReader(reader);
			_pluginReaders.Clear();
			ReleaseWheelTelemetry();
		}

		#region Wheel telemetry providers

		sealed class TelemetryProviderSlot
		{
			public PluginTelemetryType Type;
			public IPluginWheelTelemetry Provider;
			public IPluginWheelTelemetrySettings Settings;
		}

		PluginTelemetryType[] _telemetryKnownTypes;
		TelemetryProviderSlot[] _telemetrySlots = new TelemetryProviderSlot[0];
		int _telemetryPort = -1;
		bool _telemetryStandardPorts;
		bool _telemetryForwardEnabled;
		string _telemetryForwardIp;
		int _telemetryForwardPort = -1;
		string _telemetryForwardTargets;

		/// <summary>Latest listener / port conflict note from active wheel telemetry providers, for the Wheel tab.</summary>
		public string WheelTelemetryPortStatus { get { return _wheelTelemetryPortStatus; } }
		volatile string _wheelTelemetryPortStatus;

		/// <summary>Latest live telemetry packet rate in Hz, for diagnostics.</summary>
		public int WheelTelemetryPacketRateHz { get { return _wheelTelemetryPacketRateHz; } }
		volatile int _wheelTelemetryPacketRateHz;

		// Lock-free seqlock holding the newest telemetry sample for UI dashboards (works even without a physical FFB wheel).
		int _latestTelemetrySeq;
		WheelTelemetrySample _latestTelemetry = WheelTelemetrySample.Inactive;
		long _wheelTelemetryMonitoredUntilMs;

		/// <summary>Marks wheel telemetry as actively observed by the UI (e.g. while the Wheel Telemetry tab is open) so live telemetry is decoded even without an FFB wheel attached.</summary>
		public void MarkWheelTelemetryMonitored()
		{
			System.Threading.Volatile.Write(ref _wheelTelemetryMonitoredUntilMs, WheelTelemetryMath.MonotonicNowMs() + 2000);
		}

		bool IsWheelTelemetryMonitored()
		{
			var until = System.Threading.Volatile.Read(ref _wheelTelemetryMonitoredUntilMs);
			return until > 0 && WheelTelemetryMath.MonotonicNowMs() <= until;
		}

		/// <summary>Lock-free read of the newest wheel telemetry sample received by the engine.</summary>
		public WheelTelemetrySample GetLatestWheelTelemetry()
		{
			for (var tries = 0; tries < 4; tries++)
			{
				var s1 = System.Threading.Volatile.Read(ref _latestTelemetrySeq);
				if ((s1 & 1) != 0)
					continue;
				var copy = _latestTelemetry;
				var s2 = System.Threading.Volatile.Read(ref _latestTelemetrySeq);
				if (s1 == s2)
					return copy;
			}
			return WheelTelemetrySample.Inactive;
		}

		void PublishLatestWheelTelemetry(WheelTelemetrySample sample)
		{
			var next = _latestTelemetrySeq + 1;
			System.Threading.Volatile.Write(ref _latestTelemetrySeq, next);
			_latestTelemetry = sample;
			System.Threading.Volatile.Write(ref _latestTelemetrySeq, next + 1);
		}

		/// <summary>True while any wheel telemetry provider is active and holding listeners open.</summary>
		bool HasActiveWheelTelemetry { get { return _telemetrySlots.Length > 0; } }

		/// <summary>Reads live vehicle telemetry from loaded <see cref="IPluginWheelTelemetry"/> providers on the engine thread.</summary>
		/// <remarks>
		/// Zero allocations and zero UI locks on the hot path. Instantiates providers lazily only while telemetry is
		/// enabled and at least one wheel centering spring, ticked <see cref="IPluginTelemetryConsumer"/> device, or UI
		/// monitor is active, and applies port configuration only when changed.
		/// </remarks>
		WheelTelemetrySample ReadWheelTelemetry(Options o, out WheelTelemetry rawTelemetry)
		{
			rawTelemetry = WheelTelemetry.Neutral;
			var manager = PluginManager.Current;
			if (manager == null)
				return WheelTelemetrySample.Inactive;

			var types = manager.TelemetryTypes;
			if (!ReferenceEquals(types, _telemetryKnownTypes))
			{
				RebuildWheelTelemetrySlots(manager, types);
				_telemetryPort = -1;
			}

			var slots = _telemetrySlots;
			if (slots.Length == 0)
				return WheelTelemetrySample.Inactive;

			// Push port configuration only when one of the options actually changed (compare raw references/values first for zero allocations).
			var port = o.WheelTelemetryPort;
			var std = o.WheelTelemetryStandardPorts;
			var fwdEnabled = o.WheelTelemetryForwardEnabled;
			var fwdIp = o.WheelTelemetryForwardIp ?? "";
			var fwd = o.WheelTelemetryForwardPort;
			var fwdTargets = o.WheelTelemetryForwardTargets ?? "";
			if (port != _telemetryPort || std != _telemetryStandardPorts || fwdEnabled != _telemetryForwardEnabled || fwdIp != _telemetryForwardIp || fwd != _telemetryForwardPort || fwdTargets != _telemetryForwardTargets)
			{
				_telemetryPort = port;
				_telemetryStandardPorts = std;
				_telemetryForwardEnabled = fwdEnabled;
				_telemetryForwardIp = fwdIp;
				_telemetryForwardPort = fwd;
				_telemetryForwardTargets = fwdTargets;
				var normalizedFwdIp = string.IsNullOrWhiteSpace(fwdIp) ? "127.0.0.1" : fwdIp.Trim();
				var effectiveTargets = !fwdEnabled
					? ""
					: (!string.IsNullOrWhiteSpace(fwdTargets)
						? fwdTargets
						: (fwd >= 1024 && fwd <= 65535 ? normalizedFwdIp + ":" + fwd : ""));
				for (var i = 0; i < slots.Length; i++)
				{
					var settings = slots[i].Settings;
					if (settings == null || slots[i].Type.Faulted)
						continue;
					try
					{
						settings.Configure(port, std, 0);
						settings.ConfigureForwardTargets(effectiveTargets);
					}
					catch (Exception ex)
					{
						manager.Fault(slots[i].Type, ex);
					}
				}
			}

			var raw = WheelTelemetry.Neutral;
			for (var i = 0; i < slots.Length; i++)
			{
				var slot = slots[i];
				if (slot.Type.Faulted)
					continue;
				if (slot.Settings != null)
				{
					_wheelTelemetryPortStatus = slot.Settings.PortStatus;
					_wheelTelemetryPacketRateHz = slot.Settings.PacketRateHz;
				}
				try
				{
					if (slot.Provider.TryReadTelemetry(ref raw))
					{
						rawTelemetry = raw;
						var sample = new WheelTelemetrySample
						{
							Active = true,
							SpeedKmh = raw.SpeedKmh,
							AlignAngleDeg = raw.AlignAngleDeg,
							FrontGrip = raw.FrontGrip,
							LateralG = raw.LateralG,
							Impulse = raw.Impulse,
							Jolt = raw.Jolt,
							LongitudinalG = raw.LongitudinalG,
							VerticalG = raw.VerticalG,
							Gear = raw.Gear,
							DistanceMeters = raw.DistanceMeters,
							PositionX = raw.PositionX,
							PositionY = raw.PositionY,
							PositionZ = raw.PositionZ,
							DampingScale = raw.DampingScale,
							RpmFraction = raw.RpmFraction,
							PitchDeg = raw.PitchDeg,
							TimestampMs = raw.TimestampMs,
							Source = raw.Source,
						};
						PublishLatestWheelTelemetry(sample);
						return sample;
					}
				}
				catch (Exception ex)
				{
					manager.Fault(slot.Type, ex);
				}
			}
			return WheelTelemetrySample.Inactive;
		}

		void RebuildWheelTelemetrySlots(PluginManager manager, PluginTelemetryType[] types)
		{
			ReleaseWheelTelemetry();
			_telemetryKnownTypes = types;
			if (types == null || types.Length == 0)
				return;
			var list = new System.Collections.Generic.List<TelemetryProviderSlot>(types.Length);
			for (var i = 0; i < types.Length; i++)
			{
				var type = types[i];
				if (type.Faulted)
					continue;
				try
				{
					var provider = type.NewInstance();
					list.Add(new TelemetryProviderSlot
					{
						Type = type,
						Provider = provider,
						Settings = provider as IPluginWheelTelemetrySettings,
					});
				}
				catch (Exception ex)
				{
					manager.Fault(type, ex);
				}
			}
			_telemetrySlots = list.ToArray();
		}

		/// <summary>Disposes all active wheel telemetry providers so UDP sockets and shared memory handles are released immediately.</summary>
		void ReleaseWheelTelemetry()
		{
			var slots = _telemetrySlots;
			_telemetrySlots = new TelemetryProviderSlot[0];
			_telemetryKnownTypes = null;
			_telemetryPort = -1;
			_wheelTelemetryPortStatus = null;
			_wheelTelemetryPacketRateHz = 0;
			PublishLatestWheelTelemetry(WheelTelemetrySample.Inactive);
			for (var i = 0; i < slots.Length; i++)
			{
				var disposable = slots[i].Provider as IDisposable;
				if (disposable == null)
					continue;
				try
				{
					disposable.Dispose();
				}
				catch (Exception ex)
				{
					var manager = PluginManager.Current;
					if (manager != null)
						manager.Fault(slots[i].Type, ex);
				}
			}
		}

		#endregion

		static void DisposePluginReader(PluginDeviceReader reader)
		{
			try
			{
				reader.Dispose();
			}
			catch (Exception ex)
			{
				var manager = PluginManager.Current;
				if (manager != null)
					manager.Fault(reader.Type, ex);
			}
		}
	}
}
