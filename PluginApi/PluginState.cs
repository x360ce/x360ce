namespace x360ce.Plugins
{
	/// <summary>Where a plugin device's controls are, as <see cref="IPluginDevice.Read"/> writes it.</summary>
	/// <remarks>Only the first <see cref="IPluginDevice.AxisCount"/> axes, and so on for the other controls, are read.</remarks>
	public sealed class PluginState
	{
		/// <summary>The middle of an axis or a slider.</summary>
		public const int Middle = 32767;

		/// <summary>The largest value of an axis or a slider.</summary>
		public const int Max = 65535;

		/// <summary>A hat switch that is not pressed.</summary>
		public const int PovReleased = -1;

		/// <summary>X, Y, Z, X Rotation, Y Rotation and Z Rotation, each 0 to 65535: <see cref="Middle"/> is the middle, 0 is left or up.</summary>
		public readonly int[] Axes = new int[6];

		/// <summary>Each slider, 0 to 65535.</summary>
		public readonly int[] Sliders = new int[8];

		/// <summary>Each hat switch: <see cref="PovReleased"/>, or the direction in hundredths of a degree clockwise from up, 0 to 35999.</summary>
		public readonly int[] Povs = { PovReleased, PovReleased, PovReleased, PovReleased };

		/// <summary>Each button: true while it is pressed.</summary>
		public readonly bool[] Buttons = new bool[128];
	}
}
