using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public class Preset : INotifyPropertyChanged
	{

		string _VendorName;
		public string VendorName
		{
			get { return _VendorName; }
			set { _VendorName = value; NotifyPropertyChanged("VendorName"); }
		}

		string _ProductName;
		public string ProductName
		{
			get { return _ProductName; }
			set { _ProductName = value; NotifyPropertyChanged("ProductName"); }
		}

		Guid _ProductGuid;
		public Guid ProductGuid
		{
			get { return _ProductGuid; }
			set { _ProductGuid = value; NotifyPropertyChanged("ProductGuid"); }
		}

		Guid _PadSettingChecksum;
		public Guid PadSettingChecksum
		{
			get { return _PadSettingChecksum; }
			set { _PadSettingChecksum = value; NotifyPropertyChanged("PadSettingChecksum"); }
		}

		int _Users;
		public int Users
		{
			get { return _Users; }
			set { _Users = value; NotifyPropertyChanged("Users"); }
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		internal object checkLock = new object();

		private void NotifyPropertyChanged(string propertyName = "")
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			ev(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
