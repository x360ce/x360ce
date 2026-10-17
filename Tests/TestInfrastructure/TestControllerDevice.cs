using System;
using System.IO;
using x360ce.App;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The Test Controller, the plugin embedded in x360ce, as the tests add it.</summary>
	/// <remarks>
	/// It is read the way the program reads it: through the program's plugin manager, made here from the plugins embedded
	/// in x360ce when no test made one first.
	/// </remarks>
	public static class TestControllerDevice
	{
		/// <summary>The Test Controller's product, the Test Device's before it.</summary>
		public static readonly Guid ProductGuid = new Guid("a10ef011-41ab-4460-8c45-c1ad2dbf7ede");

		static readonly object Lock = new object();

		/// <summary>The program's plugin manager, holding the embedded plugins.</summary>
		public static PluginManager Manager
		{
			get
			{
				lock (Lock)
				{
					if (PluginManager.Current == null)
					{
						var folder = Path.Combine(Path.GetTempPath(), "x360ce.Tests.Plugins");
						var manager = new PluginManager(folder, null, () => false, typeof(Global).Assembly);
						manager.Start();
						PluginManager.Current = manager;
					}
					return PluginManager.Current;
				}
			}
		}

		/// <summary>The Test Controller's kind of device, as the embedded plugin describes it.</summary>
		public static PluginDeviceType Type
		{
			get
			{
				var type = Manager.FindDeviceType(ProductGuid);
				if (type == null)
					throw new InvalidOperationException("The Test Controller plugin is not embedded in x360ce.");
				return type;
			}
		}

		/// <summary>A new Test Controller for the device list, as the Add Plugin Device button makes one.</summary>
		public static UserDevice NewUserDevice()
		{
			return PluginDevices.NewUserDevice(Type);
		}

		/// <summary>The Test Controller's controls, as its device row describes them.</summary>
		public static DeviceObjectItem[] GetDeviceObjects()
		{
			var type = Type;
			return PluginDevices.GetDeviceObjects(type.AxisCount, type.SliderCount, type.PovCount, type.ButtonCount);
		}
	}
}
