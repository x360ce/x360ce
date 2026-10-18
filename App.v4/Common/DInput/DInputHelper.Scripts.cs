using System;
using System.Diagnostics;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		/// <summary>The channel to an AutoHotkey script, open while <see cref="Options.AllowAutoHotkeyScripts"/> is on. Engine thread only.</summary>
		ScriptChannel _scripts;

		/// <summary>Opening the channel failed: it is not tried again until the option is turned off and on.</summary>
		bool _scriptsFailed;

		/// <summary>The routing the channel's slots were given for, and the slot of each of its devices.</summary>
		DeviceRouting _scriptRouting;
		int[] _scriptSlots = new int[0];

		/// <summary>True while an AutoHotkey script is connected to the engine.</summary>
		public bool ScriptConnected
		{
			get
			{
				var scripts = _scripts;
				return scripts != null && scripts.Connected;
			}
		}

		/// <summary>Gives a connected AutoHotkey script the routed devices' states, and sets what each device's controller reads.</summary>
		/// <remarks>
		/// Called once a pass, after every device is read. The script is never waited for: what it sent since the last pass
		/// is applied now, and what it is told now it answers on a later pass. A device the script changes nothing on keeps
		/// <see cref="UserDevice.ScriptState"/> null, and its controller reads the device as it is.
		/// </remarks>
		void UpdateScriptStates(DeviceRouting routing)
		{
			var o = SettingsManager.Options;
			if (!o.AllowAutoHotkeyScripts)
			{
				_scriptsFailed = false;
				CloseScripts();
				return;
			}
			if (_scripts == null)
			{
				if (_scriptsFailed)
					return;
				try
				{
					_scripts = new ScriptChannel();
				}
				catch (Exception ex)
				{
					// Written once: the next pass would fail the same way.
					_scriptsFailed = true;
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					return;
				}
			}
			_scripts.HeartbeatMilliseconds = o.AutoHotkeyHeartbeat;
			// At least three heartbeats, so one late beat never takes a working script for hung.
			_scripts.LeaseMilliseconds = Math.Max(o.AutoHotkeyHangTimeout, 3 * o.AutoHotkeyHeartbeat);
			var devices = routing.MappedDevices;
			if (routing != _scriptRouting)
			{
				LetGoOfScriptStates();
				_scriptRouting = routing;
				var numbers = new int[devices.Length];
				for (var i = 0; i < devices.Length; i++)
					numbers[i] = GetScriptNumbers(routing, devices[i].InstanceGuid);
				_scriptSlots = new int[devices.Length];
				_scripts.Route(devices, numbers, _scriptSlots);
			}
			var now = Stopwatch.GetTimestamp();
			var connected = _scripts.BeginPass(now);
			for (var i = 0; i < devices.Length; i++)
			{
				var ud = devices[i];
				ud.ScriptState = connected && ud.IsOnline && ud.SourceState != null
					? _scripts.Apply(_scriptSlots[i], ud.SourceState)
					: null;
			}
			_scripts.EndPass(now);
		}

		/// <summary>A device's number on each controller of a routing, a byte each: the lowest byte is its number on Controller 1, 0 where it is not on one.</summary>
		static int GetScriptNumbers(DeviceRouting routing, Guid instanceGuid)
		{
			var numbers = 0;
			for (var i = 0; i < routing.Rows.Length; i++)
			{
				var row = routing.Rows[i];
				var shift = (row.MapTo - 1) * 8;
				if (row.InstanceGuid == instanceGuid && ((numbers >> shift) & 0xFF) == 0)
					numbers |= Math.Min(routing.RowNumbers[i], 0xFF) << shift;
			}
			return numbers;
		}

		/// <summary>Gives every device of the routing the channel last served back to its own state.</summary>
		void LetGoOfScriptStates()
		{
			var routing = _scriptRouting;
			if (routing == null)
				return;
			var devices = routing.MappedDevices;
			for (var i = 0; i < devices.Length; i++)
				devices[i].ScriptState = null;
		}

		/// <summary>Closes the channel: every device reaches its controller as it is, and a script finds no engine.</summary>
		void CloseScripts()
		{
			if (_scripts == null)
				return;
			LetGoOfScriptStates();
			_scriptRouting = null;
			_scripts.Dispose();
			_scripts = null;
		}
	}
}
