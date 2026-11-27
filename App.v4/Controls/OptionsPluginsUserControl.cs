using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Controls.UiTree;
using JocysCom.ClassLibrary.IO;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Plugins;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>The plugins found, what became of each, and whether plugins from anyone else may load.</summary>
	/// <remarks>
	/// A plugin that did not load says why here, so a person who dropped a DLL into the folder is never left guessing.
	/// The list follows the plugin manager: a DLL that appears in the folder shows up without opening the page again,
	/// and one that appeared before the page was first opened is there when it is.
	/// </remarks>
	public partial class OptionsPluginsUserControl : UserControl
	{
		public OptionsPluginsUserControl()
		{
			InitializeComponent();
			// Scaled by the font once, after the designer's tables have resumed their layout: scaled in the designer, Windows
			// Forms scales a table as it resumes and then again with the control. The mode first, as changing it clears the
			// dimensions.
			SuspendLayout();
			AutoScaleMode = AutoScaleMode.Font;
			AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			ResumeLayout(false);
			if (ControlsHelper.IsDesignMode(this))
				return;
			FormsTheme.IndentUnder(NoteLabel, AllowThirdPartyPluginsCheckBox);
			FormsTheme.IndentUnder(ScriptsNoteLabel, AllowAutoHotkeyScriptsCheckBox);
			AutoHotkeyLayoutPanel.SizeChanged += (s, e) => WrapScriptsNote();
			ScriptsNoteLabel.MarginChanged += (s, e) => WrapScriptsNote();
			ControlsHelper.ApplyBorderStyle(PluginsDataGridView);
			PluginsDataGridView.AutoGenerateColumns = false;
			PluginsDataGridView.CurrentCellDirtyStateChanged += PluginsDataGridView_CurrentCellDirtyStateChanged;
			PluginsDataGridView.CellValueChanged += PluginsDataGridView_CellValueChanged;
			PluginsDataGridView.CellBeginEdit += PluginsDataGridView_CellBeginEdit;
			SettingsManager.LoadAndMonitor(x => x.AllowThirdPartyPlugins, AllowThirdPartyPluginsCheckBox);
			SettingsManager.LoadAndMonitor(x => x.AllowAutoHotkeyScripts, AllowAutoHotkeyScriptsCheckBox);
			SettingsManager.LoadAndMonitor(x => x.AutoHotkeyHeartbeat, HeartbeatUpDown);
			SettingsManager.LoadAndMonitor(x => x.AutoHotkeyHangTimeout, HangTimeoutUpDown);
			AllowAutoHotkeyScriptsCheckBox.CheckedChanged += AllowAutoHotkeyScriptsCheckBox_CheckedChanged;
			// A newer x360ce carries a newer library: written again for scripts already allowed.
			AllowAutoHotkeyScriptsCheckBox_CheckedChanged(this, EventArgs.Empty);
			var manager = PluginManager.Current;
			FolderTextBox.Text = manager == null ? "" : manager.Folder;
			// A change while the page has no window cannot be shown, so the list is read again when it gets one.
			HandleCreated += (s, e) =>
			{
				if (manager != null)
					manager.Changed += Manager_Changed;
				RefreshList();
			};
			HandleDestroyed += (s, e) =>
			{
				if (manager != null)
					manager.Changed -= Manager_Changed;
			};
			EventHandler languageChanged = (s, e) => RefreshList();
			UiLanguage.Changed += languageChanged;
			Disposed += (s, e) => UiLanguage.Changed -= languageChanged;
			RefreshList();
		}

		void Manager_Changed(object sender, EventArgs e)
		{
			// Raised on whichever thread found or switched off a plugin.
			if (IsHandleCreated && !IsDisposed)
				BeginInvoke((Action)RefreshList);
		}

		/// <summary>One row of the list: a plugin DLL and what became of it.</summary>
		public sealed class PluginRow
		{
			public bool Enabled { get; set; }
			public string Name { get; set; }
			public string Devices { get; set; }
			internal PluginFileState FileState { get; set; }
			public string State
			{
				get
				{
					switch (FileState)
					{
						case PluginFileState.Loaded: return UiLanguage.T("OptionsPluginsUserControl.StateLoaded", "Loaded");
						case PluginFileState.Disabled: return UiLanguage.T("OptionsPluginsUserControl.StateDisabled", "Disabled");
						case PluginFileState.WaitingForPermission: return UiLanguage.T("OptionsPluginsUserControl.StateWaitingForPermission", "Waiting for permission");
						default: return UiLanguage.T("OptionsPluginsUserControl.StateFailed", "Failed");
					}
				}
			}
			public string Signer { get; set; }
			public string Source { get; set; }
			public string Reason { get; set; }
		}

		/// <summary>The rows the list shows, from what the plugin manager knows now.</summary>
		public static PluginRow[] GetRows(PluginManager manager)
		{
			if (manager == null)
				return new PluginRow[0];
			return manager.Files.Select(x => new PluginRow
			{
				Enabled = x.Enabled,
				Name = x.Name,
				Devices = string.Join(", ",
					x.DeviceTypes.Select(t => t.Faulted ? SwitchedOffDeviceName(t.Name) : t.Name)
						.Concat(x.TelemetryTypes.Select(t => t.Faulted ? SwitchedOffDeviceName(t.Name) : t.Name))
						.Concat(x.GameTypes.Select(t => t.Faulted ? SwitchedOffDeviceName(t.Name)
							: string.Format(UiLanguage.T("OptionsPluginsUserControl.GamesTabDeviceName", "{0} (Games tab)"), t.Name)))),
				FileState = x.State,
				Signer = x.Embedded ? UiLanguage.T("OptionsPluginsUserControl.BuiltInSigner", "Part of x360ce") : x.Signer ?? UiLanguage.T("OptionsPluginsUserControl.UnsignedSigner", "Not signed"),
				// The folder is shown above the list: each row names the file within it.
				Source = x.Embedded ? UiLanguage.T("OptionsPluginsUserControl.EmbeddedSource", "Embedded") : PathHelper.GetRelativePath(manager.Folder.TrimEnd('\\') + "\\", x.Source, false),
				Reason = x.Reason ?? string.Join(" ",
					x.DeviceTypes.Where(t => t.Faulted).Select(t => t.Name + ": " + t.FaultReason)
						.Concat(x.TelemetryTypes.Where(t => t.Faulted).Select(t => t.Name + ": " + t.FaultReason))
						.Concat(x.GameTypes.Where(t => t.Faulted).Select(t => t.Name + ": " + t.FaultReason))),
			}).ToArray();
		}

		static string SwitchedOffDeviceName(string name)
		{
			return string.Format(UiLanguage.T("OptionsPluginsUserControl.SwitchedOffDeviceName", "{0} (switched off)"), name);
		}

		/// <summary>Lets the scripts' note wrap at the width its group has, so the group is as tall as the wrapped note needs.</summary>
		/// <remarks>
		/// The group is measured as tall as what it holds needs at no width, and its table measures a note that spans the column
		/// that takes what the others leave as one line: without a widest size the group would be a line short, and the row of
		/// number boxes below the note cut off at its edge, the more the narrower the window.
		///
		/// A minimized window leaves the group no width. The note then keeps the width it had: one pixel wide, a word to a line,
		/// it would stand thousands of pixels tall, and the rows it made that tall would stay so when the window is restored,
		/// leaving the list of plugins a pixel high.
		/// </remarks>
		void WrapScriptsNote()
		{
			var width = AutoHotkeyLayoutPanel.DisplayRectangle.Width - ScriptsNoteLabel.Margin.Horizontal;
			if (width <= 0)
				return;
			var widest = new System.Drawing.Size(width, 0);
			if (ScriptsNoteLabel.MaximumSize != widest)
				ScriptsNoteLabel.MaximumSize = widest;
		}

		bool _refreshing;

		void RefreshList()
		{
			_refreshing = true;
			try
			{
				PluginsDataGridView.DataSource = GetRows(PluginManager.Current);
			}
			finally
			{
				_refreshing = false;
			}
		}

		void PluginsDataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex != EnabledColumn.Index)
			{
				e.Cancel = true;
				return;
			}
			var rows = PluginsDataGridView.DataSource as PluginRow[];
			if (rows != null && e.RowIndex < rows.Length && rows[e.RowIndex].FileState == PluginFileState.Failed)
				e.Cancel = true;
		}

		void PluginsDataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
		{
			if (PluginsDataGridView.IsCurrentCellDirty && PluginsDataGridView.CurrentCell is DataGridViewCheckBoxCell)
				PluginsDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
		}

		void PluginsDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			if (_refreshing || e.RowIndex < 0 || e.ColumnIndex != EnabledColumn.Index)
				return;
			var rows = PluginsDataGridView.DataSource as PluginRow[];
			if (rows == null || e.RowIndex >= rows.Length)
				return;
			var row = rows[e.RowIndex];
			SettingsManager.Options.SetPluginEnabled(row.Name, row.Enabled);
			var manager = PluginManager.Current;
			if (manager != null)
				manager.SetEnabled(row.Name, row.Enabled);
		}

		void AllowAutoHotkeyScriptsCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			// The times mean nothing while no script may connect: greyed, not hidden.
			var allowed = AllowAutoHotkeyScriptsCheckBox.Checked;
			HeartbeatLabel.Enabled = allowed;
			HeartbeatUpDown.Enabled = allowed;
			HangTimeoutLabel.Enabled = allowed;
			HangTimeoutUpDown.Enabled = allowed;
			if (!allowed)
				return;
			try
			{
				InstallAutoHotkeyLibrary(AutoHotkeyLibraryFolder);
			}
			catch (Exception ex)
			{
				// Scripts still work with a copy of x360ce.ahk the person puts beside them.
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
			}
		}

		/// <summary>The user library folder AutoHotkey v2 searches for <c>#Include &lt;x360ce&gt;</c>: Documents\AutoHotkey\Lib.</summary>
		public static string AutoHotkeyLibraryFolder
		{
			get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AutoHotkey", "Lib"); }
		}

		/// <summary>Writes the x360ce.ahk this program carries into a folder, unless the same file is there already.</summary>
		/// <returns>The path of the library.</returns>
		public static string InstallAutoHotkeyLibrary(string folder)
		{
			byte[] bytes;
			using (var stream = typeof(OptionsPluginsUserControl).Assembly.GetManifestResourceStream("x360ce.App.Documents.x360ce.ahk"))
			using (var memory = new System.IO.MemoryStream())
			{
				stream.CopyTo(memory);
				bytes = memory.ToArray();
			}
			var path = System.IO.Path.Combine(folder, "x360ce.ahk");
			if (System.IO.File.Exists(path) && System.IO.File.ReadAllBytes(path).SequenceEqual(bytes))
				return path;
			System.IO.Directory.CreateDirectory(folder);
			System.IO.File.WriteAllBytes(path, bytes);
			return path;
		}

		private void OpenFolderButton_Click(object sender, EventArgs e)
		{
			var manager = PluginManager.Current;
			if (manager == null)
				return;
			System.IO.Directory.CreateDirectory(manager.Folder);
			EngineHelper.BrowsePath(manager.Folder);
		}
	}
}
