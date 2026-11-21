using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The SDL page of a controller tab: the mapping Auto gives the selected device, place by place, beside what the
	/// controller's boxes hold now: x360ce's controller database row for it, and SDL's where that row maps nothing
	/// (<see cref="X360ceDatabase.AutoMapping"/>); for a plugin device, the row its plugin recommends
	/// (<see cref="AutoMapHelper.GetPluginMapping(UserDevice)"/>).
	/// </summary>
	/// <remarks>
	/// The page says which copies of the databases are in use and which rows they have for the device. Each row of its list
	/// is a place on the Xbox controller: SDL's name for it, the place in words, what the pad labels it, the row's binding
	/// and its database, the device's control that binding is in x360ce's words, and what the box holds now, marked where
	/// the two differ. Drawn again only when the device, the controller's settings or a database change.
	/// </remarks>
	public class SdlUserControl : UserControl
	{
		public SdlUserControl()
		{
			Name = "SdlPanel";
			SuspendLayout();
			MainTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(MainTableLayoutPanel),
				Dock = DockStyle.Fill,
				// The page is made once the Controller page has been scaled, so its room is given at the screen's scale.
				Padding = AppHelper.ScaledPadding(0, 0, 0, 0),
				ColumnCount = 1,
			};
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			VersionLabel = NewLabel(nameof(VersionLabel));
			DeviceLabel = NewLabel(nameof(DeviceLabel));
			PositionsDataGridView = new DataGridView
			{
				Name = nameof(PositionsDataGridView),
				Dock = DockStyle.Fill,
				Margin = AppHelper.ScaledPadding(8, 8, 0, 0),
				AutoGenerateColumns = false,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false,
				AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
			};
			AddColumn("FieldColumn", "SDL field", "Field");
			AddColumn("PositionColumn", "Position", "Position");
			AddColumn("LabelColumn", "Label on this pad", "Label");
			AddColumn("BindingColumn", "Binding", "Binding");
			AddColumn("FromColumn", "From", "From");
			AddColumn("ControlColumn", "Maps", "Control");
			AddColumn("BoxColumn", "Box now", "Box");
			var differs = AddColumn("DiffersColumn", "Match", "Differs");
			differs.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			PositionsDataGridView.CellFormatting += PositionsDataGridView_CellFormatting;
			PositionsDataGridView.DataBindingComplete += (s, e) => PositionsDataGridView.ClearSelection();
			ControlsHelper.ApplyBorderStyle(PositionsDataGridView);
			ApplyButton = NewButton(nameof(ApplyButton), "Apply SDL Mapping");
			CopyButton = NewButton(nameof(CopyButton), "Copy as SDL");
			UpdateButton = NewButton(nameof(UpdateButton), "Update SDL Database");
			// Shown with an answer only: empty, its row would leave a band under the buttons.
			StatusLabel = NewLabel(nameof(StatusLabel));
			StatusLabel.Visible = false;
			ApplyButton.Click += (s, e) => ApplyClicked?.Invoke(this, EventArgs.Empty);
			CopyButton.Click += (s, e) => CopyClicked?.Invoke(this, EventArgs.Empty);
			UpdateButton.Click += UpdateButton_Click;
			var buttons = new FlowLayoutPanel
			{
				Name = "ButtonsFlowLayoutPanel",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Fill,
				Margin = AppHelper.ScaledPadding(0, 0, 0, 0),
				WrapContents = true,
			};
			buttons.Controls.AddRange(new Control[] { ApplyButton, CopyButton, UpdateButton });
			AddRow(VersionLabel);
			AddRow(DeviceLabel);
			AddRow(PositionsDataGridView, 100F);
			AddRow(buttons);
			// A row of its own, so a long answer wraps at the page's width.
			AddRow(StatusLabel);
			Controls.Add(MainTableLayoutPanel);
			ResumeLayout(false);
			PerformLayout();
		}

		readonly TableLayoutPanel MainTableLayoutPanel;
		public readonly Label VersionLabel;
		public readonly Label DeviceLabel;
		public readonly DataGridView PositionsDataGridView;
		public readonly Button ApplyButton;
		public readonly Button CopyButton;
		public readonly Button UpdateButton;
		public readonly Label StatusLabel;

		/// <summary>[Apply SDL Mapping] was pressed: the controller takes <see cref="Translation"/>.</summary>
		public event EventHandler ApplyClicked;

		/// <summary>[Copy as SDL] was pressed: the controller's mapping goes on the clipboard as an SDL row.</summary>
		public event EventHandler CopyClicked;

		/// <summary>The mapping Auto gives the device, as the list shows it; null when neither database nor the device's plugin has a row for the device.</summary>
		public SdlTranslation Translation { get; private set; }

		static Label NewLabel(string name)
		{
			// Across the column, so the table wraps it at the page's width.
			return new Label { Name = name, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = AppHelper.ScaledPadding(8, 8, 0, 0) };
		}

		static Button NewButton(string name, string text)
		{
			return new Button { Name = name, Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = AppHelper.ScaledPadding(8, 8, 0, 0), UseVisualStyleBackColor = true };
		}

		DataGridViewTextBoxColumn AddColumn(string name, string header, string property)
		{
			var column = new DataGridViewTextBoxColumn
			{
				Name = name,
				HeaderText = header,
				DataPropertyName = property,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
				SortMode = DataGridViewColumnSortMode.NotSortable,
			};
			PositionsDataGridView.Columns.Add(column);
			return column;
		}

		void AddRow(Control control, float percent = 0)
		{
			MainTableLayoutPanel.RowStyles.Add(percent > 0 ? new RowStyle(SizeType.Percent, percent) : new RowStyle());
			MainTableLayoutPanel.RowCount = MainTableLayoutPanel.RowStyles.Count;
			MainTableLayoutPanel.Controls.Add(control, 0, MainTableLayoutPanel.RowCount - 1);
		}

		void PositionsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0)
				return;
			var row = PositionsDataGridView.Rows[e.RowIndex].DataBoundItem as SdlPositionRow;
			if (row == null)
				return;
			if (e.ColumnIndex == PositionsDataGridView.Columns["DiffersColumn"].Index)
			{
				e.Value = Translation == null ? "" : row.Differs ? "Differs" : "Same";
				e.FormattingApplied = true;
			}
			// A difference wants a look, and is no error: the theme's caution colour. Said in words in the last column too, so
			// the mark does not rest on colour alone.
			if (row.Differs)
				e.CellStyle.ForeColor = FormsTheme.GetColor("ForegroundCaution");
		}

		/// <summary>What the page was last drawn from.</summary>
		Guid _instanceGuid;
		Guid _checksum;
		SdlDatabase _database;
		SdlDatabase _x360ceDatabase;
		bool _drawn;

		/// <summary>Draws the page for the device and the controller's settings, when they or a database changed since it was last drawn.</summary>
		/// <param name="ud">The device selected on the controller, or null.</param>
		/// <param name="current">The controller's settings now.</param>
		/// <param name="sdl">SDL's controller database; the program's own when null.</param>
		/// <param name="x360ce">x360ce's controller database; the program's own when null.</param>
		public void UpdateFrom(UserDevice ud, PadSetting current, SdlDatabase sdl = null, SdlDatabase x360ce = null)
		{
			var database = sdl ?? SdlDatabase.Current;
			var x360ceDatabase = x360ce ?? X360ceDatabase.Current;
			var instanceGuid = ud == null ? Guid.Empty : ud.InstanceGuid;
			var checksum = current == null ? Guid.Empty : current.PadSettingChecksum;
			if (_drawn && instanceGuid == _instanceGuid && checksum == _checksum && database == _database && x360ceDatabase == _x360ceDatabase)
				return;
			_drawn = true;
			_instanceGuid = instanceGuid;
			_checksum = checksum;
			_database = database;
			_x360ceDatabase = x360ceDatabase;
			ControlsHelper.SetText(VersionLabel, "x360ce's controller database: " + x360ceDatabase.Describe() + ". SDL controller database: " + database.Describe() + ".");
			var device = SdlDevice.From(ud);
			SdlMapping mapping = null;
			string text;
			var pad = ud != null && !ud.IsKeyboard && !ud.IsMouse && ud.InputSource != InputSourceType.Plugin;
			// A plugin device is mapped as its plugin recommends, in a row of SDL's form.
			var recommended = AutoMapHelper.GetPluginMapping(ud);
			Translation = pad ? X360ceDatabase.AutoMapping(device, true, x360ceDatabase, database) : recommended;
			if (ud == null)
				text = "Select a device on this controller to see what x360ce's and SDL's controller databases say about it.";
			else if (recommended != null)
				text = "Its plugin recommends a mapping for it, and Auto Preset fills it from this row: " + recommended.Mapping
					+ (recommended.Skipped.Count > 0 ? " " + recommended.SkippedText() : "");
			else if (!pad)
				text = "The controller databases describe game controllers, not keyboards, mice or plugin devices.";
			else
			{
				mapping = database.Find(device);
				var row = Translation == null ? null : Translation.X360ceRow;
				if (Translation == null && mapping != null)
					text = string.Format(CultureInfo.InvariantCulture,
						"SDL knows this controller as \"{0}\" ({1}, {2}), but its row maps none of this device's controls, so Auto Preset guesses its mapping.",
						mapping.Name, mapping.Guid.Text, Where(database.SourceOf(mapping)));
				else if (Translation == null)
					text = string.Format(CultureInfo.InvariantCulture,
						"Neither x360ce's controller database nor SDL's has a row for this controller ({0}), so Auto Preset guesses its mapping. [Copy as SDL] writes a row for it, to share with SDL_GameControllerDB.",
						Ids(device));
				else if (row == null)
					text = string.Format(CultureInfo.InvariantCulture,
						"SDL knows this controller as \"{0}\" ({1}, {2}), and Auto Preset fills it from this row.",
						mapping.Name, mapping.Guid.Text, Where(database.SourceOf(mapping)));
				else
				{
					text = string.Format(CultureInfo.InvariantCulture,
						"x360ce's controller database knows this controller as \"{0}\" ({1} people use its setting, {2}), ",
						row.Name, X360ceDatabase.Users(row), Where(x360ceDatabase.SourceOf(row)));
					text += mapping == null
						? "and Auto Preset fills it from this row. SDL's controller database has no row for it."
						: string.Format(CultureInfo.InvariantCulture,
							"and SDL's as \"{0}\" ({1}, {2}). Auto Preset fills it from x360ce's row, and from SDL's where x360ce's maps nothing, as From says.",
							mapping.Name, mapping.Guid.Text, Where(database.SourceOf(mapping)));
				}
			}
			if (pad)
			{
				var style = SdlPositions.FaceStyle(mapping, device.VendorId, mapping == null ? device.ProductName : mapping.Name);
				text += " Its buttons are labelled as on " + SdlPositions.Describe(style) + ".";
				if (Translation != null && Translation.Skipped.Count > 0)
					text += " " + Translation.SkippedText();
				PositionsDataGridView.DataSource = SdlPositions.Compare(Translation, current, style);
			}
			else
			{
				// A plugin's row is listed as on an Xbox pad, whose labels the boxes have.
				PositionsDataGridView.DataSource = recommended == null
					? new List<SdlPositionRow>()
					: SdlPositions.Compare(recommended, current, SdlFaceStyle.Abxy);
			}
			ControlsHelper.SetText(DeviceLabel, text);
			ControlsHelper.SetEnabled(ApplyButton, Translation != null && Translation.Count > 0);
			ControlsHelper.SetEnabled(CopyButton, ud != null);
		}

		/// <summary>A device's ids in words: "vendor 0x046D, product 0xC21D".</summary>
		static string Ids(SdlDevice device)
		{
			return device.HasIds
				? string.Format(CultureInfo.InvariantCulture, "vendor 0x{0:X4}, product 0x{1:X4}", device.VendorId, device.ProductId)
				: "no vendor or product id";
		}

		/// <summary>Where a row came from, in words.</summary>
		static string Where(SdlSource source)
		{
			if (source == null)
				return "";
			switch (source.Kind)
			{
				case SdlSourceKind.Downloaded: return "from the downloaded database";
				case SdlSourceKind.File: return "from " + SdlDatabase.FileName + " in the settings folder";
				case SdlSourceKind.Environment: return "from " + SdlDatabase.EnvironmentVariable;
				default: return "from the built-in database";
			}
		}

		/// <summary>Shows an answer under the buttons, in a row that is there only while it holds one.</summary>
		internal static void ShowStatus(Label label, string text)
		{
			label.Text = text;
			label.Visible = !string.IsNullOrEmpty(text);
		}

		/// <summary>Draws the page again at the next update, as after the database changed.</summary>
		public void Redraw()
		{
			_drawn = false;
		}

		/// <summary>What every [Update SDL Database] button fetches with. A test hands in one with its own answers.</summary>
		public static SdlUpdateClient Updater = new SdlUpdateClient();

		void UpdateButton_Click(object sender, EventArgs e)
		{
			StartUpdate(UpdateButton, (text, failed) => ShowStatus(StatusLabel, text), Redraw);
		}

		/// <summary>
		/// Fetches SDL's controller database from the address Options > Internet sets, on a worker thread, says how it went,
		/// and reads the database again once a newer one is saved. Every [Update SDL Database] button runs this.
		/// </summary>
		/// <remarks>The only network use the SDL features make, and only when a button is pressed. The button is off meanwhile.</remarks>
		/// <param name="button">The button pressed.</param>
		/// <param name="say">Says a sentence where the page shows how it went, on the interface thread; true when it is a failure.</param>
		/// <param name="done">Called on the interface thread once it is over, or null.</param>
		public static void StartUpdate(Button button, Action<string, bool> say, Action done)
		{
			var address = SettingsManager.Options.SdlDatabaseUrl;
			button.Enabled = false;
			say("Fetching the SDL controller database from " + address + "...", false);
			var path = SdlDatabase.SettingsFilePath;
			var current = SdlDatabase.Current;
			var updater = Updater;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate
			{
				SdlUpdateOutcome outcome;
				var message = updater.Update(address, path, current, out outcome);
				if (outcome == SdlUpdateOutcome.Saved)
				{
					SdlDatabase.Reload();
					// Read here, so the pages that draw it next do not wait for it.
					current = SdlDatabase.Current;
				}
				if (button.IsDisposed || !button.IsHandleCreated)
					return;
				button.BeginInvoke((Action)(() =>
				{
					button.Enabled = true;
					say(message, outcome == SdlUpdateOutcome.Failed);
					if (done != null)
						done();
				}));
			});
		}
	}
}
