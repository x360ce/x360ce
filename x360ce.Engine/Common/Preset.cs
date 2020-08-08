using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace x360ce.Engine
{
	public class Preset : INotifyPropertyChanged
	{

		public string VendorName
		{
			get { return _VendorName; }
			set { _VendorName = value; OnPropertyChanged(); }
		}
		string _VendorName;

		public string ProductName
		{
			get { return _ProductName; }
			set { _ProductName = value; OnPropertyChanged(); }
		}
		string _ProductName;

		public Guid ProductGuid
		{
			get { return _ProductGuid; }
			set { _ProductGuid = value; OnPropertyChanged(); }
		}
		Guid _ProductGuid;

		public Guid PadSettingChecksum
		{
			get { return _PadSettingChecksum; }
			set { _PadSettingChecksum = value; OnPropertyChanged(); }
		}
		Guid _PadSettingChecksum;

		public int Users
		{
			get { return _Users; }
			set { _Users = value; OnPropertyChanged(); }
		}
		int _Users;

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		internal object checkLock = new object();

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
