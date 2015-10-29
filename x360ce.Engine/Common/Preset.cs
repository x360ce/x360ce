using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public class PadDefaultPreset: INotifyPropertyChanged
	{
		public string ProductName;

		public Guid PadSettingChecksum;

		public int Users;

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
