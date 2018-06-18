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
		public void UpdateCompletion(PadSetting ps, UserDevice ud)
		{
			// Count axis points (maximum 6 points).
			var axisPoints = 0m;
			axisPoints += GetAxisMapPoints(ps.LeftThumbAxisX, ps.LeftThumbLeft, ps.LeftThumbRight);
			axisPoints += GetAxisMapPoints(ps.LeftThumbAxisY, ps.LeftThumbUp, ps.LeftThumbDown);
			axisPoints += GetAxisMapPoints(ps.RightThumbAxisX, ps.RightThumbLeft, ps.RightThumbRight);
			axisPoints += GetAxisMapPoints(ps.RightThumbAxisY, ps.RightThumbUp, ps.RightThumbDown);
			axisPoints += GetAxisMapPoints(ps.LeftTrigger);
			axisPoints += GetAxisMapPoints(ps.RightTrigger);
			// Get required amount of maps in order to complete 100%.
			var axisRequired = (decimal)Math.Min(6, ud.CapAxeCount);
			var buttonsRequired = (decimal)Math.Min(14, ud.CapButtonCount);
			// Calculate completion percent.
			Completion = (int)(100m * (
				// Maximum 40% score if all axis are mapped.
				Math.Min((axisPoints / axisRequired), 0.4m) +
				// Maximum 40% score if all buttons are mapped.
				Math.Min((axisPoints / axisRequired), 0.4m)
			));
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
