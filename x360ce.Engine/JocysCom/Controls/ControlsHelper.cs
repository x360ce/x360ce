using System;
using System.Threading;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.Controls
{
	public static partial class ControlsHelper
	{
		private const int WM_SETREDRAW = 0x000B;

		#region Invoke and BeginInvoke

		/// <summary>
		/// Call this method from main form constructor for BeginInvoke to work.
		/// </summary>
		public static void InitInvokeContext()
		{
			if (MainTaskScheduler != null)
				return;
			_MainThreadId = Thread.CurrentThread.ManagedThreadId;
			// Create a TaskScheduler that wraps the SynchronizationContext returned from
			// System.Threading.SynchronizationContext.Current
			MainTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		}

		/// <summary>
		/// Object that handles the low-level work of queuing tasks onto main User Interface (GUI) thread.
		/// </summary>
		public static TaskScheduler MainTaskScheduler { get; private set; }

		public static int MainThreadId => _MainThreadId;
		private static int _MainThreadId;

		public static bool InvokeRequired()
		{
			return _MainThreadId != Thread.CurrentThread.ManagedThreadId;
		}

		/*

		public static void TestTasks(TaskCreationOptions childOptions)
		{
			var i = 5000000;
			Console.WriteLine("//");
			Console.WriteLine("TestTasks(TaskCreationOptions.{0});", childOptions);
			Console.WriteLine("// Parent starting");
			var parent = Task.Factory.StartNew(() =>
			{
				Console.WriteLine("// Parent started");
				Console.WriteLine("// Child starting");
				var child = Task.Factory.StartNew(() =>
				{
					Console.WriteLine("// Child started");
					Thread.SpinWait(i);
					Console.WriteLine("// Child completing");
				}, childOptions);
				//child.Wait();
				//Console.WriteLine("// Child completed");
				Console.WriteLine("// Parent completing");
			});
			parent.Wait();
			Console.WriteLine("// Parent completed");
			Thread.SpinWait(i * 4);
		}
		
		// Attached and Detached Child Tasks.
		//
		// TaskCreationOptions.AttachedToParent:
		//
		//     - Parent task waits for child tasks to complete.
		//     - Parent task propagates exceptions thrown by child tasks.
		//     - Status of parent task depends on status of child task.
	    //
		TestTasks(TaskCreationOptions.AttachedToParent);
		//
		// Parent starting
		// Parent started
		// Child starting
		// Parent completing
		// Child started
		// Child completing
		// Parent completed
		//
		TestTasks(TaskCreationOptions.None);
		//
		// Parent starting
		// Parent started
		// Child starting
		// Parent completing
		// Parent completed
		// Child started
		// Child completing

		*/

		/// <summary>Executes the specified action delegate asynchronously on main Graphical User Interface (GUI) Thread.</summary>
		/// <param name="action">The action delegate to execute asynchronously.</param>
		/// <returns>The started System.Threading.Tasks.Task.</returns>
		public static Task BeginInvoke(Action action, int? millisecondsDelay = null)
		{
			if (millisecondsDelay.HasValue)
			{
				return Task.Run(async () =>
				{
					// Wait 1 second, which will allow to release the button.
					await Task.Delay(millisecondsDelay.Value).ConfigureAwait(true);
					await BeginInvoke(action);
				});
			}
			InitInvokeContext();
			return Task.Factory.StartNew(action,
				CancellationToken.None, TaskCreationOptions.DenyChildAttach, MainTaskScheduler);
		}

		/// <summary>Executes the specified action delegate asynchronously on main User Interface (UI) Thread.</summary>
		/// <param name="action">The action delegate to execute asynchronously.</param>
		/// <returns>The started System.Threading.Tasks.Task.</returns>
		public static Task BeginInvoke(Delegate method, params object[] args)
		{
			InitInvokeContext();
			return Task.Factory.StartNew(() => { method.DynamicInvoke(args); },
				CancellationToken.None, TaskCreationOptions.DenyChildAttach, MainTaskScheduler);
		}

		/// <summary>Executes the specified action delegate synchronously on main Graphical User Interface (GUI) Thread.</summary>
		/// <param name="action">The action delegate to execute synchronously.</param>
		public static void Invoke(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			InitInvokeContext();
			if (InvokeRequired())
			{
				var t = new Task(action);
				t.RunSynchronously(MainTaskScheduler);
			}
			else
			{
				action.DynamicInvoke();
			}
		}

		/// <summary>Executes the specified action delegate synchronously on main Graphical User Interface (GUI) Thread.</summary>
		/// <param name="action">The delegate to execute synchronously.</param>
		public static object Invoke(Delegate method, params object[] args)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			// Run method on main Graphical User Interface thread.
			if (InvokeRequired())
			{
				var t = new Task<object>(() => method.DynamicInvoke(args));
				t.RunSynchronously(MainTaskScheduler);
				return t.Result;
			}
			else
			{
				return method.DynamicInvoke(args);
			}
		}

		#endregion

	}
}
