using System;
using System.Collections.Generic;

namespace x360ce.Plugins
{
	/// <summary>The telemetry formats x360ce reads and the games it helps send telemetry, as x360ce keeps them in its settings.</summary>
	/// <remarks>
	/// x360ce's own lists, read only, made from its TelemetryFormats.xml, TelemetryGames.xml and the files beside them: a
	/// plugin reads them and keeps no copy. Each row is one of x360ce's database tables, [x360ce].[TelemetryFormat] and the
	/// rest, and a child row points to its parent by the parent's key. Read them on the interface thread.
	/// </remarks>
	public interface IPluginTelemetryData
	{
		IReadOnlyList<ITelemetryFormat> Formats { get; }
		IReadOnlyList<ITelemetryFormatSpec> FormatSpecs { get; }
		IReadOnlyList<ITelemetryGame> Games { get; }
		IReadOnlyList<ITelemetryGameStep> GameSteps { get; }
		IReadOnlyList<ITelemetryGameMod> GameMods { get; }
		IReadOnlyList<ITelemetryGameFile> GameFiles { get; }
	}

	/// <summary>A telemetry format x360ce's Wheel Telemetry plugin reads. Key: <see cref="TelemetryFormatId"/>.</summary>
	public interface ITelemetryFormat
	{
		/// <summary>The Wheel Telemetry plugin's id for it, such as Forza or EaF1.</summary>
		string TelemetryFormatId { get; }
		/// <summary>Its name, as the Wheel Telemetry plugin shows the source, such as Forza Data Out.</summary>
		string Name { get; }
		string Publisher { get; }
		/// <summary>UDP or SharedMemory.</summary>
		string Transport { get; }
		/// <summary>The UDP port the publisher's games send to unless set otherwise, or 0 when the game sets it.</summary>
		int DefaultPort { get; }
		/// <summary>The shared memory's name, or empty for UDP.</summary>
		string MapName { get; }
		string PacketSizes { get; }
		string Comment { get; }
		bool IsEnabled { get; }
	}

	/// <summary>An official specification of a telemetry format. Key: TelemetryFormatId and <see cref="SpecOrder"/>.</summary>
	public interface ITelemetryFormatSpec
	{
		Guid TelemetryFormatSpecId { get; }
		string TelemetryFormatId { get; }
		int SpecOrder { get; }
		/// <summary>The publisher's page, or empty where the publisher has none, which <see cref="Note"/> says.</summary>
		string Url { get; }
		string Version { get; }
		DateTime? SpecDate { get; }
		string Note { get; }
	}

	/// <summary>A game's program file, and the game it is. Key: <see cref="TelemetryGameId"/>; FileName and FileProductName are unique together.</summary>
	public interface ITelemetryGame
	{
		Guid TelemetryGameId { get; }
		/// <summary>The game's program file, as the Games tab lists it, such as GTA5.exe.</summary>
		string FileName { get; }
		string FileProductName { get; }
		string GameName { get; }
		/// <summary>The telemetry it sends: an <see cref="ITelemetryFormat.TelemetryFormatId"/>.</summary>
		string TelemetryFormatId { get; }
		/// <summary>The edition this program file is, such as Legacy, or empty.</summary>
		string Edition { get; }
		string Comment { get; }
		bool IsEnabled { get; }
	}

	/// <summary>A warning or a numbered step on a game's page. Key: TelemetryGameId, <see cref="IsWarning"/> and <see cref="StepOrder"/>.</summary>
	public interface ITelemetryGameStep
	{
		Guid TelemetryGameStepId { get; }
		Guid TelemetryGameId { get; }
		int StepOrder { get; }
		/// <summary>True for a warning, shown before the steps.</summary>
		bool IsWarning { get; }
		/// <summary>The text; {Port} stands for the port the game sends to.</summary>
		string Text { get; }
	}

	/// <summary>A mod a game needs, from its publisher's official page. Key: TelemetryGameId and <see cref="ModOrder"/>.</summary>
	public interface ITelemetryGameMod
	{
		Guid TelemetryGameModId { get; }
		Guid TelemetryGameId { get; }
		int ModOrder { get; }
		string Name { get; }
		/// <summary>The file, in the game's folder, that shows it is there.</summary>
		string DetectFile { get; }
		/// <summary>What that file's product name must contain, or empty when any will do.</summary>
		string DetectProduct { get; }
		string Url { get; }
	}

	/// <summary>A file a plugin installs into a game's folder. Key: TelemetryGameId and <see cref="TargetPath"/>.</summary>
	public interface ITelemetryGameFile
	{
		Guid TelemetryGameFileId { get; }
		Guid TelemetryGameId { get; }
		/// <summary>The file the plugin carries in its Resources folder; {Port} in it stands for the port the game sends to.</summary>
		string ResourceName { get; }
		/// <summary>Where in the game's folder it goes, such as scripts\x360ce.Telemetry.ini.</summary>
		string TargetPath { get; }
	}
}
