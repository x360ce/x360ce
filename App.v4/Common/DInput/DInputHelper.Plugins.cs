using System;
using System.Collections.Generic;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		/// <summary>The plugin instance behind each plugin device read, by the device's instance GUID.</summary>
		/// <remarks>Made on a device's first read and kept, so a read makes nothing. Read and written only on the engine thread.</remarks>
		readonly Dictionary<Guid, PluginDeviceReader> _pluginReaders = new Dictionary<Guid, PluginDeviceReader>();

		/// <summary>Reads a plugin device: asks the plugin, and swaps its state in as a Raw Input device's is.</summary>
		/// <remarks>
		/// The plugin is called on this thread, so its Read must return at once. A plugin that throws switches its kind of
		/// device off until the program starts again: the manager says so on the Issues tab and in the Plugins list, and
		/// the next device refresh takes its devices offline. Until then the device keeps what it showed last.
		/// </remarks>
		void ReadPluginState(UserDevice ud)
		{
			var manager = PluginManager.Current;
			PluginDeviceReader reader;
			if (!_pluginReaders.TryGetValue(ud.InstanceGuid, out reader))
			{
				var type = manager == null ? null : manager.FindDeviceType(ud.ProductGuid);
				if (type == null || type.Faulted)
					return;
				try
				{
					reader = new PluginDeviceReader(type, type.NewInstance());
				}
				catch (Exception ex)
				{
					manager.Fault(type, ex);
					return;
				}
				_pluginReaders.Add(ud.InstanceGuid, reader);
			}
			if (reader.Type.Faulted)
				return;
			try
			{
				reader.Device.Read(reader.State);
			}
			catch (Exception ex)
			{
				if (manager != null)
					manager.Fault(reader.Type, ex);
				return;
			}
			var newState = ud.OldSourceState ?? new SourceState();
			reader.CopyTo(newState);
			LogChanges(ud, ud.SourceState, newState, watch.ElapsedTicks);
			ud.OldSourceState = ud.SourceState;
			ud.OldSourceStateTime = ud.SourceStateTime;
			ud.SourceState = newState;
			ud.SourceStateTime = watch.ElapsedTicks;
		}

		/// <summary>Lets go of the plugin instance behind a device no pass reads any more.</summary>
		void ReleasePluginReader(UserDevice ud)
		{
			PluginDeviceReader reader;
			if (!_pluginReaders.TryGetValue(ud.InstanceGuid, out reader))
				return;
			_pluginReaders.Remove(ud.InstanceGuid);
			DisposePluginReader(reader);
		}

		/// <summary>Lets go of every plugin instance, as the engine thread ends.</summary>
		void ReleasePluginReaders()
		{
			foreach (var reader in _pluginReaders.Values)
				DisposePluginReader(reader);
			_pluginReaders.Clear();
		}

		static void DisposePluginReader(PluginDeviceReader reader)
		{
			try
			{
				reader.Dispose();
			}
			catch (Exception ex)
			{
				var manager = PluginManager.Current;
				if (manager != null)
					manager.Fault(reader.Type, ex);
			}
		}
	}
}
