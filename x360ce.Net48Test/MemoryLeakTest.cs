using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime;

namespace x360ce.Net48Test
{

	/// <summary>
	/// Test if the controls can be disposed and garbage collected and don't result in memory leaks.
	/// IMPORTANT!!!: Enable "Code Optimize" option for memory leak (dispose) test to work:
	///		NET 4.8: Build \ Configuration: Debug \ [x] Optimise Code".
	///		NET 6.0: Build \ General \ Optimize Code: [x] Debug
	/// Note: Compiler is trying to be helpful and Debug build can keep values rooted even if
	/// you set them to null i.e. 'wr.IsAlive' will always return 'true'.
	/// </summary>
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
			for (int a = 0; a < assemblies.Length; a++)
			{
				var assembly = assemblies[a];
				var types = assembly.GetTypes()
					.Where(x => x.IsClass)
					.Where(x => x.IsPublic)
					.Where(x => !x.IsAbstract)
					.ToArray();
				for (int t = 0; t < types.Length; t++)
				{
					var type = types[t];
					var message = "";
					var status = "[----]";
					var pad = "\r\n" + new string(' ', "[----] 000/000: ".Length);
					var constructor = type.GetConstructor(Type.EmptyTypes);
					if (type.ContainsGenericParameters)
					{
						status = "[Warn]";
						_errors.Add($"{type.FullName} ContainsGenericParameters");
						message += $"{pad}Requires generic parameters to create instance";
					}
					else if (constructor == null)
					{
						status = "[Warn]";
						_errors.Add($"{type.FullName} Requires parameters to create instance");
						message += $"{pad}Requires parameters to create instance";
					}
					else
					{
						CollectGarbage();
						try
						{
							var writeSize = true;
							//=======================================================
							// Dispose test
							//-------------------------------------------------------
							var memBeforeCreate = GC.GetTotalMemory(false);
							var o = Activator.CreateInstance(type);
							var memAfterCreate = GC.GetTotalMemory(false);
							var objectSize = memAfterCreate - memBeforeCreate;
							var wr = new WeakReference(o);
							// If WeakReference point to the intended object instance then...
							if (!wr.Target.Equals(o))
							{
								message += $"{pad}Result: Wrong Type!";
								status = "[Fail]";
								_wrongTypes.Add(type.FullName);
							}
							o = null;
							for (int i = 0; i < 4; i++)
								CollectGarbage();
							var memAfterDispose = GC.GetTotalMemory(true);
							var memDifference = memAfterDispose - memBeforeCreate;
							// wr.IsAlive is 'true' then...
							if (wr.IsAlive)
							{
								// Dispose failed. Strong references are left to the wr.Target.
								message += $"{pad}Result: Failed to dispose!";
								status = "[!!!!]";
								_aliveTypes.Add(type.FullName);
							}
							else
							{
								// Dispose was success. No strong references are left to the wr.Target.
								status = "[ OK ]";
								_disposedTypes.Add(type.FullName);
								writeSize = false;
							}
							if (writeSize)
								message += $"{pad}Object Size: {objectSize:#,##0}, Memory Difference {memDifference:+#,##0;-#,##0}";

						}
						catch (Exception ex)
						{
							status = "[Fail]";
							_errors.Add($"{type.FullName} {ex.Message}");
							message += $"{pad}Error: {type.FullName} {ex.Message}";
						}
					}
					if (status != "[ OK ]")
						Console.WriteLine($"{status} {t + 1:000}/{types.Length}: {type.FullName} {message}");
				}
			}
			disposedTypes = _disposedTypes;
			aliveTypes = _aliveTypes;
			wrongTypes = _wrongTypes;
			errors = _errors;
		}

		public static void CollectGarbage()
		{
			// Try to remove object from the memory.
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			var status = GC.WaitForFullGCComplete();
		}

		#endregion

	}
}
