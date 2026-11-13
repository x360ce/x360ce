using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Properties;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	/// <summary>The Game row and the tabs: everything in the main window under its header and above its status bar.</summary>
	/// <remarks>
	/// The window appears with its header and status bar first, and this is built while it says it is loading, then added to it
	/// (<see cref="MainForm.Current"/>).
	/// </remarks>
	public partial class MainControl : UserControl
	{
		public MainControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			// The room round the controls on the pages that scale themselves, given once the pages are scaled: Windows Forms
			// scales such a control's own margin again each time a table round it resumes, so the designer's would be scaled
			// several times over.
			RecorderPanel.Padding = AppHelper.ScaledPadding(12, 4, 12, 12);
			OptionsPanel.RemotePortNumericUpDown.Margin = AppHelper.ScaledPadding(4, 4, 4, 4);
			FillBuletImageList();
			// Before the tab is shown, so it never shows the controller of the tab before, and drawn once the switch is over.
			MainTabControl.Selecting += (s, e) =>
			{
				if (Array.IndexOf(ControlPages, e.TabPage) >= 0 && HoldDrawing())
					BeginInvoke((Action)ReleaseDrawing);
				ShowPadOn(e.TabPage);
			};
			MainTabControl.Selected += (s, e) => PadOn(e.TabPage)?.HoldTables();
			MainTabControl.SelectedIndexChanged += (s, e) => PadOn(MainTabControl.SelectedTab)?.ReleaseTables();
			Pad1TabPage.Text = "Controller 1";
			Pad2TabPage.Text = "Controller 2";
			Pad3TabPage.Text = "Controller 3";
			Pad4TabPage.Text = "Controller 4";
			GamesToolStrip_Layout(null, null);
			ControlsHelper.ApplyBorderStyle(GamesToolStrip);
			// Other option.
			TestButton.Visible = SettingsManager.Options.ShowTestButton;
			ControlPages = new[] { Pad1TabPage, Pad2TabPage, Pad3TabPage, Pad4TabPage };
			foreach (var item in ControlPages)
				item.ImageKey = "bullet_square_glass_grey.png";
			BuiltPadControls = new PadControl[1];
		}

		List<TabPage> PadTabPages => new List<TabPage> { Pad1TabPage, Pad2TabPage, Pad3TabPage, Pad4TabPage };

		public int ControllerIndex => PadTabPages.IndexOf(MainTabControl.SelectedTab);

		public AboutControl ControlAbout;
		/// <summary>The controller page, once it holds its settings; null before.</summary>
		/// <remarks>One page serves the four Controller tabs (<see cref="ShowPadOn"/>), so this holds one.</remarks>
		public PadControl[] PadControls;
		/// <summary>The controller page as built, before it is given its settings.</summary>
		internal PadControl[] BuiltPadControls;
		public TabPage[] ControlPages;

		/// <summary>Builds the controller page, on the first Controller tab. Windows makes none of it yet, and the page is given its settings later.</summary>
		internal void BuildPad()
		{
			BuiltPadControls[0] = new PadControl(MapTo.Controller1)
			{
				Name = string.Format("ControlPad{0}", (int)MapTo.Controller1),
				Dock = DockStyle.Fill
			};
			Program.StartupTrace.Mark("BuildPages: pad built");
			ControlPages[0].Controls.Add(BuiltPadControls[0]);
			BuiltPadControls[0].HoldTables();
			BuiltPadControls[0].InitPadControl();
			BuiltPadControls[0].ReleaseTables();
			Program.StartupTrace.Mark("BuildPages: pad initialised");
		}

		/// <summary>The controller page, when it is on this tab; null on any other tab or before the page is built.</summary>
		internal PadControl PadOn(TabPage page)
		{
			var pad = BuiltPadControls[0];
			return Array.IndexOf(ControlPages, page) < 0 || pad == null || pad.Parent != page ? null : pad;
		}

		/// <summary>Moves the controller page onto a Controller tab and shows that tab's controller on it.</summary>
		/// <remarks>
		/// The page's windows move with it rather than being made again. Before the page holds its settings it only moves:
		/// it is given the tab's controller once it has them (<see cref="MainForm"/>'s loading).
		/// </remarks>
		internal void ShowPadOn(TabPage page)
		{
			var index = Array.IndexOf(ControlPages, page);
			var pad = BuiltPadControls[0];
			if (index < 0 || pad == null)
				return;
			if (pad.Parent != page)
				page.Controls.Add(pad);
			if (PadControls != null)
				pad.SwitchTo((MapTo)(index + 1));
		}

		/// <summary>Whether the move of the controller page back to the tab shown is posted already.</summary>
		bool _PadBackPosted;

		/// <summary>Puts the controller page on a Controller tab the AI door reads, with that tab's controller, and back on the tab shown once the door's call is over.</summary>
		/// <remarks>
		/// The door reads and changes every Controller tab, shown or not, as it did when each tab had a page of its own. It walks
		/// the window on the interface thread, so the move back, posted to that thread, runs once the call has finished, before
		/// anything is drawn. Called for every control the door walks, so it returns at once for any other.
		/// </remarks>
		internal void PadForDoor(Control control)
		{
			var page = control as TabPage;
			if (page == null || PadControls == null || Program.IsClosing)
				return;
			var pad = PadControls[0];
			if (pad.Parent == page || Array.IndexOf(ControlPages, page) < 0)
				return;
			// The page leaves the tab shown for the length of the call, which is not to be seen.
			if (pad.Parent == MainTabControl.SelectedTab)
				HoldDrawing();
			ShowPadOn(page);
			if (_PadBackPosted || !IsHandleCreated)
				return;
			_PadBackPosted = true;
			BeginInvoke((Action)(() =>
			{
				_PadBackPosted = false;
				ShowPadOn(MainTabControl.SelectedTab);
				ReleaseDrawing();
			}));
		}

		/// <summary>Whether the tabs' drawing is held while the controller page moves.</summary>
		bool _DrawingHeld;

		/// <summary>Holds the drawing of the Game row and the tabs while the controller page moves and takes another controller's values.</summary>
		/// <remarks>
		/// Moved and filled while shown, the page was drawn in parts: it went blank for up to 90 ms and came back a control at
		/// a time. Locked, nothing on this control is drawn until <see cref="ReleaseDrawing"/>, which draws it once.
		/// Not by WM_SETREDRAW: on Windows' own tab control it stops only the tab strip, and on a plain window it works by
		/// marking the window hidden, so a screen reader selecting a Controller tab was told the tab could not be selected.
		/// Windows lets one window be locked at a time; when another program holds the lock, the switch is drawn as it comes.
		/// </remarks>
		/// <returns>True when this call held it, and so must see it released.</returns>
		bool HoldDrawing()
		{
			if (_DrawingHeld || !IsHandleCreated)
				return false;
			_DrawingHeld = LockWindowUpdate(Handle);
			return _DrawingHeld;
		}

		/// <summary>Lets the Game row and the tabs draw again, and draws them once, whole.</summary>
		void ReleaseDrawing()
		{
			if (!_DrawingHeld)
				return;
			_DrawingHeld = false;
			LockWindowUpdate(IntPtr.Zero);
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool LockWindowUpdate(IntPtr hWndLock);

		/// <summary>Builds the About page.</summary>
		internal void BuildAbout()
		{
			ControlAbout = new AboutControl
			{
				Dock = DockStyle.Fill
			};
			AboutTabPage.Controls.Add(ControlAbout);
			Program.StartupTrace.Mark("BuildPages: about built");
		}

		/// <summary>Fills the tab image list with the status lights and the icons of the theme in use.</summary>
		/// <remarks>
		/// Filled again when the theme changes: a list keeps copies of its images, so it does not follow the
		/// resources. Each image is put in place of the one under its name, so a tab showing it never goes blank,
		/// the lights of two colours (<see cref="StatusImageKey"/>) among them.
		/// </remarks>
		internal void FillBuletImageList()
		{
			// A list draws every image at its own size, so it is set to the screen's before anything goes in.
			// Setting the size empties the list, so it is set only while it differs.
			if (BuletImageList.ImageSize != SystemInformation.SmallIconSize)
				BuletImageList.ImageSize = SystemInformation.SmallIconSize;
			SetBuletImage("bullet_square_glass_red.png", AppHelper.GetStatusIcon(AppHelper.StatusRed));
			SetBuletImage("bullet_square_glass_amber.png", AppHelper.GetStatusIcon(AppHelper.StatusAmber));
			SetBuletImage("bullet_square_glass_orange.png", AppHelper.GetStatusIcon(AppHelper.StatusOrange));
			SetBuletImage("bullet_square_glass_green.png", AppHelper.GetStatusIcon(AppHelper.StatusGreen));
			SetBuletImage("bullet_square_glass_blue.png", AppHelper.GetStatusIcon(AppHelper.StatusBlue));
			SetBuletImage("bullet_square_glass_grey.png", AppHelper.GetStatusIcon(AppHelper.StatusOff));
			SetBuletImage("ok_32x32.png", Resources.ok_32x32);
			SetBuletImage("ok_off_32x32.png", Resources.ok_off_32x32);
			SetBuletImage("fix_32x32.png", Resources.fix_32x32);
			SetBuletImage("fix_off_32x32.png", Resources.fix_off_32x32);
			SetBuletImage("refresh_32x32.png", Resources.refresh_32x32);
			foreach (var key in BuletImageList.Images.Keys.Cast<string>().Where(x => x.Contains("|")).ToArray())
			{
				var colours = key.Split('|');
				SetBuletImage(key, AppHelper.GetStatusIcon(colours[0], colours[1]));
			}
		}

		/// <summary>Puts an image in the tab image list under its name, in place of the one there.</summary>
		void SetBuletImage(string key, Image image)
		{
			var index = BuletImageList.Images.IndexOfKey(key);
			if (index < 0)
				BuletImageList.Images.Add(key, image);
			else
				BuletImageList.Images[index] = image;
		}

		/// <summary>The name of a light in that colour, adding it to the list if it is new.</summary>
		/// <remarks>
		/// Tabs draw from an image list by name, so a colour mixed at run time has to be put in it before
		/// it can be shown. The colour itself is the name, which means each one is made once however many
		/// tabs ask for it.
		/// </remarks>
		internal string StatusImageKey(string left, string right)
		{
			var key = left + "|" + right;
			if (!BuletImageList.Images.ContainsKey(key))
				BuletImageList.Images.Add(key, AppHelper.GetStatusIcon(left, right));
			return key;
		}

		#region Game row

		internal void InitGameToCustomizeComboBox()
		{
			GameToCustomizeComboBox.ComboBox.DataSource = SettingsManager.UserGames.Items;
			// Make sure that X360CE.exe is on top.
			GameToCustomizeComboBox.ComboBox.DisplayMember = nameof(x360ce.Engine.Data.UserGame.DisplayName);
			// Two games listed under one name are told apart by their folders.
			GameToCustomizeComboBox.ComboBox.FormattingEnabled = true;
			GameToCustomizeComboBox.ComboBox.Format += GameToCustomizeComboBox_Format;
			GameToCustomizeComboBox.SelectedIndexChanged += GameToCustomizeComboBox_SelectedIndexChanged;
			// Select game by manually trigger event.
			Global.SelectOpenGame();
		}

		private void GameToCustomizeComboBox_Format(object sender, ListControlConvertEventArgs e)
		{
			var game = e.ListItem as UserGame;
			if (game != null)
				e.Value = SettingsManager.DisplayNameInList(game);
		}

		internal void GameToCustomizeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var game = (UserGame)GameToCustomizeComboBox.ComboBox.SelectedItem;
			SettingsManager.UpdateCurrentGame(game);
		}

		/// <summary>
		/// Gives the game list what the tool bar's other items leave, each time the tool bar lays itself out: when it is
		/// resized, and when its items change size or are shown or hidden, as the theme, a screen of another scale and the
		/// options make them, so no item is pushed into the overflow menu. The list ends where neighbours stand apart, 8 pixels
		/// at 100 %, before the first button to its right.
		/// </summary>
		private void GamesToolStrip_Layout(object sender, LayoutEventArgs e)
		{
			GameToCustomizeComboBox.AutoSize = false;
			var width = GamesToolStrip.DisplayRectangle.Width;
			ToolStripItem next = null;
			foreach (ToolStripItem tsi in GamesToolStrip.Items)
			{
				// A hidden item takes no room.
				if (tsi == GameToCustomizeComboBox || !tsi.Available)
					continue;
				// As wide as it stands on the bar: an item in the overflow menu reads the menu's width.
				width -= tsi.GetPreferredSize(Size.Empty).Width;
				width -= tsi.Margin.Horizontal;
				// The items on the right stand from the bar's end in their order, so the last of them is the list's neighbour.
				if (tsi.Alignment == ToolStripItemAlignment.Right)
					next = tsi;
			}
			var gap = (int)Math.Round(8 * ControlsHelper.DpiScale);
			// The margins between the list and its neighbour are given to the gap.
			width -= GameToCustomizeComboBox.Margin.Left + gap - (next?.Margin.Left ?? GameToCustomizeComboBox.Margin.Right);
			width = Math.Max(0, width);
			// A new width lays the tool bar out again, which finds the same width and stops.
			if (GameToCustomizeComboBox.Width != width)
				GameToCustomizeComboBox.Width = width;
		}

		private void SaveAllButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.Save();
		}

		private void AddGameButton_Click(object sender, EventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				MainTabControl.SelectedTab = GamesTabPage;
				Application.DoEvents();
				GameSettingsPanel.AddNewGame();
			});
		}

		private void TestButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.DebugPanel.ShowPanel();
		}

		#endregion

		#region Tabs

		private bool HelpInit = false;

		internal void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (MainTabControl.SelectedTab == HelpTabPage && !HelpInit)
			{
				// Move this here so interface will load one second faster.
				HelpInit = true;
				AppHelper.LoadHelp(HelpRichTextBox, AppHelper.HelpV4Resource);
			}
			else if (MainTabControl.SelectedTab == SettingsTabPage)
			{
				var o = SettingsManager.Options;
				if (o.InternetFeatures && o.InternetAutoLoad)
				{
					//SettingsDatabasePanel.RefreshGrid(true);
				}
			}
			var tab = MainTabControl.SelectedTab;
			if (tab != null)
				MainForm.Current.SetHeaderSubject(tab.Text);
		}

		public void ShowTab(bool show, TabPage page)
		{
			var tc = MainTabControl;
			// If must hide then...
			if (!show && tc.TabPages.Contains(page))
			{
				// Hide and return.
				tc.TabPages.Remove(page);
				return;
			}
			// If must show then..
			if (show && !tc.TabPages.Contains(page))
			{
				// Create list of tabs to maintain same order when hiding and showing tabs.
				var tabs = new List<TabPage>() { ProgramsTabPage, SettingsTabPage, DevicesTabPage };
				// Get index of always displayed tab.
				var index = tc.TabPages.IndexOf(GamesTabPage);
				// Get tabs in front of tab which must be inserted.
				var tabsBefore = tabs.Where(x => tabs.IndexOf(x) < tabs.IndexOf(page));
				// Count visible tabs.
				var countBefore = tabsBefore.Count(x => tc.TabPages.Contains(x));
				tc.TabPages.Insert(index + countBefore + 1, page);
			}
		}

		public void ShowProgramsTab(bool show)
		{
			ShowTab(show, ProgramsTabPage);
		}

		public void ShowSettingsTab(bool show)
		{
			ShowTab(show, SettingsTabPage);
		}

		public void ShowDevicesTab(bool show)
		{
			ShowTab(show, DevicesTabPage);
		}

		#endregion
	}
}
