using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace x360ce.App
{
	public class LogItem : EventArgs, INotifyPropertyChanged
	{
		public LogItem()
		{
			Date = DateTime.Now;
		}

		public DateTime Date { get; set; }
		public TimeSpan Delay { get; set; }
		public string Message
		{
			get { return _Message; }
			set
			{
				_Message = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(MessageDisplay));
			}
		}
		string _Message;

		public string MessageDisplay
			=> JocysCom.ClassLibrary.Text.Helper.CropLines(Message);

		public MessageBoxIcon Status { get { return _Status; } set { _Status = value; OnPropertyChanged(); } }
		MessageBoxIcon _Status;

		public Exception Error { get; set; }

		#region ■ INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
