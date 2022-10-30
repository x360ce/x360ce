using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

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
		public void Test_x360ce_Engine() =>
			Test(typeof(Engine.EngineHelper).Assembly);

		[TestMethod]
		public void Test_x360ce_App() =>
			Test(typeof(App.App).Assembly);

		[TestMethod]
		public void Test_x360ce_App_PadItem_AdvancedControl() =>
			Test<App.Controls.PadItem_AdvancedControl>();

		private void Test<T>()
		{
			Test(typeof(T).Assembly, typeof(T));
		}

		public void Test(Assembly assembly, params Type[] includeTypes)
		{
			var results = TestMemoryLeakByAssembly(assembly, includeTypes);
			var errors = results.Where(x => x.Level == TraceLevel.Error).ToList();
			var warnings = results.Where(x => x.Level == TraceLevel.Warning).ToList();
			var infoPass = results.Where(x => x.Level == TraceLevel.Info && !x.IsAlive).ToList();
			var infoFail = results.Where(x => x.Level == TraceLevel.Info && x.IsAlive).ToList();
			Console.WriteLine();
			Console.WriteLine($"Disposed: {infoPass.Count}, Dispose Failed: {infoFail.Count}");
			Console.WriteLine($"Warnings: {warnings.Count}, Dispose Errors: {errors.Count}");
			// Recommend fixing the smallest control next, because
			// more likely that it does not contain other controls, but is used by other controls.
			if (includeTypes.Length > 1)
			{
				var nextToFix = infoFail.OrderBy(x => x.MemObjectSize).FirstOrDefault();
				if (nextToFix != null)
				{
					Console.WriteLine();
					Console.WriteLine($"Smallest recommended control to fix: {nextToFix.Type.FullName}");
				}
			}
			Assert.IsTrue(infoPass.Count > 0);
			Assert.IsTrue(infoFail.Count == 0);
			Assert.IsTrue(warnings.Count == 0);
			Assert.IsTrue(errors.Count == 0);
		}

		#region TestMemoryLeak

		public List<MemTestResult> TestMemoryLeakByAssembly(Assembly assembly, params Type[] includeTypes)
		{
			var results = new List<MemTestResult>();
			// Test public non-abstracts classes only.
			var types = assembly.GetTypes()
				.Where(x => includeTypes.Length == 0 || includeTypes.Contains(x))
				.Where(x => x.IsClass)
				.Where(x => x.IsPublic)
				.Where(x => !x.IsAbstract)
				.ToArray();
			var zeros = new string('0', types.Length.ToString().Length);
			var pad = "\r\n" + new string(' ', $"[----] {zeros}/{zeros}: ".Length);
			for (int t = 0; t < types.Length; t++)
			{
				var type = types[t];
				var result = TestType(type);
				results.Add(result);
				var isSuccess = !result.IsAlive && result.Level == TraceLevel.Info;
				// If object was ddisposed without errors then continue
				if (isSuccess)
					continue;
				var status = isSuccess ? "[Pass]" : "[" + result.Level.ToString().Substring(0, 4) + "]";
				var index = string.Format("{0:" + zeros + "}/{1:" + zeros + "}", t + 1, types.Length);
				// Create main message.
				var message = $"{status} {index}: {type.FullName}";
				// Create extra messages lines.
				message += $"{pad}{result.Message}";
				if (result.MemObjectSize.HasValue || result.MemDifference.HasValue)
					message += $"{pad}Object Size: {result.MemObjectSize:#,##0}, Memory Difference: {result.MemDifference:+#,##0;-#,##0;#,##0}";
				Debug.WriteLine(message);
				//Console.WriteLine(message);

			}
			return results;
		}

		public static void CollectGarbage()
		{
			// Try to remove object from the memory.
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			GC.WaitForFullGCComplete();
		}

		private MemTestResult TestType(Type type)
		{
			var result = new MemTestResult();
			result.Type = type;
			var constructor = type.GetConstructor(Type.EmptyTypes);
			if (type.ContainsGenericParameters)
			{
				result.Level = TraceLevel.Warning;
				result.Message += "Requires generic parameters to create instance";
			}
			else if (constructor == null)
			{
				result.Level = TraceLevel.Warning;
				result.Message += "Requires parameters to create instance";
			}
			else
			{
				CollectGarbage();
				try
				{
					var memBeforeCreate = GC.GetTotalMemory(false);
					var o = Activator.CreateInstance(type);
					result.MemObjectSize = GC.GetTotalMemory(false) - memBeforeCreate;
					var wr = new WeakReference(o);
					// If WeakReference point to the intended object instance then...
					if (!wr.Target.Equals(o))
					{
						result.Level = TraceLevel.Error;
						result.Message += "Wrong Type!";
					}
					var lu = o as Engine.ILoadUnload;
					if (lu != null)
					{
						lu.Load();
						lu.Unload();
					}
					// Trigger object dispose.
					lu = null;
					o = null;
					// Cleanup memory.
					for (int i = 0; i < 4; i++)
					{
						Task.Delay(100);
						CollectGarbage();
					}
					result.MemDifference = GC.GetTotalMemory(true) - memBeforeCreate;
					result.IsAlive = wr.IsAlive;
					// if dispose failed. Strong references are left to the wr.Target.
					result.Message += wr.IsAlive
						? "Dispose Failed!"
						: "Disposed";
				}
				catch (Exception ex)
				{
					result.Level = TraceLevel.Error;
					result.Message = ex.Message;
					result.Exception = ex;
				}
			}
			return result;
		}

		#endregion

	}
}
