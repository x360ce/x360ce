using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using x360ce.Engine;

namespace x360ce.App
{
	public class DiDevice : INotifyPropertyChanged
	{
		/// <summary>DInput Device State.</summary>
		public Joystick Device;
		/// <summary>DInput Device Info.</summary>
		public DeviceInfo HidInfo;
		/// <summary>DInput Device Info.</summary>
		public DeviceInfo DevInfo;
		/// <summary>DInput Device Instance.</summary>
		public DeviceInstance Instance;
		/// <summary>Previous DInput Device Instance.</summary>
		public DeviceInstance InstanceOld;

		public bool IsOnline
		{
			get { return _IsOnline; }
			set { _IsOnline = value; ReportPropertyChanged(x => x.IsOnline); }
		}
		bool _IsOnline;

		public string InstanceId
		{
			get
			{
				return EngineHelper.GetID(InstanceGuid);
			}
		}

		public string VendorName
		{
			get
			{
				var o = HidInfo;
				return o == null ? "" : o.Manufacturer;
			}
		}

		public string ProductName
		{
			get
			{
				var o = Instance;
				return o == null ? "" : o.ProductName;
			}
		}

		public Guid InstanceGuid
		{
			get
			{
				var o = Instance;
				return o == null ? Guid.Empty : o.InstanceGuid;
			}
		}

		public string DeviceId
		{
			get
			{
				var o = HidInfo;
				return o == null ? "" : o.DeviceId;
			}
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Use: ReportPropertyChanged(x => x.PropertyName);
		/// </summary>
		void ReportPropertyChanged(Expression<Func<DiDevice, object>> selector)
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			var body = (MemberExpression)((UnaryExpression)selector.Body).Operand;
			var name = body.Member.Name;
			ev(this, new PropertyChangedEventArgs(name));
		}

		#endregion



	}
}
