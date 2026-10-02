using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace x360ce.Tests
{
	/// <summary>Runs code while another thread holds locks, and says whether it finished without waiting for them.</summary>
	/// <remarks>
	/// The engine must never take a lock the interface thread also takes: one pass that waits for the interface
	/// is the whole budget at a thousand passes a second. Holding the lock on another thread, as the interface
	/// does while it draws or loads, and timing the engine's own code against it, turns that rule into something
	/// a test can fail.
	/// </remarks>
	public static class HeldLock
	{
		/// <summary>True when <paramref name="action"/> finished within <paramref name="milliseconds"/> while another thread held every one of <paramref name="gates"/>.</summary>
		public static bool Finishes(Action action, int milliseconds, params object[] gates)
		{
			return Finishes(action, milliseconds, null, null, gates);
		}

		/// <summary>The same, while the other thread also holds a lock taken by <paramref name="enter"/> and let go of by <paramref name="exit"/>.</summary>
		/// <remarks>For a lock the program's own holders take through a method that says it is held; see <see cref="Enter"/>.</remarks>
		public static bool Finishes(Action action, int milliseconds, Action enter, Action exit, params object[] gates)
		{
			using (var held = new ManualResetEventSlim(false))
			using (var release = new ManualResetEventSlim(false))
			{
				var holder = new Thread(() =>
				{
					var entered = 0;
					var took = false;
					try
					{
						if (enter != null)
						{
							enter();
							took = true;
						}
						for (; entered < gates.Length; entered++)
							Monitor.Enter(gates[entered]);
						held.Set();
						release.Wait();
					}
					finally
					{
						while (entered-- > 0)
							Monitor.Exit(gates[entered]);
						if (took)
							exit();
					}
				});
				holder.IsBackground = true;
				holder.Start();
				held.Wait();
				try
				{
					return Task.Run(action).Wait(milliseconds);
				}
				finally
				{
					release.Set();
					holder.Join();
				}
			}
		}

		/// <summary>Takes a lock through the private static method the program's own holders take it by, with no limit.</summary>
		public static Action Enter(Type type, string method)
		{
			var enter = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(enter, type.Name + " has no " + method + ", so a holder cannot say it holds the lock.");
			return () => enter.Invoke(null, new object[] { Timeout.InfiniteTimeSpan });
		}

		/// <summary>Lets go of a lock through the private static method the program's own holders let go of it by.</summary>
		public static Action Exit(Type type, string method)
		{
			var exit = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(exit, type.Name + " has no " + method + ", so a holder cannot say it let go of the lock.");
			return () => exit.Invoke(null, null);
		}
	}
}
