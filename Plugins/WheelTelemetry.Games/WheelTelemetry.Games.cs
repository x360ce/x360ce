#:include WheelTelemetry.Games.Data.cs
#:include WheelTelemetry.Games.Page.cs
#:include WheelTelemetry.Games.Help.cs
using System.ComponentModel;
using System.Linq;

namespace x360ce.Plugins.Samples
{
	/// <summary>A tab on the Games tab, for each game x360ce's telemetry data names, that helps the game send telemetry to x360ce's Wheel Telemetry plugin.</summary>
	/// <remarks>
	/// The plugin is four files, which the #:include lines above build as one: this one, the game plugin;
	/// WheelTelemetry.Games.Data.cs, which reads a game's rows of x360ce's data and installs its files;
	/// WheelTelemetry.Games.Page.cs, the page, made from those rows; and WheelTelemetry.Games.Help.cs, its help. The games,
	/// their steps, mods and files are x360ce's TelemetryGames data, not the plugin's: a game is added there, not in code.
	/// The plugin carries only the files a game's rows name, in Resources, such as GTA V's script. It opens no port: each
	/// game sends to the Wheel Telemetry plugin's.
	/// </remarks>
	[Description(WheelTelemetryGamesHelp.Text)]
	public class WheelTelemetryGames : IPluginGame
	{
		public string Name { get { return "Wheel Telemetry Games"; } }

		/// <summary>Every game file x360ce's telemetry data names.</summary>
		public string[] FileNames
		{
			get { return TelemetryData == null ? new string[0] : TelemetryData.Games.Where(x => x.IsEnabled).Select(x => x.FileName).ToArray(); }
		}

		public IPluginTelemetryData TelemetryData { get; set; }

		public string GamePath { get; set; }
	}
}
