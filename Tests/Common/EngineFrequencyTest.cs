// @under-test: Engine/JocysCom/ComponentModel/BindingListInvoked.cs, App.v4/Common/SettingsManager.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// The engine runs on its own thread with its own window so that it does not depend on the
	/// interface. That separation is only real if the engine keeps its rate while the interface
	/// is busy. It stops being real the moment the engine waits on the interface thread, which
	/// is what the settings collections do: their changes are marshalled there and the engine
	/// writes to them on every cycle.
	///
	/// These tests measure engine cycles per second and fail when the interface can slow it
	/// down. A change that made every read and every change share one lock dropped the
	/// application from 1000 Hz to 1 Hz and shipped, because nothing measured it.
	///
	/// The engine is allowed to pause only while devices are added or removed. Nothing here
	/// adds or removes a device.
	/// </summary>
	[TestClass]
	public class EngineFrequencyTest
	{
		/// <summary>Plain item. The collection places no constraint on what it holds.</summary>
		public class Row { public Guid Id { get; set; } }

		/// <summary>Engine cycles per second below which the engine is considered stalled.</summary>
		/// <remarks>
		/// The application asks for 1000 Hz. This floor is deliberately far below that, so the
		/// test reports a collapse rather than ordinary variation between machines.
		/// </remarks>
		const int MinimumHz = 200;

		/// <summary>
		/// How much of its idle rate the engine must keep while the interface is busy.
		/// </summary>
		/// <remarks>
		/// Set well clear of both outcomes rather than close to either. Waiting on the interface
		/// measured around 1 per cent here; running beside it measures around 70. Two threads
		/// sharing a processor cost each other something, so the bar is not 100.
		/// </remarks>
		const double MinimumRatio = 0.33;

		const int MeasureMs = 2000;

		[TestMethod, TestCategory("engine"), TestCategory("smoke")]
		[Description("The engine keeps its rate while the interface thread is busy")]
		public void Engine_rate_survives_interface_work()
		{
			MeasureAgainstBusyInterface(false);
		}

		/// <summary>
		/// The same measurement over the path the device list actually uses.
		/// </summary>
		/// <remarks>
		/// SettingsManager sets AsynchronousInvoke on UserDevices.Items, so its changes are
		/// handed to the interface thread rather than waited on. That is a different branch of
		/// the collection, and for a long time nothing measured it. A change that made the
		/// engine take the interface's lock to read one item out of the list before handing the
		/// change over dropped the application to single-digit cycles a second and passed the
		/// whole suite, because every test used the other branch.
		/// </remarks>
		[TestMethod, TestCategory("engine"), TestCategory("smoke")]
		[Description("The engine keeps its rate when its changes are handed over rather than waited on")]
		public void Engine_rate_survives_interface_work_when_changes_are_handed_over()
		{
			MeasureAgainstBusyInterface(true);
		}

		static void MeasureAgainstBusyInterface(bool handOver)
		{
			using (var ui = new InterfaceThread())
			{
				var data = new JocysCom.ClassLibrary.Configuration.SettingsData<Row>();
				for (int i = 0; i < 64; i++)
					data.Items.Add(new Row { Id = Guid.NewGuid() });
				// Changes go to the interface thread, exactly as SettingsManager sets them up.
				data.Items.SynchronizingObject = ui.Scheduler;
				data.Items.AsynchronousInvoke = handOver;

				Cycles(data, 250);                       // warm up, ignore the result
				var idle = Cycles(data, MeasureMs);
				ui.KeepBusy = true;
				var busy = Cycles(data, MeasureMs);
				ui.KeepBusy = false;

				Console.WriteLine("changes {0}: engine idle {1} Hz, interface busy {2} Hz ({3:P0} kept)",
					handOver ? "handed over" : "waited on", idle, busy,
					idle == 0 ? 0 : (double)busy / idle);
				Assert.IsTrue(idle >= MinimumHz,
					"Engine ran at " + idle + " Hz with the interface idle, below the " + MinimumHz
					+ " Hz floor. The engine is waiting on something it should not.");
				Assert.IsTrue(busy >= MinimumHz,
					"Engine ran at " + busy + " Hz while the interface was busy, below the " + MinimumHz
					+ " Hz floor. Interface work must not slow the engine.");
				Assert.IsTrue(busy >= idle * MinimumRatio,
					"Engine kept only " + (idle == 0 ? 0 : busy * 100 / idle) + "% of its rate when the "
					+ "interface got busy, " + idle + " Hz down to " + busy + " Hz. The engine is waiting "
					+ "on the interface instead of running beside it.");
			}
		}

		[TestMethod, TestCategory("engine")]
		[Description("The engine keeps its rate while another thread reads the same collections")]
		public void Engine_rate_survives_concurrent_readers()
		{
			using (var ui = new InterfaceThread())
			{
				var data = new JocysCom.ClassLibrary.Configuration.SettingsData<Row>();
				for (int i = 0; i < 64; i++)
					data.Items.Add(new Row { Id = Guid.NewGuid() });
				data.Items.SynchronizingObject = ui.Scheduler;

				var stop = false;
				var reader = new Thread(() =>
				{
					while (!Volatile.Read(ref stop))
						GC.KeepAlive(data.ItemsToArraySyncronized());
				});
				reader.IsBackground = true;
				Cycles(data, 250);
				var alone = Cycles(data, MeasureMs);
				reader.Start();
				var shared = Cycles(data, MeasureMs);
				Volatile.Write(ref stop, true);
				reader.Join(TimeSpan.FromSeconds(5));

				Console.WriteLine("engine alone {0} Hz, with a reader {1} Hz", alone, shared);
				Assert.IsTrue(shared >= MinimumHz,
					"Engine ran at " + shared + " Hz while another thread read the same collection, "
					+ "below the " + MinimumHz + " Hz floor.");
			}
		}

		/// <summary>
		/// Runs the shape of one engine cycle for the given time and returns cycles per second.
		/// A cycle reads the collection and changes an item, which is what the device loop does.
		/// </summary>
		static int Cycles(JocysCom.ClassLibrary.Configuration.SettingsData<Row> data, int milliseconds)
		{
			var count = 0;
			var watch = Stopwatch.StartNew();
			while (watch.ElapsedMilliseconds < milliseconds)
			{
				GC.KeepAlive(data.ItemsToArraySyncronized());
				// Assigning through the indexer raises the change the interface listens for,
				// which is the step that reaches the interface thread.
				data.Items[0] = data.Items[0];
				count++;
			}
			watch.Stop();
			return (int)(count * 1000L / Math.Max(1, watch.ElapsedMilliseconds));
		}

		/// <summary>
		/// A stand-in for the interface thread: a real message loop with a task scheduler, which
		/// is what SettingsManager hands the collections. It can be told to stay busy so the test
		/// can see whether the engine is waiting on it.
		/// </summary>
		sealed class InterfaceThread : IDisposable
		{
			public TaskScheduler Scheduler { get; private set; }
			public volatile bool KeepBusy;

			readonly Thread _thread;
			Form _form;
			readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);

			public InterfaceThread()
			{
				_thread = new Thread(Run);
				_thread.IsBackground = true;
				_thread.SetApartmentState(ApartmentState.STA);
				_thread.Start();
				if (!_ready.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("The interface thread did not start.");
			}

			void Run()
			{
				_form = new Form { ShowInTaskbar = false, WindowState = FormWindowState.Minimized };
				var handle = _form.Handle;   // force the window and its message loop to exist
				GC.KeepAlive(handle);
				Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
				var busyTimer = new System.Windows.Forms.Timer { Interval = 1 };
				// Occupying the thread is what a redrawing interface does to it.
				busyTimer.Tick += (s, e) => { if (KeepBusy) Thread.Sleep(1); };
				busyTimer.Start();
				_ready.Set();
				Application.Run(_form);
			}

			public void Dispose()
			{
				try
				{
					if (_form != null && _form.IsHandleCreated)
						_form.BeginInvoke((MethodInvoker)(() => _form.Close()));
					_thread.Join(TimeSpan.FromSeconds(5));
				}
				catch { }
				_ready.Dispose();
			}
		}
	}
}
