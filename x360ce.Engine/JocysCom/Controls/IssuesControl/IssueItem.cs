using System;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Controls.IssueControl
{
    public class IssueItem : INotifyPropertyChanged
    {

        public IssueItem()
        {
            Description = "";
            FixName = "Fix";
        }

        public event EventHandler<EventArgs> Fixing;
        public event EventHandler<EventArgs> Fixed;

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

        public int FixType;

        public string FixName
        {
            get { return _FixName; }
            set { _FixName = value; NotifyPropertyChanged("FixName"); }
        }
        string _FixName;

        public IssueSeverity? Severity
        {
            get { return _Severity; }
            set { _Severity = value; NotifyPropertyChanged("Severity"); }
        }
        IssueSeverity? _Severity;

        public virtual void Fix() { throw new NotImplementedException(); }

        public virtual void Check() { throw new NotImplementedException(); }

        public void SetSeverity(IssueSeverity severity, int fixType = 0, string description = "")
        {
            var update = !Severity.HasValue || Severity.Value != severity || Description != description;
            if (update)
            {
                FixType = fixType;
                Severity = severity;
                Description = description;
            }
        }

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
