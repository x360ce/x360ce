using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Plugins;

namespace x360ce.App.Plugins
{
	/// <summary>The pages plugins add to the main window (<see cref="IPluginPage"/>).</summary>
	public static class PluginPages
	{
		/// <summary>Makes a page of each page class of the loaded plugins not tried yet, in the order the plugins loaded.</summary>
		/// <remarks>
		/// Called on the interface thread. A class is tried once: a page that throws while it is made is noted on its plugin
		/// (<see cref="PluginManager.FaultPage"/>), which the Plugins page and the Issues tab show, and left out.
		/// </remarks>
		/// <param name="manager">The plugins.</param>
		/// <param name="store">Where a page's values are kept (<see cref="IPluginHost.SetValue"/>).</param>
		/// <param name="tried">The page classes tried before; those tried now are added to it.</param>
		/// <returns>The pages made.</returns>
		public static Control[] Make(PluginManager manager, PluginSettings store, ISet<Type> tried)
		{
			var pages = new List<Control>();
			foreach (var file in manager.Files.Where(x => x.State == PluginFileState.Loaded))
			{
				foreach (var type in file.PageTypes)
				{
					if (!tried.Add(type))
						continue;
					try
					{
						var host = new PluginHost(file, manager, store);
						pages.Add(type.GetConstructor(new[] { typeof(IPluginHost) }) != null
							? (Control)Activator.CreateInstance(type, host)
							: (Control)Activator.CreateInstance(type));
					}
					catch (Exception ex)
					{
						manager.FaultPage(file, type, ex);
					}
				}
			}
			return pages.ToArray();
		}
	}

	/// <summary>What x360ce does for one plugin's page (<see cref="IPluginHost"/>): the same as for a person on its own pages.</summary>
	public sealed class PluginHost : IPluginHost
	{
		public PluginHost(PluginFile file, PluginManager manager, PluginSettings store)
		{
			_file = file;
			_manager = manager;
			_store = store;
		}

		readonly PluginFile _file;
		readonly PluginManager _manager;
		readonly PluginSettings _store;

		/// <summary>Adds a device of the kind as Add Plugin Device does, when the device list holds none.</summary>
		public void AddDevice(Guid productGuid)
		{
			var type = _manager.FindDeviceType(productGuid);
			if (type == null || type.Faulted)
				throw new InvalidOperationException("No loaded plugin adds a device with product " + productGuid + ".");
			if (SettingsManager.UserDevices.ItemsToArraySynchronized().Any(x => x.ProductGuid == productGuid))
				return;
			UserDevicesUserControl.AddPluginDevice(PluginDevices.NewUserDevice(type));
		}

		/// <summary>Maps the first device of the kind to a controller as the controller tab's Add does (<see cref="PadControl.MapDevices(Engine.Data.UserGame, MapTo, Engine.Data.UserDevice[])"/>).</summary>
		public void MapDevice(Guid productGuid, int controller)
		{
			if (controller < 1 || controller > 4)
				throw new ArgumentOutOfRangeException(nameof(controller), controller, "A controller is 1 to 4.");
			var game = SettingsManager.CurrentGame;
			if (game == null)
				throw new InvalidOperationException("No game is chosen. Choose one in the Game list first.");
			AddDevice(productGuid);
			var device = SettingsManager.UserDevices.ItemsToArraySynchronized().First(x => x.ProductGuid == productGuid);
			PadControl.MapDevices(game, (MapTo)controller, new[] { device });
		}

		public string GetValue(string name)
		{
			return _store.GetPluginValue(_file.Name, name);
		}

		public void SetValue(string name, string value)
		{
			_store.SetPluginValue(_file.Name, name, value);
		}
	}
}
