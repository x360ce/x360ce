using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Issues
{
	/// <summary>Xbox controllers mapped through DirectInput, which Windows feeds only while this program is in front.</summary>
	/// <remarks>
	/// Such a controller works on this program's pages and does nothing once the game is in front, with nothing on the
	/// screen saying why (<see cref="DInput.XInputPlaces.ReadsOnlyInFront"/>). Its Raw Input row keeps working, so the fix
	/// maps that row on the same controller tab, where it starts with the DirectInput row's settings, and takes the
	/// DirectInput row off. Named only where the controller has a Raw Input row to move to.
	/// </remarks>
	public class XboxDirectInputIssue : IssueItem
	{
		public XboxDirectInputIssue() : base()
		{
			Name = "Xbox Controller Through DirectInput";
			FixName = "Use Raw Input";
		}

		/// <summary>A mapping to move: its game, its DirectInput row and the controller's Raw Input row.</summary>
		public class Move
		{
			public UserGame Game;
			public UserSetting Setting;
			public UserDevice RawInput;
		}

		/// <summary>Every row on a controller tab of an Xbox controller read through DirectInput that has a Raw Input row.</summary>
		public static Move[] Moves()
		{
			var devices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			var games = SettingsManager.UserGames.ItemsToArraySynchronized();
			var moves = new List<Move>();
			foreach (var setting in SettingsManager.UserSettings.ItemsToArraySynchronized())
			{
				if (setting.MapTo < (int)MapTo.Controller1 || setting.MapTo > (int)MapTo.Controller4)
					continue;
				var ud = devices.FirstOrDefault(x => x.InstanceGuid == setting.InstanceGuid);
				if (!DInput.XInputPlaces.ReadsOnlyInFront(ud))
					continue;
				var twin = ud.RawInputTwin(devices);
				var game = games.FirstOrDefault(x => string.Equals(x.FileName, setting.FileName, StringComparison.OrdinalIgnoreCase));
				if (twin != null && game != null)
					moves.Add(new Move { Game = game, Setting = setting, RawInput = twin });
			}
			return moves.ToArray();
		}

		public override void CheckTask()
		{
			var moves = Moves();
			if (moves.Length == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var named = moves.Select(x => string.Format("    {0}: {1}, {2}",
				string.IsNullOrEmpty(x.Game.FileProductName) ? x.Game.FileName : x.Game.FileProductName,
				Attributes.GetDescription((MapTo)x.Setting.MapTo),
				string.IsNullOrEmpty(x.RawInput.ProductName) ? x.RawInput.InstanceName : x.RawInput.ProductName)).ToArray();
			SetSeverity(IssueSeverity.Moderate, 0,
				"Windows lets only the program in front read an Xbox controller through DirectInput, so "
				+ (moves.Length == 1 ? "this controller works here and stops" : "these controllers work here and stop")
				+ " once a game is in front. Use Raw Input moves "
				+ (moves.Length == 1 ? "it to its Raw Input row" : "each to its Raw Input row")
				+ ", which keeps working, with the same settings."
				+ Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, named));
		}

		public override void FixTask()
		{
			// The rows are bound to the controller tabs' lists, so they change on the interface thread.
			ControlsHelper.Invoke(() =>
			{
				Apply(Moves(), SettingsManager.Options.HidGuardianConfigureAutomatically);
				SettingsManager.Current.RaiseSettingsChanged(null);
			});
		}

		/// <summary>Maps each controller's Raw Input row where its DirectInput row was, and takes the DirectInput row off.</summary>
		/// <param name="moves">The mappings to move (<see cref="Moves"/>).</param>
		/// <param name="hidGuardian">Whether HID Guardian hides and shows the devices as their mappings change.</param>
		public static void Apply(Move[] moves, bool hidGuardian)
		{
			foreach (var move in moves)
			{
				var tab = (MapTo)move.Setting.MapTo;
				// The Raw Input row starts with the settings of its twin on this tab: the row it replaces.
				SettingsManager.MapGamePadDevices(move.Game, tab, new[] { move.RawInput }, hidGuardian, true);
				SettingsManager.UnMapGamePadDevices(move.Game, move.Setting, hidGuardian);
			}
		}
	}
}
