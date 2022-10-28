using JocysCom.ClassLibrary.Controls;

namespace x360ce.Test
{
	[TestClass]
	public class MemoryLeakTest
	{

		[TestMethod]
		public void Test_x360ce_Engine()
		{
			TestMemoryLeakAssemblies(
				new[] { typeof(Engine.EngineHelper).Assembly },
				out List<string> disposedTypes,
				out List<string> aliveTypes,
				out List<string> wrongTypes,
				out List<string> errorList);
			var alive = string.Join(", ", aliveTypes);
			if (string.IsNullOrEmpty(alive))
				alive = null;
			var errors = string.Join(", ", errorList);
			if (string.IsNullOrEmpty(errors))
				errors = null;
			// Convert this to proper tests.
			Assert.IsNull(alive);
			Assert.IsNull(errors);
			Assert.IsTrue(disposedTypes.Count > 0);
			Assert.IsTrue(wrongTypes.Count == 0);
		}

		[TestMethod]
		public void Test_x360ce_App()
		{
			TestMemoryLeakAssemblies(
				new[] { typeof(App.App).Assembly },
				out List<string> disposedTypes,
				out List<string> aliveTypes,
				out List<string> wrongTypes,
				out List<string> errorList);
			var alive = string.Join(", ", aliveTypes);
			if (string.IsNullOrEmpty(alive))
				alive = null;
			var errors = string.Join(", ", errorList);
			if (string.IsNullOrEmpty(errors))
				errors = null;
			// Convert this to proper tests.
			Assert.IsNull(alive);
			Assert.IsNull(errors);
			Assert.IsTrue(disposedTypes.Count > 0);
			Assert.IsTrue(wrongTypes.Count == 0);
		}

		#region TestMemoryLeak

		public void TestMemoryLeakAssemblies(
		System.Reflection.Assembly[] assemblies,
		out List<string> disposedTypes,
		out List<string> aliveTypes,
		out List<string> wrongTypes,
		out List<string> errors
	)
		{
			var _disposedTypes = new List<string>();
			var _aliveTypes = new List<string>();
			var _wrongTypes = new List<string>();
			var _errors = new List<string>();
			var e = new ProgressEventArgs();
			for (int a = 0; a < assemblies.Length; a++)
			{
				var assembly = assemblies[a];
				e.TopCount = assemblies.Length;
				e.TopIndex = a;
				e.TopData = assemblies;
				e.TopMessage = $"Assembly: {assembly.FullName}";
				UpdateProgress(e);
				var types = assembly.GetTypes();
				for (int t = 0; t < types.Length; t++)
				{
					var type = types[t];
					e.SubCount = types.Length;
					e.SubIndex = t;
					e.SubData = types;
					e.SubMessage = $"Type: {type.FullName}";
					UpdateProgress(e);
					// Don't test interfaces.
					if (type.IsInterface)
						continue;
					if (!type.FullName!.Contains(".Controls.") && !type.FullName.Contains(".Forms."))
						continue;
					try
					{
						var isDisposed = TestDispose(type);
						// Found different type from expected.
						if (isDisposed == null)
							_wrongTypes.Add(type.FullName);
						else if (isDisposed.Value)
							_disposedTypes.Add(type.FullName);
						else
							_aliveTypes.Add(type.FullName);
					}
					catch (Exception ex)
					{
						_errors.Add($"{type.FullName} {ex.Message}");
					}
				}
			}
			disposedTypes = _disposedTypes;
			aliveTypes = _aliveTypes;
			wrongTypes = _wrongTypes;
			errors = _errors;
		}

		public bool? TestDispose(Type type)
		{
			var o = Activator.CreateInstance(type);
			var wr = new WeakReference(o);
			// If WeakReference don't point to the intended object instance then return null.
			if (!wr.Target!.Equals(o))
				return null;
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
			//
			// Return true if object is not allive, that is, disposed.
			return !wr.IsAlive;
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
