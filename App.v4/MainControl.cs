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
			FillBuletImageList();
			MainTabControl.Selected += (s, e) => PadOn(e.TabPage)?.HoldTables();
			MainTabControl.SelectedIndexChanged += (s, e) => PadOn(MainTabControl.SelectedTab)?.ReleaseTables();
			Pad1TabPage.Text = "Controller 1";
			Pad2TabPage.Text = "Controller 2";
			Pad3TabPage.Text = "Controller 3";
			Pad4TabPage.Text = "Controller 4";
			GamesToolStrip_Resize(null, null);
			ControlsHelper.ApplyBorderStyle(GamesToolStrip);
			// Other option.
			TestButton.Visible = SettingsManager.Options.ShowTestButton;
			ControlPages = new[] { Pad1TabPage, Pad2TabPage, Pad3TabPage, Pad4TabPage };
			foreach (var item in ControlPages)
				item.ImageKey = "bullet_square_glass_grey.png";
			BuiltPadControls = new PadControl[ControlPages.Length];
		}

		List<TabPage> PadTabPages => new List<TabPage> { Pad1TabPage, Pad2TabPage, Pad3TabPage, Pad4TabPage };

		public int ControllerIndex => PadTabPages.IndexOf(MainTabControl.SelectedTab);

		public AboutControl ControlAbout;
		/// <summary>The controller pages, once they hold their settings; null before.</summary>
		public PadControl[] PadControls;
		/// <summary>The controller pages as built, before they are given their settings.</summary>
		internal PadControl[] BuiltPadControls;
		public TabPage[] ControlPages;

		/// <summary>Builds the controller page of a tab, 0 to 3. Windows makes none of it yet, and the page is given its settings later.</summary>
		internal void BuildPad(int index)
		{
			var mapTo = (MapTo)(index + 1);
			BuiltPadControls[index] = new PadControl(mapTo)
			{
				Name = string.Format("ControlPad{0}", (int)mapTo),
				Dock = DockStyle.Fill
			};
			Program.StartupTrace.Mark("BuildPages: pad " + (index + 1) + " built");
			ControlPages[index].Controls.Add(BuiltPadControls[index]);
			BuiltPadControls[index].HoldTables();
			BuiltPadControls[index].InitPadControl();
			BuiltPadControls[index].ReleaseTables();
			Program.StartupTrace.Mark("BuildPages: pad " + (index + 1) + " initialised");
		}

		/// <summary>The controller page on a tab, or null on any other tab or before the page is built.</summary>
		internal PadControl PadOn(TabPage page)
		{
			var index = Array.IndexOf(ControlPages, page);
			return index < 0 ? null : BuiltPadControls[index];
		}

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

		private void GamesToolStrip_Resize(object sender, EventArgs e)
		{
			GameToCustomizeComboBox.AutoSize = false;
			var width = GamesToolStrip.DisplayRectangle.Width;
			foreach (ToolStripItem tsi in GamesToolStrip.Items)
			{
				if (!(tsi == GameToCustomizeComboBox))
				{
					width -= tsi.Width;
					width -= tsi.Margin.Horizontal;
				}
			}
			GameToCustomizeComboBox.Width = Math.Max(0, width - GameToCustomizeComboBox.Margin.Horizontal);
			// Resolve disappearing.
			GamesToolStrip.PerformLayout();
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
