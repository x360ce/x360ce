using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.Controls
{
	public static partial class ControlsHelper
	{
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

		public static bool InvokeRequired
			=> _MainThreadId != Thread.CurrentThread.ManagedThreadId;

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
			if (InvokeRequired)
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
			if (InvokeRequired)
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

		#region Open Path or URL

		public static void OpenUrl(string url)
		{
			try
			{
				System.Diagnostics.Process.Start(url);
			}
			catch (System.ComponentModel.Win32Exception winEx)
			{
				if (winEx.ErrorCode == -2147467259)
					MessageBoxShow(winEx.Message);
			}
			catch (Exception ex)
			{
				MessageBoxShow(ex.Message);
			}
		}

		private static void MessageBoxShow(string message)
		{
#if NETCOREAPP // .NET Core
			System.Windows.Forms.MessageBox.Show(message);
#elif NETSTANDARD // .NET Standard
#elif NETFRAMEWORK // .NET Framework
			// Requires: PresentationFramework.dll
			System.Windows.MessageBox.Show(message);
#else
			throw new NotImplementedException("MessageBox not available for this .NET type");
#endif
		}

		/// <summary>
		/// Open file with associated program.
		/// </summary>
		/// <param name="path">file to open.</param>
		public static void OpenPath(string path, string arguments = null)
		{
			try
			{
				var fi = new System.IO.FileInfo(path);
				// Brings up the "Windows cannot open this file" dialog if association not found.
				var psi = new System.Diagnostics.ProcessStartInfo(path);
				psi.UseShellExecute = true;
				psi.WorkingDirectory = fi.Directory.FullName;
				psi.ErrorDialog = true;
				if (arguments != null)
					psi.Arguments = arguments;
				System.Diagnostics.Process.Start(psi);
			}
			catch (Exception) { }
		}

		#endregion

		public static PropertyInfo GetPrimaryKeyPropertyInfo(object item)
		{
			if (item == null)
				return null;
			var t = item.GetType();
			PropertyInfo pi = null;
#if NETCOREAPP // .NET Core
			// Try to find property by KeyAttribute.
			pi = t.GetProperties()
				.Where(x => Attribute.IsDefined(x, typeof(System.ComponentModel.DataAnnotations.KeyAttribute), true))
				.FirstOrDefault();
			if (pi != null)
				return pi;
#else
			// Try to find property by EntityFramework EdmScalarPropertyAttribute (System.Data.Entity.dll).
			pi = t.GetProperties()
				.Where(x =>
					x.GetCustomAttributes(typeof(System.Data.Objects.DataClasses.EdmScalarPropertyAttribute), true)
					.Cast<System.Data.Objects.DataClasses.EdmScalarPropertyAttribute>()
					.Any(a => a.EntityKeyProperty))
				.FirstOrDefault();
			if (pi != null)
				return pi;

#endif
			return null;
		}

		/// <summary>
		/// Get DataViewRow, DataRow or item property value.
		/// </summary>
		/// <typeparam name="T">Return value type.</typeparam>
		/// <param name="item">DataViewRow, DataRow or another type.</param>
		/// <param name="keyPropertyName">Data property or column name.</param>
		/// <param name="pi">Optional property info cache.</param>
		/// <returns></returns>
		private static T GetValue<T>(object item, string keyPropertyName, PropertyInfo pi = null)
		{
			// Return object value if property info supplied.
			if (pi != null)
				return (T)pi.GetValue(item, null);
			// Get DataRow.
			var row = item is System.Data.DataRowView rowView
				? rowView.Row
				: (System.Data.DataRow)item;
			// Return DataRow value.
			return row.IsNull(keyPropertyName)
					? default
					: (T)row[keyPropertyName];
		}

		/// <summary>
		///  Get Property info 
		/// </summary>
		/// <param name="keyPropertyName"></param>
		/// <param name="item"></param>
		private static PropertyInfo GetPropertyInfo(string keyPropertyName, object item)
		{
			// Get property info if not DataRowView or DataRow.
			PropertyInfo pi = null;
			if (!(item is DataRowView) && !(item is DataRow))
				pi = string.IsNullOrEmpty(keyPropertyName)
					? GetPrimaryKeyPropertyInfo(item)
					: item.GetType().GetProperty(keyPropertyName);
			return pi;
		}

		#region Add cool downs to controls.

		// Default cool-down 1 second.
		public static TimeSpan ControlCooldown = new TimeSpan(0, 0, 1);

		public static Dictionary<object, DateTime> ControlCooldowns { get; } = new Dictionary<object, DateTime>();

		/// <summary>
		/// Returns true if control is on cool-down.
		/// </summary>
		/// <param name="control">Control to check.</param>
		public static bool IsOnCooldown(object control, int? milliseconds = null)
		{
			lock (ControlCooldowns)
			{
				var now = DateTime.Now;
				// Get expired controls.
		        var keys = ControlCooldowns.Where(x => now > x.Value).Select(x => x.Key).ToList();
				// Cleanup the list.
				foreach (var key in keys)
					ControlCooldowns.Remove(key);
				// If on cool-down then...
				if (ControlCooldowns.ContainsKey(control))
					return true;
				var cooldown = milliseconds.HasValue
					? new TimeSpan(0, 0, 0, milliseconds.Value)
					: ControlCooldown;
				ControlCooldowns.Add(control, now.Add(cooldown));
				return false;
			}
		}

		#endregion

	}
}
