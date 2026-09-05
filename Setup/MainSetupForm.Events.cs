using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.Setup
{
	public partial class MainSetupForm
	{
		private void SetFolder(string folderOrFile)
		{
			if (string.IsNullOrEmpty(folderOrFile))
				return;

			string targetDir = folderOrFile;
			bool isFile = File.Exists(folderOrFile);
			if (isFile)
			{
				targetDir = Path.GetDirectoryName(folderOrFile);
			}

			if (!Directory.Exists(targetDir))
				return;

			_selectedGameFolder = targetDir;
			folderTextBox.Text = isFile ? folderOrFile : targetDir;

			_detectedGames = _engine.ScanFolderForGameExecutables(targetDir);
			if (isFile && folderOrFile.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				if (!_detectedGames.Any(g => g.FilePath.Equals(folderOrFile, StringComparison.OrdinalIgnoreCase)))
				{
					_detectedGames.Insert(0, new DetectedGameInfo
					{
						FilePath = folderOrFile,
						Is64Bit = Environment.Is64BitOperatingSystem
					});
				}
			}

			if (_detectedGames.Count > 0)
			{
				var names = string.Join(", ", _detectedGames.Select(g => g.FileName));
				gameStatusLabel.Text = string.Format("✔ Found {0} game executable(s): {1}", _detectedGames.Count, names);
				gameStatusLabel.ForeColor = Color.FromArgb(22, 163, 74); // Crisp emerald green
				installButton.Enabled = true;
			}
			else
			{
				gameStatusLabel.Text = "ℹ No .exe files found directly in root, but folder can still be configured.";
				gameStatusLabel.ForeColor = Color.FromArgb(202, 138, 4); // Warm amber
				installButton.Enabled = true;
			}
		}

		private void BrowseButton_Click(object sender, EventArgs e)
		{
			using (var dialog = new FolderBrowserDialog())
			{
				dialog.Description = "Select your game's installation folder";
				dialog.ShowNewFolderButton = false;
				if (!string.IsNullOrEmpty(_selectedGameFolder) && Directory.Exists(_selectedGameFolder))
					dialog.SelectedPath = _selectedGameFolder;

				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					SetFolder(dialog.SelectedPath);
				}
			}
		}

		private void DetectedLibraryComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (detectedLibraryComboBox.SelectedIndex > 0)
			{
				var chosen = detectedLibraryComboBox.SelectedItem as string;
				if (!string.IsNullOrEmpty(chosen))
				{
					SetFolder(chosen);
				}
			}
		}

		private void MainSetupForm_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
				folderCard.IsHighlighted = true;
				folderCard.Invalidate();
			}
		}

		private void MainSetupForm_DragOver(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void MainSetupForm_DragLeave(object sender, EventArgs e)
		{
			folderCard.IsHighlighted = false;
			folderCard.Invalidate();
		}

		private void MainSetupForm_DragDrop(object sender, DragEventArgs e)
		{
			folderCard.IsHighlighted = false;
			folderCard.Invalidate();

			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files != null && files.Length > 0)
			{
				SetFolder(files[0]);
			}
		}

		private void InstallButton_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(_selectedGameFolder) || !Directory.Exists(_selectedGameFolder))
			{
				MessageBox.Show(this, "Please select a valid game folder first.", "No Folder Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			installButton.Enabled = false;
			browseButton.Enabled = false;
			installProgressBar.Visible = true;
			installProgressBar.SetProgress(0);
			logTextBox.Clear();

			logTextBox.AppendText("============================================================\r\n");
			logTextBox.AppendText("       x360ce AUTOMATIC INSTALLATION & CONTROLLER SETUP     \r\n");
			logTextBox.AppendText("============================================================\r\n");

			var success = _engine.InstallToFolder(
				_selectedGameFolder,
				msg =>
				{
					if (InvokeRequired)
					{
						Invoke(new Action(() => logTextBox.AppendText(msg + "\r\n")));
					}
					else
					{
						logTextBox.AppendText(msg + "\r\n");
					}
				},
				prog =>
				{
					if (InvokeRequired)
					{
						Invoke(new Action(() => installProgressBar.SetProgress(prog)));
					}
					else
					{
						installProgressBar.SetProgress(prog);
					}
				});

			if (success)
			{
				installButton.Text = "Game Configured & Optimized Successfully!";
				installButton.NormalColor = Color.FromArgb(22, 163, 74);
				installButton.HoverColor = Color.FromArgb(21, 128, 61);
				installButton.BorderColor = Color.FromArgb(21, 128, 61);
				postInstallPanel.Visible = true;
				AdjustLayout();
			}
			else
			{
				installButton.Text = "Installation Encountered an Issue";
				installButton.NormalColor = Color.FromArgb(220, 38, 38);
				installButton.Enabled = true;
				browseButton.Enabled = true;
			}
		}

		private void LaunchGameButton_Click(object sender, EventArgs e)
		{
			try
			{
				var primaryGame = _detectedGames.FirstOrDefault()?.FilePath;
				if (!string.IsNullOrEmpty(primaryGame) && File.Exists(primaryGame))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = primaryGame,
						WorkingDirectory = Path.GetDirectoryName(primaryGame)
					});
				}
				else if (!string.IsNullOrEmpty(_selectedGameFolder))
				{
					Process.Start("explorer.exe", _selectedGameFolder);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "Failed to launch game: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LaunchX360ceButton_Click(object sender, EventArgs e)
		{
			try
			{
				var exeInGame = Path.Combine(_selectedGameFolder, "x360ce.exe");
				if (File.Exists(exeInGame))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = exeInGame,
						WorkingDirectory = _selectedGameFolder
					});
				}
				else
				{
					var portableExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x360ce.exe");
					if (File.Exists(portableExe))
						Process.Start(portableExe);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "Failed to launch x360ce: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenFolderButton_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(_selectedGameFolder) && Directory.Exists(_selectedGameFolder))
			{
				Process.Start("explorer.exe", _selectedGameFolder);
			}
		}
	}
}
