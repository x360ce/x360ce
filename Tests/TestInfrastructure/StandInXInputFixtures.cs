using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>Windows' XInput replaced, for the length of a test, by one that answers as the test sets.</summary>
	/// <remarks>
	/// The places reader calls XInputGetState for the four places in order. Here that call waits as the test sets and
	/// then says whether each place is taken, so a test can make XInput silent, slow, or show a controller in any place,
	/// without a controller or a driver on the machine. Used with a bus client that has nothing native behind it.
	/// </remarks>
	internal static class StandInXInputFixtures
	{
		/// <summary>What XInput answers for a place with no controller: ERROR_DEVICE_NOT_CONNECTED.</summary>
		public const int NotConnected = 1167;

		/// <summary>Where Windows puts a controller the stand-in bus holds: XInput 3, another tab's place.</summary>
		public const int GivenPlace = 2;

		/// <summary>The places the stand-in XInput reports taken, one bit each.</summary>
		public static int Taken;

		/// <summary><see cref="Environment.TickCount"/> until which the stand-in XInput does not answer.</summary>
		public static int SilentUntil;

		/// <summary>How long each read of the four places takes, in milliseconds.</summary>
		public static int ReadTakes;

		/// <summary>The stand-in controller for Controller 1, shown in <see cref="GivenTo"/> while the bus holds it; or null.</summary>
		public static volatile RefusedRemovalTest.FakePad Plugged;

		/// <summary>Where Windows puts the stand-in controller: <see cref="GivenPlace"/> unless a test says otherwise, and nowhere while below zero.</summary>
		public static int GivenTo = GivenPlace;

		/// <summary>Whether the stand-in XInput says nothing while the stand-in controller is on the bus.</summary>
		public static volatile bool SilentWhilePlugged;

		/// <summary>Stands in for XInputGetState: waits as the test sets, then says whether the place is taken.</summary>
		public static int Answer(int place)
		{
			// Once per read: the reader asks for the four places in order.
			if (place == 0)
			{
				var silent = unchecked(Volatile.Read(ref SilentUntil) - Environment.TickCount);
				if (silent > 0)
					Thread.Sleep(silent);
				var takes = Volatile.Read(ref ReadTakes);
				if (takes > 0)
					Thread.Sleep(takes);
				// Windows builds the controller for as long as it is on the bus, and XInput waits for it.
				for (var waited = 0; waited < 15000; waited += 10)
				{
					var building = Plugged;
					if (!SilentWhilePlugged || building == null || !building.Attached)
						break;
					Thread.Sleep(10);
				}
			}
			var taken = Volatile.Read(ref Taken);
			var pad = Plugged;
			var given = Volatile.Read(ref GivenTo);
			if (pad != null && pad.Attached && given >= 0)
				taken |= 1 << given;
			return (taken & (1 << place)) != 0 ? 0 : NotConnected;
		}

		/// <summary>Runs the action with Windows' XInput replaced by <see cref="Answer"/>, and puts it back after.</summary>
		public static void WithStandInXInput(Action action)
		{
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer before the test.");
			var field = typeof(SystemXInput).GetField("_GetState", BindingFlags.NonPublic | BindingFlags.Static);
			var parameters = field.FieldType.GetMethod("Invoke").GetParameters().Select(x => Expression.Parameter(x.ParameterType)).ToArray();
			var standIn = Expression.Lambda(field.FieldType,
				Expression.Call(typeof(StandInXInputFixtures).GetMethod(nameof(Answer)), parameters[0]), parameters).Compile();
			var loadLock = XInputPlacesNotAnsweringTest.LoadLock();
			object loaded;
			// The reader holds the load lock for each read, so the library is never swapped under one.
			lock (loadLock)
			{
				loaded = field.GetValue(null);
				field.SetValue(null, standIn);
			}
			try
			{
				action();
			}
			finally
			{
				Volatile.Write(ref SilentUntil, Environment.TickCount);
				Volatile.Write(ref ReadTakes, 0);
				Volatile.Write(ref Taken, 0);
				Volatile.Write(ref GivenTo, GivenPlace);
				SilentWhilePlugged = false;
				Plugged = null;
				lock (loadLock)
					if (ReferenceEquals(field.GetValue(null), standIn))
						field.SetValue(null, loaded);
			}
			Assert.IsNotNull(DInputHelper.OccupiedPlaces(), "XInput does not answer after the test.");
		}
	}
}
