using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using x360ce.App.Issues;

namespace x360ce.App
{
	public class WarningItem : INotifyPropertyChanged
	{

		public event EventHandler<EventArgs> FixApplied;

		internal void RaiseFixApplied()
		{
			FixApplied?.Invoke(this, new EventArgs());
		}

		public string Name
		{
			get { return _Name; }
			set { _Name = value; OnPropertyChanged(); }
		}
		string _Name;

		public string Description
		{
			get { return _Description; }
			set { _Description = value; OnPropertyChanged(); }
		}
		string _Description;

		public string FixName
		{
			get { return _FixName; }
			set { _FixName = value; OnPropertyChanged(); }
		}
		string _FixName;

		public IssueSeverity Severity
		{
			get { return _Severity; }
			set { _Severity = value; OnPropertyChanged(); }
		}
		IssueSeverity _Severity;

		public virtual void Fix() { throw new NotImplementedException(); }

		public virtual void Check() { throw new NotImplementedException(); }

		#region INotifyPropertyChanged

		internal object checkLock = new object();

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
