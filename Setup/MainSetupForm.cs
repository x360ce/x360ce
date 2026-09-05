using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.Setup
{
	public class MainSetupForm : Form
	{
		private readonly SetupEngine _engine = new SetupEngine();
		private string _selectedGameFolder;
		private List<DetectedGameInfo> _detectedGames = new List<DetectedGameInfo>();
		private List<DetectedControllerInfo> _detectedControllers = new List<DetectedControllerInfo>();

		// Header
		private X360ceHeaderBanner headerBanner;

		// Main Content
		private Panel contentPanel;
		private X360ceCard folderCard;
		private TextBox folderTextBox;
		private X360ceButton browseButton;
		private Label libraryLabel;
		private ComboBox detectedLibraryComboBox;
		private Label gameStatusLabel;

		private X360ceCard controllerCard;
		private X360ceDeviceGrid deviceGrid;
		private Label hardwareOptLabel;

		// Action & Progress
		private X360ceButton installButton;
		private X360ceProgressBar installProgressBar;
		private TextBox logTextBox;

		// Post Install
		private Panel postInstallPanel;
		private X360ceButton launchGameButton;
		private X360ceButton launchX360ceButton;
		private X360ceButton openFolderButton;

		public MainSetupForm() : this(null) { }

		public MainSetupForm(string initialFolder)
		{
			InitializeComponents();
			LoadHardwareAndLibraries();

			if (!string.IsNullOrEmpty(initialFolder) && (Directory.Exists(initialFolder) || File.Exists(initialFolder)))
			{
				SetFolder(initialFolder);
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			UiHelper.ApplyModernWindowTheme(Handle);
		}

		private void InitializeComponents()
		{
			Text = "x360ce Game Setup & Controller Optimizer";
			AutoScaleMode = AutoScaleMode.Dpi;
			Size = new Size(820, 715);
			MinimumSize = new Size(780, 660);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Color.FromArgb(243, 244, 246); // Clean neutral background matching x360ce
			ForeColor = Color.FromArgb(15, 23, 42);
			Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
			AllowDrop = true;
			DoubleBuffered = true;

			DragEnter += MainSetupForm_DragEnter;
			DragOver += MainSetupForm_DragOver;
			DragLeave += MainSetupForm_DragLeave;
			DragDrop += MainSetupForm_DragDrop;

			// ==========================================
			// 1. Signature x360ce Top Banner
			// ==========================================
			headerBanner = new X360ceHeaderBanner
			{
				Subject = "x360ce Game Setup & Controller Optimizer",
				Description = "Useful Tip: Select your game folder below to automatically deploy emulator files and calibrate Player 1 & 2 gamepads."
			};

			try
			{
				var candidates = new[]
				{
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x360ce.exe"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Release_Portable\x360ce.exe"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\Release_Portable\x360ce.exe"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\App.v4\bin\Release\x360ce.exe"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\App.v4\app.ico"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico")
				};

				foreach (var cand in candidates)
				{
					if (File.Exists(cand))
					{
						Icon ico = cand.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
							? new Icon(cand, 48, 48)
							: Icon.ExtractAssociatedIcon(cand);

						if (ico != null)
						{
							Icon = ico;
							headerBanner.AppIconImage = ico.ToBitmap();
							break;
						}
					}
				}
			}
			catch { }

			// ==========================================
			// 2. Main Content Container
			// ==========================================
			contentPanel = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(18, 12, 18, 12),
				AutoScroll = true
			};

			// ==========================================
			// Card 1: Game Folder Selection
			// ==========================================
			folderCard = new X360ceCard
			{
				CardTitle = "1. Target Game Installation Folder",
				Location = new Point(18, 10),
				Size = new Size(766, 134)
			};

			folderTextBox = new TextBox
			{
				Location = new Point(14, 36),
				Size = new Size(628, 27),
				Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
				BackColor = Color.White,
				ForeColor = Color.FromArgb(15, 23, 42),
				BorderStyle = BorderStyle.FixedSingle
			};

			browseButton = new X360ceButton
			{
				Text = "Browse...",
				Location = new Point(650, 34),
				Size = new Size(102, 30),
				Font = new Font("Segoe UI", 9F, FontStyle.Regular),
				NormalColor = Color.FromArgb(241, 245, 249),
				HoverColor = Color.FromArgb(226, 232, 240),
				PressedColor = Color.FromArgb(203, 213, 225),
				BorderColor = Color.FromArgb(203, 213, 225),
				ForeColor = Color.FromArgb(15, 23, 42),
				CornerRadius = 6
			};
			browseButton.Click += BrowseButton_Click;

			libraryLabel = new Label
			{
				Text = "Quick-Pick:",
				UseMnemonic = false,
				Location = new Point(14, 73),
				Size = new Size(75, 20),
				Font = new Font("Segoe UI", 8.8F, FontStyle.Regular),
				ForeColor = Color.FromArgb(71, 85, 105)
			};

			detectedLibraryComboBox = new ComboBox
			{
				Location = new Point(94, 70),
				Size = new Size(658, 25),
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new Font("Segoe UI", 8.8F, FontStyle.Regular),
				BackColor = Color.White,
				ForeColor = Color.FromArgb(15, 23, 42),
				FlatStyle = FlatStyle.System
			};
			detectedLibraryComboBox.SelectedIndexChanged += DetectedLibraryComboBox_SelectedIndexChanged;

			gameStatusLabel = new Label
			{
				Text = "Status: Drag & drop your game folder or executable here, or click Browse.",
				UseMnemonic = false,
				Location = new Point(14, 105),
				Size = new Size(738, 20),
				Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
				ForeColor = Color.FromArgb(100, 116, 139)
			};

			folderCard.Controls.Add(folderTextBox);
			folderCard.Controls.Add(browseButton);
			folderCard.Controls.Add(libraryLabel);
			folderCard.Controls.Add(detectedLibraryComboBox);
			folderCard.Controls.Add(gameStatusLabel);

			// ==========================================
			// Card 2: Connected Controllers & Calibration
			// ==========================================
			controllerCard = new X360ceCard
			{
				CardTitle = "2. Connected Gamepads & Hardware Auto-Calibration",
				Location = new Point(18, 152),
				Size = new Size(766, 138)
			};

			deviceGrid = new X360ceDeviceGrid
			{
				Location = new Point(14, 30),
				Size = new Size(738, 80)
			};

			hardwareOptLabel = new Label
			{
				Text = "✔ DirectInput 1000 Hz polling, AboveNormal CPU priority & verified Twin USB mapping ready.",
				UseMnemonic = false,
				Location = new Point(14, 115),
				Size = new Size(738, 18),
				Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
				ForeColor = Color.FromArgb(22, 163, 74) // Crisp green
			};

			controllerCard.Controls.Add(deviceGrid);
			controllerCard.Controls.Add(hardwareOptLabel);

			// ==========================================
			// Action Button & Progress
			// ==========================================
			installButton = new X360ceButton
			{
				Text = "Install & Optimize Game",
				Location = new Point(18, 300),
				Size = new Size(766, 46),
				Font = new Font("Segoe UI", 11F, FontStyle.Bold),
				NormalColor = Color.FromArgb(37, 99, 235), // Primary Windows Blue
				HoverColor = Color.FromArgb(29, 78, 216),
				PressedColor = Color.FromArgb(30, 64, 175),
				BorderColor = Color.FromArgb(29, 78, 216),
				CornerRadius = 8
			};
			installButton.Click += InstallButton_Click;

			installProgressBar = new X360ceProgressBar
			{
				Location = new Point(18, 354),
				Size = new Size(766, 10),
				CornerRadius = 4,
				Visible = false
			};

			// ==========================================
			// Terminal / Log Box
			// ==========================================
			logTextBox = new TextBox
			{
				Location = new Point(18, 372),
				Size = new Size(766, 120),
				Multiline = true,
				ReadOnly = true,
				ScrollBars = ScrollBars.Vertical,
				BackColor = Color.White,
				ForeColor = Color.FromArgb(30, 41, 59),
				Font = new Font("Consolas", 9F, FontStyle.Regular),
				BorderStyle = BorderStyle.FixedSingle
			};

			// ==========================================
			// Post Install Actions Panel
			// ==========================================
			postInstallPanel = new Panel
			{
				Location = new Point(18, 500),
				Size = new Size(766, 46),
				Visible = false
			};

			launchGameButton = new X360ceButton
			{
				Text = "Launch Game",
				Location = new Point(0, 2),
				Size = new Size(246, 40),
				Font = new Font("Segoe UI", 10F, FontStyle.Bold),
				NormalColor = Color.FromArgb(16, 185, 129),
				HoverColor = Color.FromArgb(5, 150, 105),
				BorderColor = Color.FromArgb(5, 150, 105),
				CornerRadius = 6
			};
			launchGameButton.Click += LaunchGameButton_Click;

			launchX360ceButton = new X360ceButton
			{
				Text = "Open x360ce",
				Location = new Point(260, 2),
				Size = new Size(246, 40),
				Font = new Font("Segoe UI", 10F, FontStyle.Bold),
				NormalColor = Color.FromArgb(79, 70, 229),
				HoverColor = Color.FromArgb(67, 56, 202),
				BorderColor = Color.FromArgb(67, 56, 202),
				CornerRadius = 6
			};
			launchX360ceButton.Click += LaunchX360ceButton_Click;

			openFolderButton = new X360ceButton
			{
				Text = "Open Folder",
				Location = new Point(520, 2),
				Size = new Size(246, 40),
				Font = new Font("Segoe UI", 10F, FontStyle.Bold),
				NormalColor = Color.FromArgb(71, 85, 105),
				HoverColor = Color.FromArgb(51, 65, 85),
				BorderColor = Color.FromArgb(51, 65, 85),
				CornerRadius = 6
			};
			openFolderButton.Click += OpenFolderButton_Click;

			postInstallPanel.Controls.Add(launchGameButton);
			postInstallPanel.Controls.Add(launchX360ceButton);
			postInstallPanel.Controls.Add(openFolderButton);

			contentPanel.Controls.Add(folderCard);
			contentPanel.Controls.Add(controllerCard);
			contentPanel.Controls.Add(installButton);
			contentPanel.Controls.Add(installProgressBar);
			contentPanel.Controls.Add(logTextBox);
			contentPanel.Controls.Add(postInstallPanel);

			logTextBox.AppendText("Ready. Select your game folder above to begin automatic setup.\r\n");
			logTextBox.AppendText("• Automatically copies emulator DLLs and generates optimal configuration.\r\n");
			logTextBox.AppendText("• DirectInput 1000 Hz polling and verified button mappings are ready for connected gamepads.\r\n");

			Controls.Add(contentPanel);
			Controls.Add(headerBanner);

			contentPanel.Resize += (s, e) => AdjustLayout();
			AdjustLayout();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			AdjustLayout();
		}

		private void AdjustLayout()
		{
			if (contentPanel == null || folderCard == null || controllerCard == null)
				return;

			int clientW = contentPanel.ClientSize.Width;
			int clientH = contentPanel.ClientSize.Height;
			if (clientW < 200) return;

			int cardMargin = 16;
			int cardW = Math.Max(500, clientW - (cardMargin * 2));

			// Card 1
			folderCard.Location = new Point(cardMargin, 10);
			folderCard.Size = new Size(cardW, 136);

			int innerW = folderCard.ClientSize.Width;
			browseButton.Size = new Size(102, 30);
			browseButton.Location = new Point(innerW - 14 - browseButton.Width, 34);
			folderTextBox.Location = new Point(14, 36);
			folderTextBox.Size = new Size(Math.Max(100, browseButton.Left - 14 - 10), 27);

			libraryLabel.Location = new Point(14, 73);
			int comboX = libraryLabel.Right + 8;
			detectedLibraryComboBox.Location = new Point(comboX, 70);
			detectedLibraryComboBox.Size = new Size(Math.Max(100, innerW - comboX - 14), 25);

			gameStatusLabel.Location = new Point(14, 105);
			gameStatusLabel.Size = new Size(innerW - 28, 20);

			// Card 2
			controllerCard.Location = new Point(cardMargin, folderCard.Bottom + 12);
			controllerCard.Size = new Size(cardW, 144);

			int card2W = controllerCard.ClientSize.Width;
			deviceGrid.Location = new Point(14, 30);
			deviceGrid.Size = new Size(card2W - 28, 80);

			hardwareOptLabel.Location = new Point(14, 118);
			hardwareOptLabel.Size = new Size(card2W - 28, 18);

			// Action button & progress
			installButton.Location = new Point(cardMargin, controllerCard.Bottom + 12);
			installButton.Size = new Size(cardW, 46);

			installProgressBar.Location = new Point(cardMargin, installButton.Bottom + 8);
			installProgressBar.Size = new Size(cardW, 10);

			int logTop = installProgressBar.Bottom + 8;
			if (postInstallPanel.Visible)
			{
				int postHeight = 44;
				int postTop = clientH - postHeight - 12;
				postInstallPanel.Location = new Point(cardMargin, postTop);
				postInstallPanel.Size = new Size(cardW, postHeight);

				int btnW = (cardW - 24) / 3;
				launchGameButton.Location = new Point(0, 2);
				launchGameButton.Size = new Size(btnW, 40);

				launchX360ceButton.Location = new Point(btnW + 12, 2);
				launchX360ceButton.Size = new Size(btnW, 40);

				openFolderButton.Location = new Point((btnW + 12) * 2, 2);
				openFolderButton.Size = new Size(cardW - openFolderButton.Left, 40);

				int logHeight = Math.Max(90, postTop - logTop - 8);
				logTextBox.Location = new Point(cardMargin, logTop);
				logTextBox.Size = new Size(cardW, logHeight);
			}
			else
			{
				int logHeight = Math.Max(110, clientH - logTop - 12);
				logTextBox.Location = new Point(cardMargin, logTop);
				logTextBox.Size = new Size(cardW, logHeight);
			}
		}

		private void LoadHardwareAndLibraries()
		{
			// 1. Detect Connected Controllers (Keyboards/Mice Filtered Out!)
			_detectedControllers = _engine.DetectConnectedControllers();
			if (_detectedControllers.Count > 0)
			{
				var c1 = _detectedControllers.FirstOrDefault(c => c.PlayerIndex == 1) ?? _detectedControllers[0];
				deviceGrid.P1Name = c1.Name;
				deviceGrid.P1HwId = string.Format("VID: 0x{0:X4} PID: 0x{1:X4}", c1.VendorId, c1.ProductId);
				deviceGrid.P1Online = c1.IsOnline;

				var c2 = _detectedControllers.FirstOrDefault(c => c.PlayerIndex == 2);
				if (c2 != null)
				{
					deviceGrid.P2Name = c2.Name;
					deviceGrid.P2HwId = string.Format("VID: 0x{0:X4} PID: 0x{1:X4}", c2.VendorId, c2.ProductId);
					deviceGrid.P2Online = c2.IsOnline;
				}
				else
				{
					deviceGrid.P2Name = "Twin USB (Slot 2)";
					deviceGrid.P2HwId = "2-Player Co-op Profile";
					deviceGrid.P2Online = false;
				}
				deviceGrid.Invalidate();

				var devNames = string.Join(", ", _detectedControllers.Select(c => c.Name).Distinct());
				hardwareOptLabel.Text = string.Format("✔ DirectInput 1000 Hz polling, AboveNormal CPU priority & verified {0} mapping ready.", devNames);
				hardwareOptLabel.ForeColor = Color.FromArgb(22, 163, 74);
			}
			else
			{
				hardwareOptLabel.Text = "✔ DirectInput 1000 Hz polling & AboveNormal CPU priority active. Connect gamepad anytime.";
				hardwareOptLabel.ForeColor = Color.FromArgb(22, 163, 74);
			}

			// 2. Scan for Game Libraries
			var libraries = _engine.DetectCommonGameFolders();
			detectedLibraryComboBox.Items.Clear();
			if (libraries.Count > 0)
			{
				detectedLibraryComboBox.Items.Add("-- Select from detected game libraries --");
				foreach (var lib in libraries)
				{
					detectedLibraryComboBox.Items.Add(lib);
				}
				detectedLibraryComboBox.SelectedIndex = 0;
			}
			else
			{
				detectedLibraryComboBox.Items.Add("-- No games detected automatically. Browse manually --");
				detectedLibraryComboBox.SelectedIndex = 0;
				detectedLibraryComboBox.Enabled = false;
			}
		}

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
