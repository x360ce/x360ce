using JocysCom.ClassLibrary.Controls;
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
			if (ControlsHelper.IsDesignMode(this))
				return;
			ControlsHelper.ApplyBorderStyle(PluginsDataGridView);
			PluginsDataGridView.AutoGenerateColumns = false;
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
			public string Name { get; set; }
			public string Devices { get; set; }
			public string State { get; set; }
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
				Name = x.Name,
				Devices = string.Join(", ", x.DeviceTypes.Select(t => t.Faulted ? t.Name + " (switched off)" : t.Name)),
				State = x.State == PluginFileState.Loaded ? "Loaded"
					: x.State == PluginFileState.WaitingForPermission ? "Waiting for permission"
					: "Failed",
				Signer = x.Embedded ? "Part of x360ce" : x.Signer ?? "Not signed",
				// The folder is shown above the list: each row names the file within it.
				Source = x.Embedded ? "Embedded" : PathHelper.GetRelativePath(manager.Folder.TrimEnd('\\') + "\\", x.Source, false),
				Reason = x.Reason ?? string.Join(" ", x.DeviceTypes.Where(t => t.Faulted).Select(t => t.Name + ": " + t.FaultReason)),
			}).ToArray();
		}

		void RefreshList()
		{
			PluginsDataGridView.DataSource = GetRows(PluginManager.Current);
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
