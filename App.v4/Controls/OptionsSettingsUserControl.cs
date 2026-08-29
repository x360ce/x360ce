using JocysCom.ClassLibrary.Controls;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Shows where settings are kept, and lets a person keep them somewhere else.
	/// </summary>
	/// <remarks>
	/// There is a real choice to make here, and until now the program made it silently.
	/// C:\ProgramData lets anybody create a file and only its creator change it
	/// afterwards, so on a shared machine, or after an installer wrote the settings,
	/// a user can find their own settings read-only. This is where they can see that,
	/// and move to a folder nobody else's permissions can close.
	/// </remarks>
	public partial class OptionsSettingsUserControl : UserControl
	{

		public OptionsSettingsUserControl()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			MoveModeComboBox.Items.AddRange(new object[]
			{
				"Move them here, once every file is checked",
				"Copy them, and leave the originals",
				"Move them here and point the old folder at this one",
			});
			MoveModeComboBox.SelectedIndex = 0;
			// A drop-down list sizes to the width it was given, not to what is in it, so
			// the widest entry is measured and it is made that wide. Anchoring it to the
			// edge instead would stretch a list of three phrases across the window.
			FitToContent(MoveModeComboBox);
			Refresh2();
		}

		internal bool IsDesignMode { get { return ControlsHelper.IsDesignMode(this); } }

		/// <summary>Makes a list exactly as wide as its longest entry needs.</summary>
		private static void FitToContent(ComboBox box)
		{
			var widest = 0;
			using (var graphics = box.CreateGraphics())
				foreach (var item in box.Items)
					widest = Math.Max(widest, (int)graphics.MeasureString(item.ToString(), box.Font).Width);
			// Room for the arrow and the frame, which the text measurement knows nothing of.
			box.Width = widest + SystemInformation.VerticalScrollBarWidth + 8;
		}

		private SettingsLocation[] _locations;

		/// <summary>Reads the folders again and shows what each of them is good for.</summary>
		private void Refresh2()
		{
			_locations = SettingsLocation.All(AppDomain.CurrentDomain.BaseDirectory);
			var current = EngineHelper.AppDataPath;
			CurrentPathTextBox.Text = Path.Combine(current, "Settings");

			LocationComboBox.Items.Clear();
			foreach (var location in _locations)
				LocationComboBox.Items.Add(location.Name);
			var inUse = Array.FindIndex(_locations, x =>
				string.Equals(x.Path, current, StringComparison.OrdinalIgnoreCase));
			LocationComboBox.SelectedIndex = inUse < 0 ? 0 : inUse;
			FitToContent(LocationComboBox);
			ShowStatus();
		}

		/// <summary>Says what the chosen folder holds and whether it can be written.</summary>
		private void ShowStatus()
		{
			var location = Selected();
			if (location == null)
				return;
			PathLabel.Text = Path.Combine(location.Path, "Settings");
			var problem = location.WriteProblem;
			var holds = location.HasSettings ? "Holds settings." : "Empty.";
			StatusLabel.Text = problem == null
				? holds + " Can be written."
				: holds + " Cannot be written: " + problem;
			var inUse = string.Equals(location.Path, EngineHelper.AppDataPath, StringComparison.OrdinalIgnoreCase);
			ApplyButton.Enabled = !inUse && problem == null;
			ApplyButton.Text = inUse ? "In use" : "&Keep settings here";
		}

		private SettingsLocation Selected()
		{
			var index = LocationComboBox.SelectedIndex;
			return _locations == null || index < 0 || index >= _locations.Length
				? null
				: _locations[index];
		}

		private void LocationComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (IsDesignMode)
				return;
			ShowStatus();
		}

		private void OpenFolderButton_Click(object sender, EventArgs e)
		{
			var folder = Path.Combine(EngineHelper.AppDataPath, "Settings");
			// The folder is made when the first setting is saved, so a fresh install has
			// nothing to open yet. Opening the one above it still shows where it will be.
			if (!Directory.Exists(folder))
				folder = EngineHelper.AppDataPath;
			EngineHelper.BrowsePath(folder);
		}

		private void ApplyButton_Click(object sender, EventArgs e)
		{
			var target = Selected();
			if (target == null)
				return;
			var mode = MoveModeComboBox.SelectedIndex == 1
				? SettingsTransferMode.Copy
				: MoveModeComboBox.SelectedIndex == 2
					? SettingsTransferMode.Link
					: SettingsTransferMode.Move;
			var from = Path.Combine(EngineHelper.AppDataPath, "Settings");
			var to = Path.Combine(target.Path, "Settings");

			var warning = mode == SettingsTransferMode.Copy
				? "The settings will be copied. Both folders keep a copy, and they will " +
					"drift apart from now on."
				: "Every file is copied and then checked against the original before " +
					"anything is removed. If one does not match, nothing is removed at all.";
			var confirm = MessageBoxForm.Show(
				"Settings will be kept in:" + Environment.NewLine + to + Environment.NewLine + Environment.NewLine +
				warning + Environment.NewLine + Environment.NewLine + "Continue?",
				"Move settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (confirm != DialogResult.Yes)
				return;

			var result = SettingsTransfer.Run(from, to, mode);
			if (!result.Success)
			{
				MessageBoxForm.Show(
					"Nothing was changed." + Environment.NewLine + Environment.NewLine + result.Problem,
					"Settings not moved", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Refresh2();
				return;
			}
			SettingsLocation.Preference = target.Key;
			EngineHelper.AppDataPath = target.Path;
			SettingsManager.ReloadSettingsFiles();
			MessageBoxForm.Show(
				string.Format("{0} file(s) copied, {1} checked, {2} removed from the old folder.",
					result.Copied, result.Verified, result.Removed),
				"Settings moved", MessageBoxButtons.OK, MessageBoxIcon.Information);
			Refresh2();
		}

	}
}
