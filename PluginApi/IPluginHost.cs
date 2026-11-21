using System;

namespace x360ce.Plugins
{
	/// <summary>What a plugin's page may ask of x360ce.</summary>
	/// <remarks>Given to the constructor of the page (<see cref="IPluginPage"/>), and called on x360ce's interface thread.</remarks>
	public interface IPluginHost
	{
		/// <summary>Adds a device of this kind to the Devices list, as Add Plugin Device does, when the list holds none.</summary>
		/// <param name="productGuid">The kind of device: the <see cref="IPluginDevice.ProductGuid"/> of a class of a loaded plugin.</param>
		/// <exception cref="InvalidOperationException">No loaded plugin adds this kind of device, or it was switched off.</exception>
		void AddDevice(Guid productGuid);

		/// <summary>
		/// Maps the first device of this kind to a controller for the game chosen, as the controller tab's Add does: filled
		/// as the device recommends, and asking whether a device on another controller moves or is copied. Adds the device
		/// first when the Devices list holds none.
		/// </summary>
		/// <param name="productGuid">The kind of device.</param>
		/// <param name="controller">The controller, 1 to 4.</param>
		/// <exception cref="InvalidOperationException">No game is chosen, or no loaded plugin adds this kind of device.</exception>
		void MapDevice(Guid productGuid, int controller);

		/// <summary>The value the plugin kept under this name, or null when it kept none.</summary>
		string GetValue(string name);

		/// <summary>Keeps a value for the plugin, not for one of its devices, with x360ce's other settings; null forgets it.</summary>
		void SetValue(string name, string value);
	}
}
