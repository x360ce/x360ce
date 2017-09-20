using System;
using System.ComponentModel;
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
        public string Message { get { return _Message; } set { _Message = value; NotifyPropertyChanged("Message"); } }
        string _Message;
        public MessageBoxIcon Status { get { return _Status; } set { _Status = value; NotifyPropertyChanged("Status"); } }
        MessageBoxIcon _Status;

        public Exception Error { get; set; }

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
