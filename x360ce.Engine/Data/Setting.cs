using SharpDX.XInput;
using System;
using System.Linq.Expressions;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class Setting
	{

		[XmlIgnore]
		public bool IsOnline
		{
			get { return _IsOnline; }
			set { _IsOnline = value; ReportPropertyChanged(x => x.IsOnline); }
		}
		bool _IsOnline;

		/// <summary>XInput state converted from X360CE custom DirectInput state.</summary>
		[XmlIgnore]
		public Gamepad XiState;

		/// <summary>
		/// Calculate map completion percent based on controller capabilities.
		/// </summary>
		public int GetCompletion(PadSetting ps, UserDevice ud = null)
		{
			// Xbox 360 Controller have 6 axis. Use device count if less.
			var maxAxis = ud != null && ud.CapAxeCount > 0 && ud.CapAxeCount < 6m ? ud.CapAxeCount : 6m;
			// Xbox 360 Controller have 14 buttons. Use device count if less.
			var maxButtons = ud != null && ud.CapButtonCount > 0 && ud.CapButtonCount < 14m ? ud.CapButtonCount : 14m;
			// Use device motor count.
			var maxMotors = ud != null ? ud.DiActuatorMask : 0m;
			var motorPoints = 0m;
			// If force feedback enabled then...
			if (ps.ForceEnable == "1")
			{
				// Give all available points.
				motorPoints = maxMotors;
			}
			// Count axis points (maximum 6 points).
			var axisPoints = 0m;
			axisPoints += GetAxisMapPoints(ps.LeftThumbAxisX, ps.LeftThumbLeft, ps.LeftThumbRight);
			axisPoints += GetAxisMapPoints(ps.LeftThumbAxisY, ps.LeftThumbUp, ps.LeftThumbDown);
			axisPoints += GetAxisMapPoints(ps.RightThumbAxisX, ps.RightThumbLeft, ps.RightThumbRight);
			axisPoints += GetAxisMapPoints(ps.RightThumbAxisY, ps.RightThumbUp, ps.RightThumbDown);
			axisPoints += GetAxisMapPoints(ps.LeftTrigger);
			axisPoints += GetAxisMapPoints(ps.RightTrigger);
			// Count button points (maximum 14 points).
			var buttonPoints = 0m;
			SettingType type;
			int index;
			if (SettingsConverter.TryParseIniValue(ps.ButtonA, out type, out index)) buttonPoints += 1m;
			if (SettingsConverter.TryParseIniValue(ps.ButtonB, out type, out index)) buttonPoints += 1m;
			if (SettingsConverter.TryParseIniValue(ps.ButtonX, out type, out index)) buttonPoints += 1m;
			if (SettingsConverter.TryParseIniValue(ps.ButtonY, out type, out index)) buttonPoints += 1m;
			if (SettingsConverter.TryParseIniValue(ps.ButtonBack, out type, out index)) buttonPoints += 1m;
			if (SettingsConverter.TryParseIniValue(ps.ButtonStart, out type, out index)) buttonPoints += 1m;
			// If DPad is properly mapped to POV then...
			if (SettingsConverter.TryParseIniValue(ps.DPadDown, out type, out index) && type == SettingType.POV)
			{
				// Add 4 points.
				buttonPoints += 4m;
			}
			else
			{
				// Add point for each button.
				if (SettingsConverter.TryParseIniValue(ps.DPadUp, out type, out index)) buttonPoints += 1m;
				if (SettingsConverter.TryParseIniValue(ps.DPadDown, out type, out index)) buttonPoints += 1m;
				if (SettingsConverter.TryParseIniValue(ps.DPadLeft, out type, out index)) buttonPoints += 1m;
				if (SettingsConverter.TryParseIniValue(ps.DPadRight, out type, out index)) buttonPoints += 1m;
			}
			if (SettingsConverter.TryParseIniValue(ps.LeftThumbButton, out type, out index)) buttonPoints = +1m;
			if (SettingsConverter.TryParseIniValue(ps.RightThumbButton, out type, out index)) buttonPoints = +1m;
			if (SettingsConverter.TryParseIniValue(ps.LeftShoulder, out type, out index)) buttonPoints = +1m;
			if (SettingsConverter.TryParseIniValue(ps.RightShoulder, out type, out index)) buttonPoints = +1m;
			// Calculate completion percent.
			var completion = (int)(100m * (
				(axisPoints + buttonPoints + motorPoints) / (maxAxis + maxButtons + maxMotors)
			));
			return completion;
		}

		decimal GetAxisMapPoints(string axis, string up = null, string down = null)
		{
			var points = 0m;
			SettingType type;
			int index;
			if (SettingsConverter.TryParseIniValue(axis, out type, out index))
			{
				// If proper axis mapped then give full point.
				points += 1m;
			}
			else
			{
				// For every map 0.25 points.
				if (SettingsConverter.TryParseIniValue(up, out type, out index))
					points += 0.25m;
				if (SettingsConverter.TryParseIniValue(down, out type, out index))
					points += 0.25m;
			}
			return points;
		}

		#region INotifyPropertyChanged

		/// <summary>
		/// Use: ReportPropertyChanged(x => x.PropertyName);
		/// </summary>
		void ReportPropertyChanged(Expression<Func<UserDevice, object>> selector)
		{
			var body = (MemberExpression)((UnaryExpression)selector.Body).Operand;
			var name = body.Member.Name;
			ReportPropertyChanged(name);
		}

		#endregion
	}
}
