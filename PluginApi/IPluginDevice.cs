using System;

namespace x360ce.Plugins
{
	/// <summary>A device a plugin adds to x360ce.</summary>
	/// <remarks>
	/// x360ce finds every public class that implements this and has a public constructor without parameters. Each one
	/// is a kind of device the person can add on the Devices page; x360ce makes one instance for each device added,
	/// lists it as a Plugin device, and maps it to a controller like any other.
	///
	/// <see cref="Read"/> is called on x360ce's input thread, up to a thousand times a second. It must return at once:
	/// anything slow, such as a network or a Bluetooth read, belongs on a thread of the plugin's own, which keeps the
	/// newest values for <see cref="Read"/> to copy. A plugin that also implements <see cref="IDisposable"/> is disposed
	/// when its device is removed and when x360ce closes.
	///
	/// An exception thrown from any member switches that kind of device off until x360ce starts again, and the Issues
	/// tab says why.
	/// </remarks>
	public interface IPluginDevice
	{
		/// <summary>The device's name, shown on the Devices page and in its Add list.</summary>
		string Name { get; }

		/// <summary>Names this kind of device: the same for every device of the kind, and in every version of the plugin.</summary>
		/// <remarks>Saved mappings and presets find their device by it, so a new value makes every saved mapping of the old one stop working.</remarks>
		Guid ProductGuid { get; }

		/// <summary>How many axes the device has, 0 to 6: X, Y, Z, X Rotation, Y Rotation and Z Rotation, in that order.</summary>
		int AxisCount { get; }

		/// <summary>How many sliders the device has, 0 to 8.</summary>
		int SliderCount { get; }

		/// <summary>How many hat switches (POVs) the device has, 0 to 4.</summary>
		int PovCount { get; }

		/// <summary>How many buttons the device has, 0 to 128.</summary>
		int ButtonCount { get; }

		/// <summary>Writes where the device's controls are now into <paramref name="state"/>.</summary>
		/// <param name="state">The device's own state, the same object on every call, holding what was written last.</param>
		void Read(PluginState state);
	}
}
