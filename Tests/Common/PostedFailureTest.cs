// @under-test: Engine/JocysCom/Controls/ControlsHelper.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Most work reaches the interface thread by being posted to it, and almost nothing waits
	/// for the result. A failure in posted work used to be stored on a task nobody waited for.
	/// It then surfaced whenever the finalizer noticed, wrapped in a second exception, with the
	/// original stack gone and long after whatever caused it.
	///
	/// A crash report built from that says a task failed somewhere. This test holds the failure
	/// to the moment and the place it happened, which is what makes a report worth sending.
	/// </summary>
	[TestClass]
	public class PostedFailureTest
	{
		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("A failure in posted work is reported at once, with its own stack")]
		public void Posted_failure_is_reported_with_its_stack()
		{
			using (var ui = new InterfaceThread())
			{
				ControlsHelper.BeginInvoke(new Action(Fail));

				Assert.IsTrue(ui.Reported.Wait(TimeSpan.FromSeconds(10)),
					"Nothing was reported. The failure is sitting on a task nobody waits for, so "
					+ "it will surface late, wrapped, and with no stack - if at all.");
				Assert.IsInstanceOfType(ui.Failure, typeof(InvalidOperationException),
					"Reported " + ui.Failure?.GetType().Name + " instead of what actually failed. "
					+ "A wrapper hides the failure from anyone reading the report.");
				StringAssert.Contains(ui.Failure.StackTrace ?? "", "PostedFailureTest.cs",
					"The report does not name the file that failed, so it cannot be acted on.");
			}
		}

		/// <summary>Fails the way posted work fails: on the interface thread, with nobody waiting.</summary>
		static void Fail()
		{
			throw new InvalidOperationException("Deliberate failure in posted work");
		}

		/// <summary>
		/// Unbinds the helper from whichever thread claimed it, so a test can bind it to its own.
		/// </summary>
		static void ReleaseInvokeContext()
		{
			typeof(ControlsHelper)
				.GetProperty("MainTaskScheduler")
				.SetValue(null, null, null);
		}

		/// <summary>A real interface thread: a message loop, its scheduler, and its error handler.</summary>
		sealed class InterfaceThread : IDisposable
		{
			public readonly ManualResetEventSlim Reported = new ManualResetEventSlim(false);
			public Exception Failure;

			readonly Thread _thread;
			Form _form;
			readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);

			public InterfaceThread()
			{
				_thread = new Thread(Run) { IsBackground = true };
				_thread.SetApartmentState(ApartmentState.STA);
				_thread.Start();
				if (!_ready.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("The interface thread did not start.");
			}

			void Run()
			{
				_form = new Form { ShowInTaskbar = false, WindowState = FormWindowState.Minimized };
				GC.KeepAlive(_form.Handle);
				// The helper binds to the first thread that asks and keeps that binding for the
				// life of the process. In the application there is one interface thread, so that
				// is correct; in a test run an earlier test may already have bound it to a thread
				// that has since ended. Cleared so it binds to this one.
				ReleaseInvokeContext();
				ControlsHelper.InitInvokeContext();
				Application.ThreadException += (s, e) =>
				{
					Failure = e.Exception;
					Reported.Set();
				};
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
				catch (InvalidOperationException) { }
				// Left unbound so the next test to need it binds to its own thread rather than
				// to this one, which is now gone.
				ReleaseInvokeContext();
				_ready.Dispose();
				Reported.Dispose();
			}
		}
	}
}
