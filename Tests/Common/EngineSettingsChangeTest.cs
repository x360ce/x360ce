// @under-test: App.v4/Common/DInput/DInputHelper.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs
// @area: engine   @layer: unit
using Microsoft.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// A change of the Windows settings never waits for the engine thread, which reads no window messages.
	/// </summary>
	/// <remarks>
	/// .NET tells each subscriber of a settings change through the context of the thread it subscribed on, and waits
	/// for that thread. A Windows Forms window made on the engine thread subscribed there, so the first change of
	/// colours, which a wallpaper slideshow makes every few minutes when the accent colour follows the wallpaper, held
	/// the interface thread for ever while the engine ran on.
	/// </remarks>
	[TestClass]
	public class EngineSettingsChangeTest
	{
		const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

		/// <summary>The threads the settings-change subscribers are told on, other than through no context.</summary>
		static List<Thread> SubscriberThreads()
		{
			var threads = new List<Thread>();
			var handlers = (IDictionary)typeof(SystemEvents).GetField("_handlers", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			if (handlers == null)
				return threads;
			lock (handlers)
			{
				foreach (IList list in handlers.Values)
					foreach (var info in list)
					{
						var context = info.GetType().GetField("_syncContext", Private).GetValue(info);
						var weak = context?.GetType().GetField("destinationThreadRef", Private)?.GetValue(context) as WeakReference;
						if (weak?.Target is Thread thread)
							threads.Add(thread);
					}
			}
			return threads;
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The engine thread makes no Windows Forms window, so no settings-change subscriber is told through it")]
		public void A_settings_change_never_waits_for_the_engine_thread()
		{
			// Suspended: the thread runs and reads nothing.
			var helper = new DInputHelper { Suspended = true };
			try
			{
				helper.Start();
				// The thread makes its window as it starts.
				Thread.Sleep(500);
				foreach (var thread in SubscriberThreads())
					Assert.AreNotEqual("RefreshAllThread", thread.Name,
						"A settings-change subscriber is told through the engine thread, which reads no messages: the interface thread would wait for it for ever.");
			}
			finally
			{
				helper.Dispose();
			}
		}

		[TestMethod, TestCategory("engine")]
		[Description("The check finds a Windows Forms window made on a thread that reads no messages, as the engine thread made one")]
		public void The_check_finds_a_window_made_on_a_thread_that_reads_no_messages()
		{
			System.Windows.Forms.Form form = null;
			using (var made = new ManualResetEventSlim())
			using (var end = new ManualResetEventSlim())
			{
				var thread = new Thread(() =>
				{
					form = new System.Windows.Forms.Form();
					var handle = form.Handle;
					made.Set();
					end.Wait();
					form.Dispose();
				}) { IsBackground = true, Name = "RefreshAllThread" };
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				made.Wait();
				try
				{
					CollectionAssert.Contains(SubscriberThreads(), thread, "The check misses the window that held the interface thread.");
				}
				finally
				{
					end.Set();
					thread.Join(5000);
				}
			}
		}
	}
}
