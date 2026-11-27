using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine.Data;
using x360ce.Plugins;

namespace x360ce.App.Plugins
{
	/// <summary>x360ce's telemetry formats and games, as plugins read them: the settings' own rows, behind the contract's read-only interfaces.</summary>
	/// <remarks>
	/// The engine does not reference the plugin contract, so the generated entities cannot implement its interfaces
	/// themselves; each row here is a view of one entity, made when a list is read, and holds no copy of its values.
	/// </remarks>
	public sealed class PluginTelemetryData : IPluginTelemetryData
	{
		/// <summary>The program's data, from <see cref="SettingsManager"/>.</summary>
		public static readonly PluginTelemetryData Current = new PluginTelemetryData();

		public IReadOnlyList<ITelemetryFormat> Formats { get { return View(SettingsManager.TelemetryFormats.Items, x => (ITelemetryFormat)new Format(x)); } }
		public IReadOnlyList<ITelemetryFormatSpec> FormatSpecs { get { return View(SettingsManager.TelemetryFormatSpecs.Items, x => (ITelemetryFormatSpec)new FormatSpec(x)); } }
		public IReadOnlyList<ITelemetryGame> Games { get { return View(SettingsManager.TelemetryGames.Items, x => (ITelemetryGame)new Game(x)); } }
		public IReadOnlyList<ITelemetryGameStep> GameSteps { get { return View(SettingsManager.TelemetryGameSteps.Items, x => (ITelemetryGameStep)new GameStep(x)); } }
		public IReadOnlyList<ITelemetryGameMod> GameMods { get { return View(SettingsManager.TelemetryGameMods.Items, x => (ITelemetryGameMod)new GameMod(x)); } }
		public IReadOnlyList<ITelemetryGameFile> GameFiles { get { return View(SettingsManager.TelemetryGameFiles.Items, x => (ITelemetryGameFile)new GameFile(x)); } }

		static IReadOnlyList<TView> View<TItem, TView>(IEnumerable<TItem> items, Func<TItem, TView> view)
		{
			return items.Select(view).ToList().AsReadOnly();
		}

		sealed class Format : ITelemetryFormat
		{
			public Format(TelemetryFormat item) { _item = item; }
			readonly TelemetryFormat _item;
			public string TelemetryFormatId { get { return _item.TelemetryFormatId; } }
			public string Name { get { return _item.Name; } }
			public string Publisher { get { return _item.Publisher; } }
			public string Transport { get { return _item.Transport; } }
			public int DefaultPort { get { return _item.DefaultPort; } }
			public string MapName { get { return _item.MapName; } }
			public string PacketSizes { get { return _item.PacketSizes; } }
			public string Comment { get { return _item.Comment; } }
			public bool IsEnabled { get { return _item.IsEnabled; } }
		}

		sealed class FormatSpec : ITelemetryFormatSpec
		{
			public FormatSpec(TelemetryFormatSpec item) { _item = item; }
			readonly TelemetryFormatSpec _item;
			public Guid TelemetryFormatSpecId { get { return _item.TelemetryFormatSpecId; } }
			public string TelemetryFormatId { get { return _item.TelemetryFormatId; } }
			public int SpecOrder { get { return _item.SpecOrder; } }
			public string Url { get { return _item.Url; } }
			public string Version { get { return _item.Version; } }
			public DateTime? SpecDate { get { return _item.SpecDate; } }
			public string Note { get { return _item.Note; } }
		}

		sealed class Game : ITelemetryGame
		{
			public Game(TelemetryGame item) { _item = item; }
			readonly TelemetryGame _item;
			public Guid TelemetryGameId { get { return _item.TelemetryGameId; } }
			public string FileName { get { return _item.FileName; } }
			public string FileProductName { get { return _item.FileProductName; } }
			public string GameName { get { return _item.GameName; } }
			public string TelemetryFormatId { get { return _item.TelemetryFormatId; } }
			public string Edition { get { return _item.Edition; } }
			public string LaunchArguments { get { return _item.LaunchArguments; } }
			public string Comment { get { return _item.Comment; } }
			public bool IsEnabled { get { return _item.IsEnabled; } }
		}

		sealed class GameStep : ITelemetryGameStep
		{
			public GameStep(TelemetryGameStep item) { _item = item; }
			readonly TelemetryGameStep _item;
			public Guid TelemetryGameStepId { get { return _item.TelemetryGameStepId; } }
			public Guid TelemetryGameId { get { return _item.TelemetryGameId; } }
			public int StepOrder { get { return _item.StepOrder; } }
			public bool IsWarning { get { return _item.IsWarning; } }
			public string Text { get { return _item.Text; } }
		}

		sealed class GameMod : ITelemetryGameMod
		{
			public GameMod(TelemetryGameMod item) { _item = item; }
			readonly TelemetryGameMod _item;
			public Guid TelemetryGameModId { get { return _item.TelemetryGameModId; } }
			public Guid TelemetryGameId { get { return _item.TelemetryGameId; } }
			public int ModOrder { get { return _item.ModOrder; } }
			public string Name { get { return _item.Name; } }
			public string DetectFile { get { return _item.DetectFile; } }
			public string DetectProduct { get { return _item.DetectProduct; } }
			public string Url { get { return _item.Url; } }
		}

		sealed class GameFile : ITelemetryGameFile
		{
			public GameFile(TelemetryGameFile item) { _item = item; }
			readonly TelemetryGameFile _item;
			public Guid TelemetryGameFileId { get { return _item.TelemetryGameFileId; } }
			public Guid TelemetryGameId { get { return _item.TelemetryGameId; } }
			public string ResourceName { get { return _item.ResourceName; } }
			public string TargetPath { get { return _item.TargetPath; } }
		}
	}
}
