using System;
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
			using (var held = new ManualResetEventSlim(false))
			using (var release = new ManualResetEventSlim(false))
			{
				var holder = new Thread(() =>
				{
					var entered = 0;
					try
					{
						for (; entered < gates.Length; entered++)
							Monitor.Enter(gates[entered]);
						held.Set();
						release.Wait();
					}
					finally
					{
						while (entered-- > 0)
							Monitor.Exit(gates[entered]);
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
	}
}
