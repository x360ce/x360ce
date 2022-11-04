using System.Diagnostics;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.Runtime.InteropServices;

namespace x360ce.App
{
	public partial class Program
	{
		public const string TestTypeName = nameof(TestTypeName);

		static void TestMemoryLeak(Type type)
		{
			var window = new Window();
			window.Topmost = true;
			MessageBox.Show(window, $"Take memory snapshot before object is created.\r\nType = {type.FullName}", "MemoryLeak Test",
				MessageBoxButton.OK, MessageBoxImage.Information);
			var o = Activator.CreateInstance(type);
			MessageBox.Show(window, "Take memory snapshot after control is created", "MemoryLeak Test",
				MessageBoxButton.OK, MessageBoxImage.Information);
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var wr = new WeakReference(o);
			window.Content = o;
			window.Activate();
			Task.Delay(200);
			window.Content = null;
			o = null;
			// Loop untill object allive, but no longer than  seconds.
			while (wr.IsAlive && stopwatch.ElapsedMilliseconds < 5000)
			{
				Task.Delay(100);
				// Try to remove object from the memory.
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
				GC.WaitForFullGCComplete();
			}
			MessageBox.Show(window, $"Take memory snapshot after control is disposed. \r\nIsAlive = {wr.IsAlive}, Elapsed = {stopwatch.ElapsedMilliseconds}ms", "MemoryLeak Test",
				MessageBoxButton.OK, MessageBoxImage.Information);
			window = null;
		}

	}
}
