using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class BaseFormWithHeader : Form, IBaseWithHeaderControl
	{
		public BaseFormWithHeader()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			defaultBody = HelpBodyLabel.Text;
			InitLoadingCircle();
		}

		internal bool IsDesignMode => JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this);

		#region WebService loading circle

		private void InitLoadingCircle()
		{
			BusyLoadingCircle.Visible = false;
			BusyLoadingCircle.Top = HeaderPictureBox.Top;
			BusyLoadingCircle.Left = HeaderPictureBox.Left;
		}

		private readonly object TasksLock = new object();
		private readonly BindingList<TaskName> Tasks = new BindingList<TaskName>();

		/// <summary>Activate busy spinner.</summary>
		public void AddTask(TaskName name)
		{
			lock (TasksLock)
			{
				Tasks.Add(name);
				UpdateIcon();
			}
		}

		/// <summary>Deactivate busy spinner if all tasks are gone.</summary>
		public void RemoveTask(TaskName name)
		{
			lock (TasksLock)
			{
				if (Tasks.Contains(name))
					Tasks.Remove(name);
				UpdateIcon();
			}
		}

		private void UpdateIcon()
		{
			// Update interface from the same thread.
			if (InvokeRequired)
			{
				Invoke(new Action(() => UpdateIcon()));
				return;
			}
			var value = Tasks.Count > 0;
			if (value && !BusyLoadingCircle.Active)
			{
				BusyLoadingCircle.Color = Color.SteelBlue;
				BusyLoadingCircle.InnerCircleRadius = 12;
				BusyLoadingCircle.NumberSpoke = 100;
				BusyLoadingCircle.OuterCircleRadius = 18;
				BusyLoadingCircle.RotationSpeed = 10;
				BusyLoadingCircle.SpokeThickness = 3;
				BusyLoadingCircle.Active = value;
				BusyLoadingCircle.Visible = value;
			}
			else if (!value && BusyLoadingCircle.Active)
			{
				BusyLoadingCircle.Active = false;
				BusyLoadingCircle.Visible = false;
			}
		}

		#endregion

		#region Help Header

		private readonly string defaultBody;

		public void SetHead(string format, params object[] args)
		{
			var text = args.Length == 0 ? format : string.Format(format, args);
			if (HelpSubjectLabel.Text != text)
				HelpSubjectLabel.Text = text;
		}

		public void SetTitle(string format, params object[] args)
		{
			var text = args.Length == 0 ? format : string.Format(format, args);
			if (Text != text)
				Text = text;
		}

		public void SetBodyError(string format, params object[] args)
		{
			// Apply format.
			if (format == null)
				format = defaultBody;
			else if (args.Length > 0)
				format = string.Format(format, args);
			// Set info with time.
			SetHeaderBody(MessageBoxIcon.Error, "{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, format);
		}

		public void SetBodyInfo(string format, params object[] args)
		{
			// Apply format.
			if (format == null)
				format = defaultBody;
			else if (args.Length > 0)
				format = string.Format(format, args);
			// Set info with time.
			SetHeaderBody(MessageBoxIcon.Information, "{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, format);
		}

		public void SetHeaderBody(MessageBoxIcon icon, string body = null, params object[] args)
		{
			if (body == null)
				body = defaultBody;
			else if (args.Length > 0)
				body = string.Format(body, args);
			HelpBodyLabel.Text = body;
			// Update body colors.
			if (icon == MessageBoxIcon.Error)
				HelpBodyLabel.ForeColor = Color.DarkRed;
			else if (icon == MessageBoxIcon.Information)
				HelpBodyLabel.ForeColor = Color.DarkGreen;
			else
				HelpBodyLabel.ForeColor = SystemColors.ControlText;
		}

		#endregion
	}
}
