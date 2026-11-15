// @under-test: App.v4/Common/Recording/ReportRecorder.Stall.cs
// @area: recording   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using x360ce.App.Recording;

namespace x360ce.Tests
{
	/// <summary>
	/// When the window stops answering, a recording says which posted call the interface thread is in and what Windows sees
	/// it wait on; every ten seconds it says how the PC is doing. All of it is code names and numbers.
	/// </summary>
	[TestClass]
	public class ReportRecorderStallTest
	{
		static T Call<T>(string name, params object[] args)
		{
			var method = typeof(ReportRecorder).GetMethod(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			return (T)method.Invoke(null, args);
		}

		static void Marker()
		{
		}

		[TestMethod, TestCategory("recording"), TestCategory("critical")]
		[Description("A call posted through the posting helper is named by the method it runs, not by the helper's wrapper")]
		public void A_posted_call_is_named_by_the_method_it_runs()
		{
			Action action = Marker;
			Assert.AreEqual(nameof(ReportRecorderStallTest) + "." + nameof(Marker), Call<string>("WorkName", action));
			// As ControlsHelper.BeginInvoke posts it: a task whose action is the helper's lambda around the call.
			var task = new Task(() => action());
			Assert.AreEqual(nameof(ReportRecorderStallTest) + "." + nameof(A_posted_call_is_named_by_the_method_it_runs) + " (a lambda in it)",
				Call<string>("WorkName", task), "A task is not named by the method its action was written in.");
			Assert.IsNull(Call<string>("WorkName", new object[] { null }), "Between calls, a call is named.");
		}

		[TestMethod, TestCategory("recording"), TestCategory("critical")]
		[Description("A thread waiting for a .NET lock another thread holds shows as blocked on nothing Windows can name")]
		public void A_thread_waiting_for_a_lock_shows_as_blocked()
		{
			var gate = new object();
			var nativeId = 0;
			using (var held = new ManualResetEventSlim())
			using (var waiting = new ManualResetEventSlim())
			{
				var holder = new Thread(() => { lock (gate) { held.Set(); waiting.Wait(5000); Thread.Sleep(1000); } }) { IsBackground = true };
				holder.Start();
				held.Wait();
				var waiter = new Thread(() =>
				{
					nativeId = GetCurrentThreadId();
					waiting.Set();
					lock (gate) { }
				}) { IsBackground = true };
				waiter.Start();
				waiting.Wait();
				// Past the lock's spinning, into its wait.
				Thread.Sleep(300);
				var chain = Call<string>("WaitChain", nativeId);
				StringAssert.Contains(chain, "blocked", "A thread waiting for a lock is not said to be blocked.");
				waiter.Join();
				holder.Join();
			}
		}

		[TestMethod, TestCategory("recording")]
		[Description("The PC line holds memory, processors, .NET clean-ups, power and device changes, and nothing else")]
		public void The_PC_line_holds_numbers_only()
		{
			var recorder = (ReportRecorder)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ReportRecorder));
			typeof(ReportRecorder).GetField("_pcCollections", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(recorder, new int[3]);
			typeof(ReportRecorder).GetField("_pcIdle", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(recorder, -1L);
			var describe = typeof(ReportRecorder).GetMethod("DescribePc", BindingFlags.Instance | BindingFlags.NonPublic);
			using (var process = Process.GetCurrentProcess())
			{
				describe.Invoke(recorder, new object[] { process });
				Thread.Sleep(200);
				var line = (string)describe.Invoke(recorder, new object[] { process });
				StringAssert.StartsWith(line, "PC: memory ");
				StringAssert.Contains(line, "processors ");
				StringAssert.Contains(line, ".NET clean-ups ");
				StringAssert.Contains(line, " device changes");
				StringAssert.DoesNotMatch(line, new System.Text.RegularExpressions.Regex(Environment.UserName + "|" + Environment.MachineName,
					System.Text.RegularExpressions.RegexOptions.IgnoreCase), "The PC line names the person or the PC.");
			}
		}

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern int GetCurrentThreadId();
	}
}
