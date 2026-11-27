using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Controls.UiTree;
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
			InitializeComponent();
			// The page's sentences and the Match column are worded as they are drawn: drawn again in the language chosen.
			EventHandler languageChanged = (s, e) =>
			{
				Redraw();
				PositionsDataGridView.Invalidate();
			};
			UiLanguage.Changed += languageChanged;
			Disposed += (s, e) => UiLanguage.Changed -= languageChanged;
		}

		void InitializeComponent()
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
			AddRow(DeviceLabel);
			AddRow(PositionsDataGridView, 100F);
			AddRow(buttons);
			// A row of its own, so a long answer wraps at the page's width.
			AddRow(StatusLabel);
			Controls.Add(MainTableLayoutPanel);
			ResumeLayout(false);
			PerformLayout();
		}

		TableLayoutPanel MainTableLayoutPanel;
		/// <summary>Which copies of the databases are in use, and what they say about the device.</summary>
		public Label DeviceLabel { get; private set; }
		public DataGridView PositionsDataGridView { get; private set; }
		public Button ApplyButton { get; private set; }
		public Button CopyButton { get; private set; }
		public Button UpdateButton { get; private set; }
		public Label StatusLabel { get; private set; }

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
				e.Value = Translation == null ? ""
					: row.Differs ? UiLanguage.T("SdlUserControl.Differs", "Differs") : UiLanguage.T("SdlUserControl.Same", "Same");
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
			// Each database is named with the copies of it in use.
			var x360ceCopies = x360ceDatabase.Describe();
			var sdlCopies = database.Describe();
			var device = SdlDevice.From(ud);
			SdlMapping mapping = null;
			string text;
			var pad = ud != null && !ud.IsKeyboard && !ud.IsMouse && ud.InputSource != InputSourceType.Plugin;
			// A plugin device is mapped as its plugin recommends, in a row of SDL's form.
			var recommended = AutoMapHelper.GetPluginMapping(ud);
			Translation = pad ? X360ceDatabase.AutoMapping(device, true, x360ceDatabase, database) : recommended;
			if (ud == null)
				text = string.Format(UiLanguage.T("SdlUserControl.DatabasesInUse",
					"x360ce’s database ({0}) and SDL’s database ({1}) are in use. Select a device on this controller to see what they say about it."),
					x360ceCopies, sdlCopies);
			else if (recommended != null)
				text = string.Format(UiLanguage.T("SdlUserControl.PluginRecommends",
					"x360ce’s database ({0}) and SDL’s database ({1}) describe game controllers, not this device. Its plugin recommends a mapping for it, and Auto Preset fills it from this row: {2}"),
					x360ceCopies, sdlCopies, recommended.Mapping)
					+ (recommended.Skipped.Count > 0 ? " " + recommended.SkippedText() : "");
			else if (!pad)
				text = string.Format(UiLanguage.T("SdlUserControl.NotGameController",
					"x360ce’s database ({0}) and SDL’s database ({1}) describe game controllers, not keyboards, mice or plugin devices."),
					x360ceCopies, sdlCopies);
			else
			{
				mapping = database.Find(device);
				var row = Translation == null ? null : Translation.X360ceRow;
				if (Translation == null && mapping != null)
					text = string.Format(CultureInfo.InvariantCulture, UiLanguage.T("SdlUserControl.SdlRowMapsNothing",
						"x360ce’s database ({0}) has no row for this controller. SDL’s database ({1}) knows it as \"{2}\" ({3}{4}), but its row maps none of this device’s controls, so Auto Preset guesses its mapping."),
						x360ceCopies, sdlCopies, mapping.Name, mapping.Guid.Text, Where(database, mapping, ", {0}"));
				else if (Translation == null)
					text = string.Format(CultureInfo.InvariantCulture, UiLanguage.T("SdlUserControl.NoRow",
						"Neither x360ce’s database ({0}) nor SDL’s database ({1}) has a row for this controller ({2}), so Auto Preset guesses its mapping. [Copy as SDL] writes a row for it, to share with SDL_GameControllerDB."),
						x360ceCopies, sdlCopies, Ids(device));
				else if (row == null)
					text = string.Format(CultureInfo.InvariantCulture, UiLanguage.T("SdlUserControl.SdlRowFills",
						"x360ce’s database ({0}) has no row for this controller. SDL’s database ({1}) knows it as \"{2}\" ({3}{4}), and Auto Preset fills it from that row."),
						x360ceCopies, sdlCopies, mapping.Name, mapping.Guid.Text, Where(database, mapping, ", {0}"));
				else if (mapping == null)
					text = string.Format(CultureInfo.InvariantCulture, UiLanguage.T("SdlUserControl.X360ceRowFills",
						"x360ce’s database ({0}) knows this controller as \"{1}\"{2}, used by {3} people, and Auto Preset fills it from that row. SDL’s database ({4}) has no row for it."),
						x360ceCopies, row.Name, Where(x360ceDatabase, row, " ({0})"), X360ceDatabase.Users(row), sdlCopies);
				else
					text = string.Format(CultureInfo.InvariantCulture, UiLanguage.T("SdlUserControl.BothRowsFill",
						"x360ce’s database ({0}) knows this controller as \"{1}\"{2}, used by {3} people; SDL’s database ({4}) knows it as \"{5}\" ({6}{7}). Auto Preset fills from x360ce’s row and takes SDL’s where x360ce’s maps nothing; From shows which."),
						x360ceCopies, row.Name, Where(x360ceDatabase, row, " ({0})"), X360ceDatabase.Users(row),
						sdlCopies, mapping.Name, mapping.Guid.Text, Where(database, mapping, ", {0}"));
			}
			if (pad)
			{
				var style = SdlPositions.FaceStyle(mapping, device.VendorId, mapping == null ? device.ProductName : mapping.Name);
				text += " " + string.Format(UiLanguage.T("SdlUserControl.ButtonsLabelledAs", "Its buttons are labelled as on {0}."), SdlPositions.Describe(style));
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
				? string.Format(CultureInfo.InvariantCulture, UiLanguage.T("SdlUserControl.VendorProductIds", "vendor 0x{0:X4}, product 0x{1:X4}"), device.VendorId, device.ProductId)
				: UiLanguage.T("SdlUserControl.NoIds", "no vendor or product id");
		}

		/// <summary>
		/// Which copy of a database a row came from, in words, as "from the downloaded copy", put into a format; empty when the
		/// database is one copy, which the page names already.
		/// </summary>
		/// <param name="database">The database the row is in.</param>
		/// <param name="mapping">The row.</param>
		/// <param name="format">Where the words go, as ", {0}".</param>
		static string Where(SdlDatabase database, SdlMapping mapping, string format)
		{
			var source = database.SourceOf(mapping);
			if (source == null || database.Sources.Count < 2)
				return "";
			string where;
			switch (source.Kind)
			{
				case SdlSourceKind.Downloaded: where = UiLanguage.T("SdlUserControl.FromDownloadedCopy", "from the downloaded copy"); break;
				case SdlSourceKind.File: where = string.Format(UiLanguage.T("SdlUserControl.FromSettingsFolder", "from {0} in the settings folder"), SdlDatabase.FileName); break;
				case SdlSourceKind.Environment: where = string.Format(UiLanguage.T("SdlUserControl.FromVariable", "from {0}"), SdlDatabase.EnvironmentVariable); break;
				default: where = UiLanguage.T("SdlUserControl.FromBuiltInCopy", "from the built-in copy"); break;
			}
			return string.Format(CultureInfo.InvariantCulture, format, where);
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
			say(string.Format(UiLanguage.T("SdlUserControl.Fetching", "Fetching the SDL controller database from {0}..."), address), false);
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
