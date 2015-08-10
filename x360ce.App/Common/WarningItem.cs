using System;
using System.ComponentModel;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Issues;

namespace x360ce.App
{
	public class WarningItem : INotifyPropertyChanged
	{

		public string Name
		{
			get { return _Name; }
			set { _Name = value; NotifyPropertyChanged("Name"); }
		}
		string _Name;

		public string Description
		{
			get { return _Description; }
			set { _Description = value; NotifyPropertyChanged("Description"); }
		}
		string _Description;

		public string FixName
		{
			get { return _FixName; }
			set { _FixName = value; NotifyPropertyChanged("FixName"); }
		}
		string _FixName;

		public IssueSeverity Severity
		{
			get { return _Severity; }
			set { _Severity = value; NotifyPropertyChanged("Severity"); }
		}
		IssueSeverity _Severity;

		public virtual void Fix() { throw new NotImplementedException(); }

		public virtual void Check() { throw new NotImplementedException(); }

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName = "")
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			ev(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
