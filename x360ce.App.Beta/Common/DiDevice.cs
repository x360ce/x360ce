using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Xml.Serialization;
using x360ce.Engine;

namespace x360ce.App
{
	public class DiDevice : INotifyPropertyChanged
	{
		/// <summary>DInput Device Instance.</summary>
		public DeviceInstance Instance { get; set; }

		/// <summary>DInput Device Capabilities.</summary>
		public Capabilities Capabilities { get; set; }

		/// <summary>DInput Device Info.</summary>
		public DeviceInfo HidInfo { get; set; }

		/// <summary>DInput Device Info.</summary>
		public DeviceInfo DevInfo { get; set; }

		// =================

		[XmlIgnore]
		/// <summary>Previous DInput Device Instance.</summary>
		public DeviceInstance InstanceOld { get; set; }

		/// <summary>DInput Device State.</summary>
		[XmlIgnore]
		public Joystick Device;


		[XmlIgnore]
		public bool IsOnline
		{
			get { return _IsOnline; }
			set { _IsOnline = value; ReportPropertyChanged(x => x.IsOnline); }
		}
		bool _IsOnline;

		[XmlIgnore]
		public string InstanceId
		{
			get
			{
				return EngineHelper.GetID(InstanceGuid);
			}
		}

		[XmlIgnore]
		public string VendorName
		{
			get
			{
				var o = HidInfo;
				return o == null ? "" : o.Manufacturer;
			}
		}

		[XmlIgnore]
		public string ProductName
		{
			get
			{
				var o = Instance;
				return o == null ? "" : o.ProductName;
			}
		}

		[XmlIgnore]
		public Guid InstanceGuid
		{
			get
			{
				var o = Instance;
				return o == null ? Guid.Empty : o.InstanceGuid;
			}
		}

		[XmlIgnore]
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
