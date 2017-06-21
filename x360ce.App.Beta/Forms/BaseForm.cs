using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class BaseForm : Form
	{
		public BaseForm()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			defaultBody = HelpBodyLabel.Text;
			InitLoadingCircle();
		}

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		#region WebService loading circle

		void InitLoadingCircle()
		{
			BusyLoadingCircle.Visible = false;
			BusyLoadingCircle.Top = HeaderPictureBox.Top;
			BusyLoadingCircle.Left = HeaderPictureBox.Left;
			LoadinngCircleTimeout = new Timer();
			LoadinngCircleTimeout.Tick += new EventHandler(LoadinngCircleTimeout_Tick);
		}

		Timer LoadinngCircleTimeout = new Timer();

		object TasksLock = new object();
		BindingList<TaskName> Tasks = new BindingList<TaskName>();

		public void AddTask(TaskName name)
		{
			lock (TasksLock)
			{
				Tasks.Add(name);
				UpdateIcon();
			}
		}

		public void RemoveTask(TaskName name)
		{
			lock (TasksLock)
			{
				if (Tasks.Contains(name))
				{
					Tasks.Remove(name);
				}
				UpdateIcon();
			}
		}

		void UpdateIcon()
		{
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
				LoadinngCircleTimeout.Enabled = true;
			}
		}

		void LoadinngCircleTimeout_Tick(object sender, EventArgs e)
		{
			LoadinngCircleTimeout.Enabled = false;
			BusyLoadingCircle.Active = false;
			BusyLoadingCircle.Visible = false;
		}

		#endregion

		#region Help Header

		string defaultBody;

		public void SetHeaderSubject(string subject)
		{
			HelpSubjectLabel.Text = subject;
		}

		public void SetHeaderBody(MessageBoxIcon icon, string body = null, params object[] args)
		{
			if (body == null) body = defaultBody;
			else if (args != null) string.Format(body, args);
			HelpBodyLabel.Text = body;
			// Update body colours.
			if (icon == MessageBoxIcon.Error) HelpBodyLabel.ForeColor = Color.DarkRed;
			else if (icon == MessageBoxIcon.Information) HelpBodyLabel.ForeColor = Color.DarkGreen;
			else HelpBodyLabel.ForeColor = SystemColors.ControlText;
		}

		#endregion
	}
}
