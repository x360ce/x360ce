// @under-test: App.v4/Controls/GameDetailsUserControl.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A game handed to the game settings panel, or changed on it, after the panel has closed.</summary>
	/// <remarks>
	/// The games and default-settings lists hand their selection to the panel from work posted to the
	/// interface thread, which runs whenever that thread gets to it, after the window has closed as well.
	/// A closed panel's check boxes belong to no group any more, so the group title update fails on the
	/// missing group. The same panel also stays subscribed to a game it held when it closed, so a later
	/// change to that game runs the panel's property-changed handler against disposed combo boxes.
	/// </remarks>
	[TestClass]
	public class GameDetailsClosedTest
	{
		static UserGame Game()
		{
			return new UserGame { FileName = "game.exe", XInputMask = (int)XInputMask.XInput13_x86 };
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A game handed to a closed panel is ignored")]
		public void A_closed_panel_ignores_a_game()
		{
			Ui.OnUiThread(() =>
			{
				var panel = new GameDetailsUserControl();
				panel.Dispose();
				panel.CurrentItem = Game();
				Assert.IsNull(panel.CurrentItem, "A closed panel took a game it can no longer show.");
			});
		}

		[TestMethod, TestCategory("games")]
		[Description("An open panel still shows the game's mask in the group title")]
		public void An_open_panel_shows_the_mask()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new GameDetailsUserControl())
				{
					panel.CurrentItem = Game();
					var box = panel.Controls.Find("XInputMaskGroupBox", true).Single();
					StringAssert.EndsWith(box.Text, ((int)XInputMask.XInput13_x86).ToString("X8"));
				}
			});
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A later change on a game held by a closed panel is ignored")]
		public void A_closed_panel_ignores_a_later_change_on_its_game()
		{
			Ui.OnUiThreadWatched(() =>
			{
				var panel = new GameDetailsUserControl();
				var game = Game();
				panel.CurrentItem = game;
				panel.Dispose();
				game.EmulationType = (int)EmulationType.Library;
			}, "A change on the game");
		}
	}
}
