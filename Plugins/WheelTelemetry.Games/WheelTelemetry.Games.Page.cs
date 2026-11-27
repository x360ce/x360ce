using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.Plugins.Samples
{
	/// <summary>
	/// A game's page on the Games tab, made from its catalogue entry alone: across the top, Open game folder, Install and
	/// Remove for a game the plugin installs files into, and the port; on the left the game and the telemetry it sends, each
	/// mod it needs with whether it is there and its official page, the installed files, and what x360ce must have on to
	/// read the game; on the right its warnings, then the numbered steps.
	/// </summary>
	/// <remarks>
	/// One layout for every game: nothing here is about one game. x360ce makes it with the plugin's instance for the game
	/// selected. The port starts as the one the installed files send to, or the Wheel Telemetry plugin's; a change is shown in
	/// the steps and written into files already installed. The page reads the game's folder once a second, so a mod unzipped
	/// there shows at once, and draws only what changed.
	///
	/// Tables of a caption, its value, and the action for it. Drawn in the system colours, so x360ce's theme applies, and
	/// laid out by the table and automatic sizes, with the room between controls taken from the font, so it follows the font
	/// and the zoom. Every control is named, so the AI door reaches it, and a control that cannot be used now is greyed, never
	/// hidden.
	/// </remarks>
	public class WheelTelemetryGamesPage : UserControl
	{
		public WheelTelemetryGamesPage(WheelTelemetryGames plugin)
		{
			_gamePath = plugin.GamePath ?? "";
			_folder = Path.GetDirectoryName(_gamePath) ?? "";
			_data = plugin.TelemetryData;
			_game = GameData.Find(_data, _gamePath);
			_port = _game == null ? GameData.DefaultPort : GameInstaller.ReadPort(_data, _game, _folder);
			Name = "WheelTelemetryGamesPage";
			// The page's name on the plugin's tab: the game's.
			Text = _game == null ? "Game" : _game.GameName;
			AccessibleName = Text;
			AccessibleDescription = "What to do so the game sends its telemetry to x360ce’s Wheel Telemetry plugin.";
			// As tall as x360ce's own page of the game: what does not fit scrolls.
			AutoScroll = true;
			SuspendLayout();
			Build();
			ResumeLayout(false);
			PerformLayout();
			_timer = new Timer { Interval = 1000 };
			_timer.Tick += (s, e) => ShowState();
			_timer.Start();
			ShowState();
		}

		readonly string _gamePath;
		readonly string _folder;
		readonly IPluginTelemetryData _data;
		readonly ITelemetryGame _game;
		ITelemetryGameStep[] _stepRows = new ITelemetryGameStep[0];
		ITelemetryGameFile[] _files = new ITelemetryGameFile[0];
		readonly Timer _timer;
		int _port;

		/// <summary>What Install or Remove last said went wrong, shown in place of the files' state until the next press.</summary>
		string _problem;

		/// <summary>True while the page sets its own controls, which must not write the files again.</summary>
		bool _showing;

		#region Controls

		internal TableLayoutPanel MainTableLayoutPanel;
		internal FlowLayoutPanel ActionsFlowLayoutPanel;
		internal TableLayoutPanel GameTableLayoutPanel;
		internal TableLayoutPanel StepsTableLayoutPanel;
		internal TableLayoutPanel WarningsTableLayoutPanel;
		internal Label GameValueLabel;
		internal Button OpenFolderButton;
		internal Label FormatValueLabel;
		internal NumericUpDown PortUpDown;
		internal Label FilesValueLabel;
		internal Button InstallButton;
		internal Button RemoveButton;
		internal Label WheelTelemetryNoteLabel;
		readonly List<Label> _steps = new List<Label>();
		readonly List<Label> _mods = new List<Label>();

		/// <summary>Gives each control its room from the font: run again when the font changes.</summary>
		readonly List<Action<int>> _spacing = new List<Action<int>>();

		/// <summary>
		/// The actions across the top, as on x360ce's own pages; under them, on the left, the game and what it needs, each with
		/// whether it is there, and on the right the warnings and the steps.
		/// </summary>
		void Build()
		{
			MainTableLayoutPanel = NewTable(nameof(MainTableLayoutPanel), 2);
			MainTableLayoutPanel.Dock = DockStyle.Top;
			// Two halves: the game's state and the steps side by side.
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			_spacing.Add(g => MainTableLayoutPanel.Padding = new Padding(g, g, g, 0));
			Controls.Add(MainTableLayoutPanel);
			if (_game == null)
			{
				var missing = NewValue("MissingLabel", "No entry", "x360ce’s telemetry data has no game for " + Path.GetFileName(_gamePath) + ".");
				missing.Text = missing.AccessibleDescription;
				AddWide(MainTableLayoutPanel, missing, 2);
				ApplyFont();
				return;
			}

			// The actions: what the page does to the game's folder, and the port the game sends to.
			ActionsFlowLayoutPanel = new FlowLayoutPanel
			{
				Name = nameof(ActionsFlowLayoutPanel),
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left,
				WrapContents = true,
			};
			OpenFolderButton = NewButton(nameof(OpenFolderButton), PluginText.T("WheelTelemetry.Games.OpenGameFolder", "Open game folder"), "Opens the game’s folder, where the downloads of the mods it needs are unzipped.");
			OpenFolderButton.Click += (s, e) => Process.Start(new ProcessStartInfo(_folder) { UseShellExecute = true });
			ActionsFlowLayoutPanel.Controls.Add(OpenFolderButton);
			_files = GameData.Files(_data, _game);
			var paths = string.Join(" and ", _files.Select(x => x.TargetPath));
			if (_files.Length > 0)
			{
				InstallButton = NewButton(nameof(InstallButton), PluginText.T("WheelTelemetry.Games.Install", "Install"), "Writes " + paths + " into the game’s folder, and nothing else.");
				InstallButton.Click += (s, e) => Change(() => GameInstaller.Install(_data, _game, _gamePath, _port));
				RemoveButton = NewButton(nameof(RemoveButton), PluginText.T("WheelTelemetry.Games.Remove", "Remove"), "Deletes " + paths + " from the game’s folder, and nothing else.");
				RemoveButton.Click += (s, e) => Change(() => GameInstaller.Remove(_data, _game, _gamePath));
				ActionsFlowLayoutPanel.Controls.Add(InstallButton);
				ActionsFlowLayoutPanel.Controls.Add(RemoveButton);
			}
			var portLabel = new Label { Name = "PortLabel", Text = PluginText.T("WheelTelemetry.Games.Port", "Port"), AutoSize = true, Anchor = AnchorStyles.Left };
			PortUpDown = NewNumber(nameof(PortUpDown), "Port", "The UDP port on this computer the game sends to: x360ce’s Wheel Telemetry plugin listens on 37150.", 1, 65535);
			PortUpDown.ValueChanged += (s, e) => SetPort((int)PortUpDown.Value);
			ActionsFlowLayoutPanel.Controls.Add(portLabel);
			ActionsFlowLayoutPanel.Controls.Add(PortUpDown);
			_spacing.Add(g =>
			{
				foreach (Control control in ActionsFlowLayoutPanel.Controls)
					control.Margin = new Padding(0, 0, g, 0);
				PortUpDown.Margin = Padding.Empty;
			});
			AddWide(MainTableLayoutPanel, ActionsFlowLayoutPanel, 2);

			// Left: the game, the telemetry it sends, the mods it needs and the files the plugin installs, each as it is now.
			GameTableLayoutPanel = NewTable(nameof(GameTableLayoutPanel), 3);
			// Captions as wide as the widest; values take the rest; links as wide as they are.
			GameTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			GameTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			GameTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_spacing.Add(g => GameTableLayoutPanel.Margin = new Padding(0, 0, g, 0));
			GameValueLabel = NewValue(nameof(GameValueLabel), "Game", "The game, its file, and the telemetry it sends.");
			AddRow(GameTableLayoutPanel, "Game", PluginText.T("WheelTelemetry.Games.Game", "Game"), GameValueLabel, null);
			// The telemetry it sends, named and linked from x360ce's list of formats.
			var format = GameData.Format(_data, _game);
			var spec = GameData.Spec(_data, _game);
			FormatValueLabel = NewValue(nameof(FormatValueLabel), "Telemetry", "The telemetry the game sends, which x360ce’s Wheel Telemetry plugin reads.");
			FormatValueLabel.Text = format == null
				? _game.TelemetryFormatId + "."
				: format.Name + (string.IsNullOrEmpty(format.Publisher) ? "" : ", from " + format.Publisher) + ".";
			AddRow(GameTableLayoutPanel, "Format", PluginText.T("WheelTelemetry.Games.Telemetry", "Telemetry"), FormatValueLabel, spec == null ? null : NewLink("FormatLinkLabel", PluginText.T("WheelTelemetry.Games.Specification", "Specification"), spec.Url));
			var mods = GameData.Mods(_data, _game);
			for (var i = 0; i < mods.Length; i++)
			{
				var key = "Mod" + (i + 1);
				var state = NewValue(key + "ValueLabel", mods[i].Name, "Whether " + mods[i].Name + " is in the game’s folder: " + mods[i].DetectFile + ".");
				state.Tag = mods[i];
				_mods.Add(state);
				AddRow(GameTableLayoutPanel, key, mods[i].Name, state, NewLink(key + "LinkLabel", PluginText.T("WheelTelemetry.Games.OfficialPage", "Official page"), mods[i].Url));
			}
			if (_files.Length > 0)
			{
				FilesValueLabel = NewValue(nameof(FilesValueLabel), "Installed files", "Whether " + paths + ", which Install writes, are in the game’s folder.");
				AddRow(GameTableLayoutPanel, "Files", PluginText.T("WheelTelemetry.Games.Files", "Files"), FilesValueLabel, null);
			}
			WheelTelemetryNoteLabel = NewValue(nameof(WheelTelemetryNoteLabel), "Wheel Telemetry", "What x360ce must have on to read the game’s telemetry.");
			WheelTelemetryNoteLabel.Text = PluginText.T("WheelTelemetry.Games.WheelTelemetryNote", "x360ce reads it on the wheel’s controller tab: Game Telemetry, Enable live game telemetry ticked and Listen port the same as Port here; Wheel, Centering spring ticked.");
			AddWide(GameTableLayoutPanel, WheelTelemetryNoteLabel, 3);

			// Right: the warnings, together in the colours of a tip, then the numbered steps.
			StepsTableLayoutPanel = NewTable(nameof(StepsTableLayoutPanel), 2);
			StepsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			StepsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			var warnings = GameData.Warnings(_data, _game);
			if (warnings.Length > 0)
			{
				WarningsTableLayoutPanel = NewTable(nameof(WarningsTableLayoutPanel), 1);
				WarningsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
				WarningsTableLayoutPanel.BackColor = SystemColors.Info;
				WarningsTableLayoutPanel.ForeColor = SystemColors.InfoText;
				_spacing.Add(g => WarningsTableLayoutPanel.Padding = new Padding(g / 2));
				for (var i = 0; i < warnings.Length; i++)
				{
					var warning = NewValue("Warning" + (i + 1) + "Label", "Warning", warnings[i].Text);
					warning.Text = warnings[i].Text;
					warning.Margin = Padding.Empty;
					WarningsTableLayoutPanel.RowStyles.Add(new RowStyle());
					WarningsTableLayoutPanel.RowCount = i + 1;
					WarningsTableLayoutPanel.Controls.Add(warning, 0, i);
				}
				AddWide(StepsTableLayoutPanel, WarningsTableLayoutPanel, 2);
			}
			_stepRows = GameData.Steps(_data, _game);
			for (var i = 0; i < _stepRows.Length; i++)
			{
				var step = NewValue("Step" + (i + 1) + "Label", "Step " + (i + 1), "Step " + (i + 1) + " of what to do.");
				_steps.Add(step);
				AddRow(StepsTableLayoutPanel, "Step" + (i + 1) + "Number", (i + 1).ToString(CultureInfo.CurrentCulture) + ".", step, null);
			}

			MainTableLayoutPanel.RowStyles.Add(new RowStyle());
			MainTableLayoutPanel.RowCount = MainTableLayoutPanel.RowStyles.Count;
			MainTableLayoutPanel.Controls.Add(GameTableLayoutPanel, 0, MainTableLayoutPanel.RowStyles.Count - 1);
			MainTableLayoutPanel.Controls.Add(StepsTableLayoutPanel, 1, MainTableLayoutPanel.RowStyles.Count - 1);
			ApplyFont();
		}

		/// <summary>A table as tall as its rows and as wide as its column gives it.</summary>
		static TableLayoutPanel NewTable(string name, int columns)
		{
			return new TableLayoutPanel
			{
				Name = name,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
				ColumnCount = columns,
				Margin = Padding.Empty,
			};
		}

		/// <summary>Adds a row: its caption, its value and its action, each with the room the font gives after it.</summary>
		/// <remarks>A value without an action spans the columns after it.</remarks>
		void AddRow(TableLayoutPanel table, string key, string caption, Control value, Control action)
		{
			var label = new Label { Name = key + "Label", Text = caption, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
			_spacing.Add(g =>
			{
				label.Margin = new Padding(0, 0, g, g);
				value.Margin = new Padding(0, 0, action == null ? 0 : g, g);
				if (action != null)
					action.Margin = new Padding(0, 0, 0, g);
			});
			var row = table.RowStyles.Count;
			table.RowStyles.Add(new RowStyle());
			table.RowCount = row + 1;
			table.Controls.Add(label, 0, row);
			table.Controls.Add(value, 1, row);
			if (action != null)
				table.Controls.Add(action, 2, row);
			else if (table.ColumnCount > 2)
				table.SetColumnSpan(value, table.ColumnCount - 1);
		}

		/// <summary>Adds a row of one control across a table.</summary>
		void AddWide(TableLayoutPanel table, Control control, int columns)
		{
			_spacing.Add(g => control.Margin = new Padding(0, 0, 0, g));
			var row = table.RowStyles.Count;
			table.RowStyles.Add(new RowStyle());
			table.RowCount = row + 1;
			table.Controls.Add(control, 0, row);
			table.SetColumnSpan(control, columns);
		}

		static Label NewValue(string name, string accessibleName, string description)
		{
			return new Label
			{
				Name = name,
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				AccessibleName = accessibleName,
				AccessibleDescription = description,
			};
		}

		Button NewButton(string name, string text, string description)
		{
			var button = new Button
			{
				Name = name,
				Text = text,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left,
				UseVisualStyleBackColor = true,
				AccessibleDescription = description,
			};
			_spacing.Add(g => button.Padding = new Padding(g / 2, 0, g / 2, 0));
			return button;
		}

		static LinkLabel NewLink(string name, string text, string address)
		{
			var link = new LinkLabel
			{
				Name = name,
				Text = text,
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				AccessibleName = text,
				AccessibleDescription = "Opens " + address + " in the browser. x360ce does not include or download it.",
			};
			link.Links[0].LinkData = address;
			link.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo((string)e.Link.LinkData) { UseShellExecute = true });
			return link;
		}

		NumericUpDown NewNumber(string name, string accessibleName, string description, int minimum, int maximum)
		{
			var number = new NumericUpDown
			{
				Name = name,
				Minimum = minimum,
				Maximum = maximum,
				Anchor = AnchorStyles.Left,
				AccessibleName = accessibleName,
				AccessibleDescription = description,
			};
			// As wide as its largest number and its arrows, in the font's units.
			_spacing.Add(g => number.Width = TextRenderer.MeasureText(maximum.ToString(CultureInfo.CurrentCulture) + "0", Font).Width + SystemInformation.VerticalScrollBarWidth + g);
			return number;
		}

		/// <summary>Gives the controls their room from the page's font, which follows x360ce's font and zoom.</summary>
		void ApplyFont()
		{
			// Half a line, 8 pixels at 100 % in x360ce's font.
			var gap = Math.Max(1, (Font.Height + 1) / 2);
			foreach (var space in _spacing)
				space(gap);
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (MainTableLayoutPanel != null)
				ApplyFont();
		}

		#endregion

		#region What the page shows

		void ShowState()
		{
			if (IsDisposed || _game == null)
				return;
			_showing = true;
			try
			{
				var found = GameInstaller.IsGame(_game, _gamePath);
				SetText(GameValueLabel, (found ? _game.GameName + (_game.Edition.Length == 0 ? "" : " " + _game.Edition) + ", " + Path.GetFileName(_gamePath)
					: "The game’s file is not at " + _gamePath) + ".");
				OpenFolderButton.Enabled = found;
				var port = _port.ToString(CultureInfo.InvariantCulture);
				for (var i = 0; i < _steps.Count; i++)
					SetText(_steps[i], _stepRows[i].Text.Replace(GameData.PortText, port));
				foreach (var mod in _mods)
					SetText(mod, found ? GameData.ModState((ITelemetryGameMod)mod.Tag, _folder) : PluginText.T("WheelTelemetry.Games.NoGameFolder", "No game folder."));
				SetNumber(PortUpDown, _port);
				if (FilesValueLabel != null)
				{
					var installed = found ? GameInstaller.Installed(_data, _game, _folder) : 0;
					SetText(FilesValueLabel, _problem ?? (!found ? PluginText.T("WheelTelemetry.Games.NoGameFolder", "No game folder.")
						: installed == 0 ? PluginText.T("WheelTelemetry.Games.NotInstalled", "Not installed.")
						: installed == _files.Length ? "Installed: " + string.Join(", ", _files.Select(x => x.TargetPath)) + ". They send to port " + port + "."
						: PluginText.T("WheelTelemetry.Games.PartlyInstalled", "Partly installed. Press Install again.")));
					InstallButton.Enabled = found;
					RemoveButton.Enabled = found && installed > 0;
				}
			}
			finally
			{
				_showing = false;
			}
		}

		static void SetText(Control control, string text)
		{
			if (control.Text != text)
				control.Text = text;
		}

		static void SetNumber(NumericUpDown number, int value)
		{
			var clamped = Math.Max(number.Minimum, Math.Min(number.Maximum, value));
			if (number.Value != clamped)
				number.Value = clamped;
		}

		#endregion

		#region What the page does

		/// <summary>Installs or removes, and says why Windows refused when it does: x360ce never asks for administrator rights by itself.</summary>
		void Change(Func<string> change)
		{
			try
			{
				_problem = change();
			}
			catch (UnauthorizedAccessException)
			{
				_problem = "Windows did not let x360ce write in " + _folder + ". That folder needs administrator rights: run x360ce as administrator and press the button again.";
			}
			catch (IOException ex)
			{
				_problem = "x360ce could not change the game’s folder: " + ex.Message;
			}
			ShowState();
		}

		/// <summary>Takes the port: the steps show it, and files already installed are written again with it.</summary>
		void SetPort(int port)
		{
			if (_showing)
				return;
			_port = port;
			if (_files.Length > 0 && GameInstaller.IsGame(_game, _gamePath) && GameInstaller.Installed(_data, _game, _folder) > 0)
				Change(() => GameInstaller.Install(_data, _game, _gamePath, _port));
			else
				ShowState();
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			if (disposing && _timer != null)
				_timer.Dispose();
			base.Dispose(disposing);
		}
	}
}
