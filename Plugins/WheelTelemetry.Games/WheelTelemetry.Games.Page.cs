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
	/// A game's page on the Games tab, made from its catalogue entry alone: its warnings first, the game and the telemetry it
	/// sends, the numbered steps, each mod it needs with whether it is there and its official page, the port, Install and
	/// Remove for a game the plugin installs files into, and what x360ce must have on to read the game.
	/// </summary>
	/// <remarks>
	/// One layout for every game: nothing here is about one game. x360ce makes it with the plugin's instance for the game
	/// selected. The port starts as the one the installed files send to, or the Wheel Telemetry plugin's; a change is shown in
	/// the steps and written into files already installed. The page reads the game's folder once a second, so a mod unzipped
	/// there shows at once, and draws only what changed.
	///
	/// One table: a caption, its value, and the action for it. Drawn in the system colours, so x360ce's theme applies, and
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
			AccessibleDescription = "What to do so the game sends its telemetry to x360ce's Wheel Telemetry plugin.";
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

		int _row;

		void Build()
		{
			MainTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(MainTableLayoutPanel),
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 3,
			};
			// Captions as wide as the widest; values take the rest; actions as wide as they are.
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_spacing.Add(g => MainTableLayoutPanel.Padding = new Padding(g, g, g, 0));
			Controls.Add(MainTableLayoutPanel);
			if (_game == null)
			{
				AddWide(NewValue("MissingLabel", "No entry", "x360ce's telemetry data has no game for " + Path.GetFileName(_gamePath) + "."));
				MainTableLayoutPanel.Controls[0].Text = MainTableLayoutPanel.Controls[0].AccessibleDescription;
				Finish();
				return;
			}

			// The warnings first, across the table, in the colours of a tip.
			var warnings = GameData.Warnings(_data, _game);
			for (var i = 0; i < warnings.Length; i++)
				AddWide(NewNote("Warning" + (i + 1) + "Label", "Warning", warnings[i].Text));

			GameValueLabel = NewValue(nameof(GameValueLabel), "Game", "The game, its file, and the telemetry it sends.");
			OpenFolderButton = NewButton(nameof(OpenFolderButton), "Open game folder", "Opens the game's folder, where the downloads of the mods it needs are unzipped.");
			OpenFolderButton.Click += (s, e) => Process.Start(new ProcessStartInfo(_folder) { UseShellExecute = true });
			AddRow("Game", "Game", GameValueLabel, OpenFolderButton);

			// The telemetry it sends, named and linked from x360ce's list of formats.
			var format = GameData.Format(_data, _game);
			var spec = GameData.Spec(_data, _game);
			FormatValueLabel = NewValue(nameof(FormatValueLabel), "Telemetry", "The telemetry the game sends, which x360ce's Wheel Telemetry plugin reads.");
			FormatValueLabel.Text = format == null
				? _game.TelemetryFormatId + "."
				: format.Name + (string.IsNullOrEmpty(format.Publisher) ? "" : ", from " + format.Publisher) + ".";
			AddRow("Format", "Telemetry", FormatValueLabel, spec == null ? null : NewLink("FormatLinkLabel", "Specification", spec.Url));

			_stepRows = GameData.Steps(_data, _game);
			for (var i = 0; i < _stepRows.Length; i++)
			{
				var step = NewValue("Step" + (i + 1) + "Label", "Step " + (i + 1), "Step " + (i + 1) + " of what to do.");
				_steps.Add(step);
				AddRow("Step" + (i + 1) + "Number", (i + 1).ToString(CultureInfo.CurrentCulture) + ".", step, null);
			}

			var mods = GameData.Mods(_data, _game);
			for (var i = 0; i < mods.Length; i++)
			{
				var key = "Mod" + (i + 1);
				var state = NewValue(key + "ValueLabel", mods[i].Name, "Whether " + mods[i].Name + " is in the game's folder: " + mods[i].DetectFile + ".");
				state.Tag = mods[i];
				_mods.Add(state);
				AddRow(key, mods[i].Name, state, NewLink(key + "LinkLabel", "Official page", mods[i].Url));
			}

			PortUpDown = NewNumber(nameof(PortUpDown), "Port", "The UDP port on this computer the game sends to: x360ce's Wheel Telemetry plugin listens on 37150.", 1, 65535);
			PortUpDown.ValueChanged += (s, e) => SetPort((int)PortUpDown.Value);
			AddRow("Port", "Port", PortUpDown, null);

			_files = GameData.Files(_data, _game);
			if (_files.Length > 0)
			{
				FilesValueLabel = NewValue(nameof(FilesValueLabel), "Installed files", "Whether the files the plugin installs into the game are there.");
				AddRow("Files", "Files", FilesValueLabel, null);
				var paths = string.Join(" and ", _files.Select(x => x.TargetPath));
				InstallButton = NewButton(nameof(InstallButton), "Install", "Writes " + paths + " into the game's folder, and nothing else.");
				InstallButton.Click += (s, e) => Change(() => GameInstaller.Install(_data, _game, _gamePath, _port));
				RemoveButton = NewButton(nameof(RemoveButton), "Remove", "Deletes " + paths + " from the game's folder, and nothing else.");
				RemoveButton.Click += (s, e) => Change(() => GameInstaller.Remove(_data, _game, _gamePath));
				var actions = new FlowLayoutPanel
				{
					Name = "ActionsFlowLayoutPanel",
					AutoSize = true,
					AutoSizeMode = AutoSizeMode.GrowAndShrink,
					Anchor = AnchorStyles.Left,
					WrapContents = true,
				};
				actions.Controls.Add(InstallButton);
				actions.Controls.Add(RemoveButton);
				_spacing.Add(g =>
				{
					InstallButton.Margin = new Padding(0, 0, g, 0);
					RemoveButton.Margin = new Padding(0);
				});
				AddRow("Actions", "", actions, null);
			}

			WheelTelemetryNoteLabel = NewValue(nameof(WheelTelemetryNoteLabel), "Wheel Telemetry", "What x360ce must have on to read the game's telemetry.");
			WheelTelemetryNoteLabel.Text = "x360ce reads the game while its Wheel Telemetry plugin listens on the same port: on the wheel's controller "
				+ "tab, Game Telemetry, Telemetry, Enable live game telemetry ticked and Listen port. The wheel feels the game while "
				+ "Centering spring is ticked on its Wheel tab.";
			AddWide(WheelTelemetryNoteLabel);
			Finish();
		}

		void Finish()
		{
			MainTableLayoutPanel.RowCount = _row;
			ApplyFont();
		}

		/// <summary>Adds a row: its caption, its value and its action, each with the room the font gives after it.</summary>
		/// <remarks>A value without an action spans the action's column too, but for a number box, whose width is its own.</remarks>
		void AddRow(string key, string caption, Control value, Control action)
		{
			var label = new Label { Name = key + "Label", Text = caption, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
			_spacing.Add(g =>
			{
				label.Margin = new Padding(0, 0, g, g);
				value.Margin = new Padding(0, 0, g, g);
				if (action != null)
					action.Margin = new Padding(0, 0, 0, g);
			});
			MainTableLayoutPanel.RowStyles.Add(new RowStyle());
			MainTableLayoutPanel.Controls.Add(label, 0, _row);
			MainTableLayoutPanel.Controls.Add(value, 1, _row);
			if (action != null)
				MainTableLayoutPanel.Controls.Add(action, 2, _row);
			else if (!(value is UpDownBase))
				MainTableLayoutPanel.SetColumnSpan(value, 2);
			_row++;
		}

		/// <summary>Adds a row of one control across the table.</summary>
		void AddWide(Control control)
		{
			_spacing.Add(g => control.Margin = new Padding(0, 0, 0, g));
			MainTableLayoutPanel.RowStyles.Add(new RowStyle());
			MainTableLayoutPanel.Controls.Add(control, 0, _row);
			MainTableLayoutPanel.SetColumnSpan(control, 3);
			_row++;
		}

		Label NewNote(string name, string accessibleName, string text)
		{
			var note = new Label
			{
				Name = name,
				Text = text,
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				BackColor = SystemColors.Info,
				ForeColor = SystemColors.InfoText,
				AccessibleName = accessibleName,
				AccessibleDescription = text,
			};
			_spacing.Add(g => note.Padding = new Padding(g / 2));
			return note;
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
					: "The game's file is not at " + _gamePath) + ".");
				OpenFolderButton.Enabled = found;
				var port = _port.ToString(CultureInfo.InvariantCulture);
				for (var i = 0; i < _steps.Count; i++)
					SetText(_steps[i], _stepRows[i].Text.Replace(GameData.PortText, port));
				foreach (var mod in _mods)
					SetText(mod, found ? GameData.ModState((ITelemetryGameMod)mod.Tag, _folder) : "No game folder.");
				SetNumber(PortUpDown, _port);
				if (FilesValueLabel != null)
				{
					var installed = found ? GameInstaller.Installed(_data, _game, _folder) : 0;
					SetText(FilesValueLabel, _problem ?? (!found ? "No game folder."
						: installed == 0 ? "Not installed."
						: installed == _files.Length ? "Installed: " + string.Join(", ", _files.Select(x => x.TargetPath)) + ". They send to port " + port + "."
						: "Partly installed. Press Install again."));
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
				_problem = "x360ce could not change the game's folder: " + ex.Message;
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
