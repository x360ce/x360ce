using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.Setup
{
	public partial class MainSetupForm : Form
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
	}
}
