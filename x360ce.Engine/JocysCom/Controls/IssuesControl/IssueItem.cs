using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JocysCom.ClassLibrary.Controls.IssuesControl
{
	public class IssueItem : INotifyPropertyChanged
	{

		public IssueItem(int orderId = 0)
		{
			Description = "";
			FixName = "Fix";
			OrderId = orderId;
		}

		public event EventHandler<EventArgs> Checking;
		public event EventHandler<EventArgs> Checked;

		public event EventHandler<EventArgs> Fixing;
		public event EventHandler<EventArgs> Fixed;

		public Exception LastException;

		public IssueStatus Status { get; private set; }

		void SetStatus(IssueStatus status)
		{
			Status = status;
			OnPropertyChanged(nameof(Status));
		}

		/// <summary>
		/// Order ID will define in which order issues will be checked.
		/// </summary>
		public int OrderId
		{
			get { return _OrderId; }
			set { _OrderId = value; OnPropertyChanged(); }
		}
		private int _OrderId ;

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

		public Uri MoreInfo
		{
			get { return _MoreInfo; }
			set { _MoreInfo = value; OnPropertyChanged(); }
		}
		private Uri _MoreInfo;

		public int FixType;

		public string FixName
		{
			get { return _FixName; }
			set { _FixName = value; OnPropertyChanged(); }
		}
		string _FixName;

		[DefaultValue(true)]
		public bool IsEnabled
		{
			get { return _IsEnabled; }
			set { _IsEnabled = value; OnPropertyChanged(); }
		}
		bool _IsEnabled = true;

		public IssueSeverity? Severity
		{
			get { return _Severity; }
			set { _Severity = value; OnPropertyChanged(); }
		}
		IssueSeverity? _Severity;

		public virtual void CheckTask()
		{
			throw new NotImplementedException();
		}

		public void Check()
		{
			Debug.WriteLine("---> {0}: {1}", "Check", GetType());
			SetStatus(IssueStatus.Checking);
			Checking?.Invoke(this, new EventArgs());
			LastException = null;
			try
			{
				if (IsEnabled)
					CheckTask();
			}
			catch (Exception ex)
			{
				LastException = ex;
				//throw;
			}
			finally
			{
				SetStatus(IssueStatus.Idle);
				Checked?.Invoke(this, new EventArgs());
			}
		}

		public virtual void FixTask()
		{
			throw new NotImplementedException();
		}

		object FixLock = new object();

		public void Fix()
		{
			lock (FixLock)
			{
				// Do not allow for fix to run second time.
				if (Status == IssueStatus.Fixing)
					return;
			}
			SetStatus(IssueStatus.Fixing);
			Fixing?.Invoke(this, new EventArgs());
			LastException = null;
			try
			{

				FixTask();
			}
			catch (Exception ex)
			{
				LastException = ex;
				//throw;
			}
			finally
			{
				SetStatus(IssueStatus.Idle);
				Fixed?.Invoke(this, new EventArgs());
			}

		}

		public void SetSeverity(IssueSeverity severity, int fixType = 0, string description = null)
		{
			var update = !Severity.HasValue || Severity.Value != severity || Description != description;
			if (update)
			{
				FixType = fixType;
				Severity = severity;
				if (description != null)
					Description = description;
			}
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
