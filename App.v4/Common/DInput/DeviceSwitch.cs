using JocysCom.ClassLibrary.IO;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace x360ce.App.DInput
{
	/// <summary>Switches a device off and on through the configuration manager, leaving nothing stored.</summary>
	/// <remarks>
	/// Switching a device off through SetupAPI stores a disabled mark for it, which outlives this program, a restart and
	/// unplugging it: a controller left that way is found by nothing until somebody finds the mark. And when a program that has
	/// the device open will not let go of it, Windows still answers success, and leaves the device needing a restart, present
	/// and answering nobody. The configuration manager's own disable, without its persist flag, stops the device and stores
	/// nothing. A restart, or plugging the controller in again, starts it as usual, so a program that stops half way leaves
	/// nothing behind to find. What the device says afterwards is what counts as the answer, not what the call said.
	///
	/// A program that has the device open can refuse to let go of it, and this program is one: XInput keeps every controller it
	/// has answered about open. A device that refuses is taken away as a cable pulled out would take it, by cycling the port it is
	/// plugged in to, and stopped as it arrives again, when nothing has it open.
	/// </remarks>
	public static class DeviceSwitch
	{
		#region Configuration manager

		const uint ProblemDisabled = 22;
		const uint DeviceStarted = 0x8;
		const int AccessDenied = 0x33;

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		static extern int CM_Locate_DevNodeW(out uint devInst, string deviceId, uint flags);

		[DllImport("cfgmgr32.dll")]
		static extern int CM_Disable_DevNode(uint devInst, uint flags);

		[DllImport("cfgmgr32.dll")]
		static extern int CM_Enable_DevNode(uint devInst, uint flags);

		[DllImport("cfgmgr32.dll")]
		static extern int CM_Get_DevNode_Status(out uint status, out uint problem, uint devInst, uint flags);

		#endregion

		/// <summary>The device's node, or null when the device is not present. Replaced by tests that have no device.</summary>
		public static Func<string, uint?> Locate = deviceId =>
		{
			uint node;
			return CM_Locate_DevNodeW(out node, deviceId, 0) == 0 ? node : (uint?)null;
		};

		/// <summary>Asks for the device to stop, politely, and returns what the configuration manager answered, zero being yes. Replaced by tests.</summary>
		public static Func<uint, int> Disable = node => CM_Disable_DevNode(node, 0);

		/// <summary>Asks for the device to start, and returns what the configuration manager answered, zero being yes. Replaced by tests.</summary>
		public static Func<uint, int> Enable = node => CM_Enable_DevNode(node, 0);

		/// <summary>What the device says it is doing: whether it is started, and the problem it reports. Null when it says nothing. Replaced by tests.</summary>
		public static Func<uint, Tuple<bool, uint>> Status = node =>
		{
			uint status, problem;
			return CM_Get_DevNode_Status(out status, out problem, node, 0) == 0
				? Tuple.Create((status & DeviceStarted) != 0, problem)
				: null;
		};

		/// <summary>Cycles the USB port the device is plugged in to. False when it is not plugged in to one. Replaced by tests.</summary>
		public static Func<string, bool> CyclePort = UsbPortCycle.Cycle;

		/// <summary>Clears a disabled mark that an older run, or another program, stored. Replaced by tests.</summary>
		public static Func<string, bool, bool> ClearStoredMark = DeviceDetector.SetDeviceState;

		/// <summary>How long a device has to say it has stopped or started once it was asked to. Replaced by tests.</summary>
		public static TimeSpan Limit = TimeSpan.FromSeconds(15);

		/// <summary>How long a device has to arrive again, and be stopped, after its port was cycled. Replaced by tests.</summary>
		public static TimeSpan CycleLimit = TimeSpan.FromSeconds(25);

		/// <summary>How long a device stopped after its port was cycled is watched, to see it is the one that arrived and not the one that left. Replaced by tests.</summary>
		public static TimeSpan Settle = TimeSpan.FromSeconds(2);

		/// <summary>Switches the device off or on, and returns once it says it is.</summary>
		/// <returns>True when the device then reads as switched off, or as started.</returns>
		/// <exception cref="InvalidOperationException">The device is not present, or did not do what was asked, and says why.</exception>
		public static bool SetState(string deviceId, bool on)
		{
			var node = Locate(deviceId);
			if (node == null)
				throw new InvalidOperationException("The device is not connected.");
			return on ? SwitchOn(node.Value, deviceId) : SwitchOff(node.Value, deviceId);
		}

		/// <summary>Whether the device is switched off, or null when it is not present.</summary>
		public static bool? IsOff(string deviceId)
		{
			var node = Locate(deviceId);
			if (node == null)
				return null;
			var status = Status(node.Value);
			return status != null && Off(status);
		}

		const string NeedsAdministrator = "Switching a device needs Administrator.";

		/// <summary>Stops the device politely, so a program holding it can close it, and by cycling its port when that program will not.</summary>
		static bool SwitchOff(uint node, string deviceId)
		{
			var answer = Disable(node);
			if (answer == AccessDenied)
				throw new InvalidOperationException(NeedsAdministrator);
			// A refusal comes back at once; only a request that was accepted is waited for.
			if (answer == 0 && WaitFor(node, Off, Limit))
				return true;
			if (CyclePort(deviceId) && StopAfterCycle(deviceId))
				return true;
			throw new InvalidOperationException("Windows did not switch it off: something holding it would not let go.");
		}

		/// <summary>Stops the device as it arrives again, and returns once the one that arrived reads as stopped.</summary>
		static bool StopAfterCycle(string deviceId)
		{
			var until = DateTime.UtcNow + CycleLimit;
			do
			{
				var node = Locate(deviceId);
				// The device that was there before may still be in the middle of leaving, so what is stopped is looked at again.
				if (node != null && Disable(node.Value) == 0 && WaitFor(node.Value, Off, TimeSpan.FromSeconds(5)))
				{
					Thread.Sleep(Settle);
					var arrived = Locate(deviceId);
					var status = arrived == null ? null : Status(arrived.Value);
					if (status != null && Off(status))
						return true;
				}
				Thread.Sleep(250);
			}
			while (DateTime.UtcNow < until);
			return false;
		}

		/// <summary>Starts the device, clearing a disabled mark stored by an earlier run when that is what stops it.</summary>
		static bool SwitchOn(uint node, string deviceId)
		{
			var answer = Enable(node);
			if (answer == AccessDenied)
				throw new InvalidOperationException(NeedsAdministrator);
			if (answer == 0 && WaitFor(node, Running, Limit))
				return true;
			ClearStoredMark(deviceId, true);
			if (WaitFor(node, Running, Limit))
				return true;
			throw new InvalidOperationException("Windows did not switch it on. It may need to be unplugged and plugged in.");
		}

		static bool Off(Tuple<bool, uint> status)
		{
			return !status.Item1 && status.Item2 == ProblemDisabled;
		}

		static bool Running(Tuple<bool, uint> status)
		{
			return status.Item1 && status.Item2 == 0;
		}

		/// <summary>Waits for what the device says to satisfy the test, up to the limit.</summary>
		static bool WaitFor(uint node, Func<Tuple<bool, uint>, bool> test, TimeSpan limit)
		{
			var until = DateTime.UtcNow + limit;
			do
			{
				var status = Status(node);
				if (status != null && test(status))
					return true;
				Thread.Sleep(100);
			}
			while (DateTime.UtcNow < until);
			return false;
		}
	}
}
