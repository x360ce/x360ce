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
