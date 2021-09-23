using System;
using System.Collections.Generic;
using System.Windows;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			//TestMemoryLeakAssembly(
			//	typeof(App).Assembly,
			//	typeof(Engine.EngineHelper).Assembly
			//);
		}

		/// <summary>
		/// Get shared resource.
		/// </summary>
		public static object GetResource(string name)
			=> Current.Resources[name];

		/*

		public static void TestMemoryLeakAssembly(params System.Reflection.Assembly[] args)
		{
			var errors = new List<string>();
			foreach (var assmebly in args)
			{
				var types = assmebly.GetTypes();
				foreach (var type in types)
				{
					if (!type.FullName.Contains(".Controls.") && !type.FullName.Contains(".Forms."))
						continue;
					string error = null;
					try
					{
						error = TestMemoryLeak(type);
						if (!string.IsNullOrEmpty(error))
							errors.Add($"{type.FullName} {error}");
					}
					catch (Exception ex)
					{
						errors.Add($"{type.FullName} {ex.Message}");
					}
				}
			}

		}
		public static string TestMemoryLeak(Type type)
		{
			var o = Activator.CreateInstance(type);
			WeakReference wr = new WeakReference(o);
			// Verify that the WeakReference actually points to the intended object instance.
			if (!wr.Target.Equals(o))
				return "not same";
			// Dispose object.
			o = default;
			var weakRef = new WeakReference(o);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.WaitForFullGCComplete();
			GC.Collect();
			if (weakRef.IsAlive)
				return "Is Alive";
			return null;
		}

		*/


	}
}
