using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Reflection;
using x360ce.App.DInput;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The engine's private steps, called as a pass calls them, with no hardware and no window.</summary>
	/// <remarks>Each step is made into a delegate once, so a test that calls it in a loop measures the step and not the call.</remarks>
	internal static class EngineSteps
	{
		/// <summary>The whole pass, <c>RefreshAll</c>: Steps 1 to 6 for the current game.</summary>
		public delegate void Pass(DirectInput manager, DeviceDetector detector);

		/// <summary>Step 2, <c>UpdateDiStates</c>: reads the devices the routing maps.</summary>
		public delegate void DiStates(DirectInput manager, UserGame game, DeviceDetector detector, DeviceRouting routing);

		/// <summary>Step 3, <c>UpdateXiStates</c>: converts each routed row.</summary>
		public delegate void XiStates(DeviceRouting routing);

		/// <summary>Step 6, <c>RetrieveXiStates</c>: takes the XInput view's last read and asks for the next.</summary>
		public delegate void XiRetrieve(UserGame game, bool getXInputStates);

		/// <summary>The whole pass of <paramref name="helper"/>.</summary>
		public static Pass RefreshAll(DInputHelper helper)
		{
			return Step<Pass>(helper, "RefreshAll");
		}

		/// <summary>Puts <paramref name="helper"/> in the state every pass has while a worker reads the device list: a list is wanted and a read is under way. No worker is started.</summary>
		public static void DeviceListReadUnderWay(DInputHelper helper)
		{
			helper.UpdateDevicesPending = true;
			typeof(DInputHelper).GetField("_deviceListReading", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(helper, true);
		}

		/// <summary>Step 2 of <paramref name="helper"/>.</summary>
		public static DiStates UpdateDiStates(DInputHelper helper)
		{
			return Step<DiStates>(helper, "UpdateDiStates");
		}

		/// <summary>Step 3 of <paramref name="helper"/>.</summary>
		public static XiStates UpdateXiStates(DInputHelper helper)
		{
			return Step<XiStates>(helper, "UpdateXiStates");
		}

		/// <summary>Step 6 of <paramref name="helper"/>.</summary>
		public static XiRetrieve RetrieveXiStates(DInputHelper helper)
		{
			return Step<XiRetrieve>(helper, "RetrieveXiStates");
		}

		/// <summary>The display reader's read of the four places, run on the calling thread.</summary>
		public static Action ReadDisplayStates(DInputHelper helper)
		{
			return Step<Action>(helper, "ReadDisplayStates");
		}

		/// <summary>Asks the display reader for one read, making the reader on the first ask.</summary>
		public static Action AskDisplayRead(DInputHelper helper)
		{
			return Step<Action>(helper, "AskDisplayRead");
		}

		/// <summary>One of the engine's private steps, as a delegate made once, so calling it makes nothing.</summary>
		static T Step<T>(DInputHelper helper, string name) where T : class
		{
			var method = typeof(DInputHelper).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
			return (T)(object)Delegate.CreateDelegate(typeof(T), helper, method);
		}
	}
}
