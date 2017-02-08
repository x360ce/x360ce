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

		#region Capabilities

		// DInput Device Capabilities.

		public int AxeCount;
		public int ButtonCount;
		public int DriverVersion;
		public int FirmwareRevision;
		public DeviceFlags Flags;
		public int ForceFeedbackMinimumTimeResolution;
		public int ForceFeedbackSamplePeriod;
		public int HardwareRevision;
		public int PovCount;

		///<summary>Gets a value indicating whether this instance is human interface device.</summary>
		public bool IsHumanInterfaceDevice { get; }

		///<summary>Gets the subtype of the device.</summary>
		public int Subtype { get; }

		///<summary>Gets the type of this device.</summary>
		public DeviceType Type { get; }
		
		#endregion

		/// <summary>DInput Device Info.</summary>
		public DeviceInfo HidInfo { get; set; }

		/// <summary>DInput Device Info.</summary>
		public DeviceInfo DevInfo { get; set; }

		#region Ignored Properties

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

		#endregion

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
