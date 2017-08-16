using System;
using System.ComponentModel;
using x360ce.Engine;

namespace x360ce.App
{
	public class CloudItem: INotifyPropertyChanged
	{
		public CloudAction Action { get { return _Action; } set { _Action = value; NotifyPropertyChanged("Action"); } }
		CloudAction _Action;

		public CloudState State { get { return _State; } set { _State = value; NotifyPropertyChanged("State"); } }
		CloudState _State;

		public DateTime Date { get; set; }

		public object Item { get; set; }
		public string Description
		{
			get
			{
				var dm = Item as IDisplayName;
				var name = dm == null ? string.Format("{0}", Item) :
					string.Format("{0}: {1}", Item.GetType().Name, dm.DisplayName);
				return name;
			}
		}


		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			ev(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}

}
