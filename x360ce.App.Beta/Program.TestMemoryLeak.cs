using System.Diagnostics;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.Reflection;

namespace x360ce.App
{
	public partial class Program
	{
		public const string TestTypeName = nameof(TestTypeName);

		static void TestMemoryLeak(Type type)
		{
			MessageBox.Show("MemoryLeak Test", $"Take memory snapshot before '{type.FullName}' is created",
				MessageBoxButton.OK, MessageBoxImage.Information);
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var o = Activator.CreateInstance(type);
			MessageBox.Show("MemoryLeak Test", "Take memory snapshot after control is created",
				MessageBoxButton.OK, MessageBoxImage.Information);
			var wr = new WeakReference(o);
			var window = new System.Windows.Window();
			window.Content = o;
			window.Activate();
			window.Content = null;
			window = null;
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
			MessageBox.Show("MemoryLeak Test", "Take memory snapshot after control is disposed. IsAlive = {wr.IsAlive}",
				MessageBoxButton.OK, MessageBoxImage.Information);
		}

	}
}
