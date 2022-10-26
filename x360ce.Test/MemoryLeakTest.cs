using JocysCom.ClassLibrary.Controls;

namespace x360ce.Test
{
	[TestClass]
	public class MemoryLeakTest
	{
		[TestMethod]
		public void TestMethod()
		{
			Console.WriteLine("Please wait...");
			// Convert this to proper tests.
			var success = System.Threading.ThreadPool.QueueUserWorkItem(TestDispose);
			Assert.IsTrue(success);
		}

		#region TestMemoryLeak

		void TestDispose(object? state)
		{
			var text = TestMemoryLeakAssemblies(
					typeof(App.App).Assembly,
					typeof(Engine.EngineHelper).Assembly
				);
			ControlsHelper.Invoke(() =>
			{
			});
		}

		public string TestMemoryLeakAssemblies(params System.Reflection.Assembly[] assemblies)
		{
			var log = new List<string>();
			var disposedCount = 0;
			var aliveCount = 0;
			var errorsCount = 0;
			var e = new ProgressEventArgs();
			for (int a = 0; a < assemblies.Length; a++)
			{
				var assembly = assemblies[a];
				e.TopCount = assemblies.Length;
				e.TopIndex = a;
				e.TopData = assemblies;
				e.TopMessage = $"Assembly: {assembly.FullName}";
				ControlsHelper.Invoke(() => UpdateProgress(e));
				var types = assembly.GetTypes();
				for (int t = 0; t < types.Length; t++)
				{
					var type = types[t];
					e.SubCount = types.Length;
					e.SubIndex = t;
					e.SubData = types;
					e.SubMessage = $"Type: {type.FullName}";
					ControlsHelper.Invoke(() => UpdateProgress(e));
					if (type.IsInterface)
						continue;
					if (!type.FullName!.Contains(".Controls.") && !type.FullName.Contains(".Forms."))
						continue;
					ControlsHelper.Invoke(() =>
					{
						try
						{
							var o = Activator.CreateInstance(type);
							var wr = new WeakReference(o);
							// Verify that the WeakReference actually points to the intended object instance.
							if (wr.Target!.Equals(o))
							{
								// Dispose object.
								o = null;
								for (int i = 0; i < 4; i++)
								{
									GC.Collect();
									GC.WaitForPendingFinalizers();
									GC.WaitForFullGCComplete();
									GC.Collect();
								}
								// Note: Debug mode turns off a lot of optimizations, because compiler is trying to be helpful.
								// Debug build can keep values rooted even if you set them to null i.e. wr.IsAlive will always return TRUE.
								if (wr.IsAlive)
								{
									log.Add($"Is Alive: {type.FullName}");
									aliveCount++;
								}
								else
								{
									log.Add($"Disposed: {type.FullName}");
									disposedCount++;
								}
							}
							else
							{
								log.Add($"Error: NOT same as {type.FullName}");
								errorsCount++;
							}
						}
						catch (Exception ex)
						{
							log.Add($"Error: {type.FullName} {ex.Message}");
							errorsCount++;
						}
					});
				}
			}
			var results = $"Disposed = {disposedCount}, Alive = {aliveCount}, Errors = {errorsCount}\r\n" + string.Join("\r\n", log);
			return results;
		}

		public void UpdateProgress(ProgressEventArgs e)
		{
			// Create top message.
			var tc = e.TopProgressText;
			if (tc == null)
			{
				tc += $"{e.TopIndex}";
				if (e.TopCount > 0)
					tc += $"/{e.TopCount}";
			}
			Console.WriteLine(tc);
			// Create sub message.
			var sc = e.SubProgressText;
			if (sc == null)
			{
				sc += $"{e.SubIndex}";
				if (e.SubCount > 0)
					sc += $"/{e.SubCount}";
			}
			Console.WriteLine(sc);
			Console.WriteLine(e.TopMessage);
			Console.WriteLine(e.SubMessage);
		}

		#endregion

	}
}
