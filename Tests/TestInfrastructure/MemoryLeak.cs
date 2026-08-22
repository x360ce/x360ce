using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// Proves an object was actually released rather than merely closed.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: this only works in an OPTIMISED build. Without optimisation the compiler
	/// keeps locals rooted for the debugger even after they are set to null, so a weak
	/// reference reports IsAlive forever and every leak test passes regardless of the truth.
	/// The test project therefore sets Optimize=true in every configuration. Technique taken
	/// from the 5.x MemoryLeakHelper, which carries the same warning.
	/// </remarks>
	public static class MemoryLeak
	{

		/// <summary>True when this assembly was built without optimisation.</summary>
		/// <remarks>
		/// Read from the attribute rather than a #if, so it reports what was actually built.
		/// </remarks>
		public static bool IsUnoptimised
		{
			get
			{
				var attribute = (System.Diagnostics.DebuggableAttribute)Attribute.GetCustomAttribute(
					typeof(MemoryLeak).Assembly, typeof(System.Diagnostics.DebuggableAttribute));
				return attribute != null && attribute.IsJITOptimizerDisabled;
			}
		}

		/// <summary>Full blocking collection, including the large object heap.</summary>
		public static void CollectGarbage()
		{
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
			GC.WaitForFullGCComplete();
			GC.WaitForPendingFinalizers();
			// A finalizer can resurrect work onto the next collection, so collect once more.
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
		}

		/// <summary>
		/// Collect until the reference dies or the timeout expires.
		/// </summary>
		/// <returns>True when the object was released.</returns>
		public static bool WasReleased(WeakReference reference, TimeSpan timeout)
		{
			if (reference == null)
				throw new ArgumentNullException(nameof(reference));
			var watch = Stopwatch.StartNew();
			while (watch.Elapsed < timeout)
			{
				CollectGarbage();
				if (!reference.IsAlive)
					return true;
				Thread.Sleep(100);
			}
			return !reference.IsAlive;
		}

		/// <summary>
		/// Build the object, use it, drop it, and report whether anything still holds it.
		/// </summary>
		/// <remarks>
		/// The object never enters a local of the calling frame, which is what makes the
		/// result trustworthy: a local would keep it rooted for the lifetime of the method.
		/// </remarks>
		public static bool CreateUseAndRelease<T>(Func<T> create, Action<T> use, TimeSpan timeout) where T : class
		{
			var reference = Build(create, use);
			return WasReleased(reference, timeout);
		}

		// Kept separate and non-inlined so the instance is unreachable once it returns.
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private static WeakReference Build<T>(Func<T> create, Action<T> use) where T : class
		{
			var instance = create();
			use?.Invoke(instance);
			return new WeakReference(instance);
		}

	}
}
