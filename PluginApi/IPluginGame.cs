namespace x360ce.Plugins
{
	/// <summary>A plugin for a game: a tab of its own on the Games tab, shown while a game it is for is selected.</summary>
	/// <remarks>
	/// x360ce finds every public class that implements this and has a public constructor without parameters. Its page is
	/// a public Windows Forms control in the same assembly whose constructor takes the class, as a device's control takes
	/// the device. Each time a game is selected on the Games tab, x360ce makes an instance, sets <see cref="TelemetryData"/>,
	/// and reads <see cref="FileNames"/>, so names read from x360ce's data follow it. When the game's file has one of them,
	/// x360ce sets <see cref="GamePath"/>, makes the page with the instance and shows it on a tab named <see cref="Name"/>,
	/// after x360ce's own settings of the game, with the class's [Description] on a Help page beside it. Another game
	/// selected, the plugin switched off, or x360ce closing disposes the page, and the instance when it implements
	/// <see cref="System.IDisposable"/>.
	///
	/// The instance and its page live on the interface thread. The tab is as tall as x360ce's own page of the game, and the
	/// page fills it: a longer page scrolls itself (AutoScroll, with its table docked to the top).
	/// </remarks>
	public interface IPluginGame
	{
		/// <summary>The name of its tab on the Games tab, such as "GTA V".</summary>
		string Name { get; }

		/// <summary>The file names of the games it is for, such as "GTA5.exe"; x360ce compares them without regard to case.</summary>
		string[] FileNames { get; }

		/// <summary>x360ce's telemetry formats and games, which x360ce sets before it reads anything else.</summary>
		IPluginTelemetryData TelemetryData { get; set; }

		/// <summary>The full path of the selected game's file, such as C:\Games\GTAV\GTA5.exe. x360ce sets it before it makes the page.</summary>
		string GamePath { get; set; }
	}
}
