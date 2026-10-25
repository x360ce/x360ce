using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace x360ce.TestGame
{
	/// <summary>
	/// Reads XInput and DirectInput on a thread of its own, every few milliseconds, as a game's input thread does, and hands
	/// the window what changed, each event with the time it was read. Drawing the window or answering the AI door never
	/// delays a reading, so the times say when the input happened.
	/// </summary>
	public sealed class DevicePoller : IDisposable
	{
		const int EveryMs = 4;

		public readonly XInputReader XInput = new XInputReader();
		public readonly DirectInputReader DirectInput = new DirectInputReader();

		/// <summary>Taken while reading, and by whoever looks at the readers' last values from another thread.</summary>
		public readonly object Sync = new object();

		readonly ConcurrentQueue<InputEvent> _events = new ConcurrentQueue<InputEvent>();
		readonly Func<long> _now;
		readonly Thread _thread;
		volatile bool _stop;

		/// <summary>Whether the window is in front, as the window last said.</summary>
		public volatile bool InFront;

		public DevicePoller(IntPtr window, Func<long> now)
		{
			_now = now;
			_thread = new Thread(() => Run(window)) { IsBackground = true, Name = "Input poller" };
			_thread.Start();
		}

		[System.Runtime.InteropServices.DllImport("winmm.dll")]
		static extern uint timeBeginPeriod(uint ms);

		[System.Runtime.InteropServices.DllImport("winmm.dll")]
		static extern uint timeEndPeriod(uint ms);

		void Run(IntPtr window)
		{
			// The system's timer at 1 ms while the game runs, as games set it, or a sleep of 4 ms lasts 15.
			timeBeginPeriod(1);
			try
			{
				Poll(window);
			}
			finally
			{
				timeEndPeriod(1);
			}
		}

		void Poll(IntPtr window)
		{
			// Made on this thread, where it is read and let go of.
			DirectInput.Open(window);
			while (!_stop)
			{
				List<InputEvent> found;
				lock (Sync)
				{
					found = XInput.Poll(_now(), InFront);
					found.AddRange(DirectInput.Poll(_now, InFront));
				}
				foreach (var e in found)
					_events.Enqueue(e);
				Thread.Sleep(EveryMs);
			}
			lock (Sync)
				DirectInput.Dispose();
		}

		/// <summary>What was read since the last call, oldest first.</summary>
		public List<InputEvent> Take()
		{
			var list = new List<InputEvent>();
			InputEvent e;
			while (_events.TryDequeue(out e))
				list.Add(e);
			return list;
		}

		public void Dispose()
		{
			_stop = true;
			_thread.Join(1000);
		}
	}
}
