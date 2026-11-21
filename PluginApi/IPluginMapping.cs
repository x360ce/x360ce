namespace x360ce.Plugins
{
	/// <summary>A plugin device that recommends how it is mapped, as one row of SDL's game controller database (gamecontrollerdb.txt).</summary>
	/// <remarks>
	/// Optional: a class that implements <see cref="IPluginDevice"/> implements this too to say which of its controls is
	/// which part of the Xbox controller. A device put on a controller tab, and the tab's Auto Preset, fill the tab from
	/// it in place of x360ce's guess, and the tab's SDL page shows it. Read once, when x360ce loads the plugin.
	/// </remarks>
	public interface IPluginMapping
	{
		/// <summary>The row's mapping fields, as SDL writes them, such as "righttrigger:a0,lefttrigger:a1".</summary>
		/// <remarks>
		/// The GUID, name and platform fields may be left out: x360ce adds the device's own. Axes count X, Y, Z, X Rotation,
		/// Y Rotation, Z Rotation, then the first two sliders, from a0, only those the device has; buttons count from b0 and
		/// hat switches from h0. A whole axis on a trigger, a0, takes it from released at 0 to pressed all the way at 65535;
		/// +a0 takes only the axis's upper half.
		/// </remarks>
		string Mapping { get; }
	}
}
