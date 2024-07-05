using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Controls.IssuesControl
{
	public class IssueItem : INotifyPropertyChanged
	{
		public IssueItem()
		{
			Description = "";
			FixName = "Fix";
		}

		public IssueItem(int orderId)
		{
			Description = "";
			FixName = "Fix";
			OrderId = orderId;
		}

		public event EventHandler<EventArgs> Checking;
		public event EventHandler<EventArgs> Checked;

		public event EventHandler<EventArgs> Fixing;
		public event EventHandler<EventArgs> Fixed;

		[XmlIgnore, JsonIgnore]
		public Exception LastException;

		public IssueStatus Status { get => _Status; set => SetProperty(ref _Status, value); }
		IssueStatus _Status;

		/// <summary>
		/// Order ID will define in which order issues will be checked.
		/// </summary>
		public int OrderId { get => _OrderId; set => SetProperty(ref _OrderId, value); }
		private int _OrderId;

		public string Name { get => _Name; set => SetProperty(ref _Name, value); }
		string _Name;

		public string Description { get => _Description; set => SetProperty(ref _Description, value); }
		string _Description;

		[XmlIgnore, JsonIgnore]
		public Uri MoreInfo { get => _MoreInfo; set => SetProperty(ref _MoreInfo, value); }
		private Uri _MoreInfo;

		public int FixType;

		public string FixName { get => _FixName; set => SetProperty(ref _FixName, value); }
		string _FixName;

		[DefaultValue(true)]
		public bool IsEnabled { get => _IsEnabled; set => SetProperty(ref _IsEnabled, value); }
		bool _IsEnabled = true;

		public IssueSeverity? Severity { get => _Severity; set => SetProperty(ref _Severity, value); }
		IssueSeverity? _Severity;

		public virtual void CheckTask()
		{
			throw new NotImplementedException();
		}

		public void Check()
		{
			Debug.WriteLine("---> {0}: {1}", "Check", GetType());
			Status = IssueStatus.Checking;
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
				Status = IssueStatus.Idle;
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
			Status = IssueStatus.Fixing;
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
				Status = IssueStatus.Idle;
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

		protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
		{
			property = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public bool ResetToDefault()
		{
			throw new NotImplementedException();
		}

		public void Save()
		{
			throw new NotImplementedException();
		}

		public void SaveAs(string fileName)
		{
			throw new NotImplementedException();
		}

		public void Load()
		{
			throw new NotImplementedException();
		}

		public void LoadFrom(string fileName)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
