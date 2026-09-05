using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class BaseFormWithHeader : Form
	{
		public BaseFormWithHeader()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
			DoubleBuffered = true;
			InitializeComponent();
			if (IsDesignMode)
				return;
			defaultBody = HelpBodyLabel.Text;
			// What the header goes back to before anything has set a subject of its own.
			restingSubject = HelpSubjectLabel.Text;
			InitLoadingCircle();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				if (!IsDesignMode)
					cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth bottom-to-top double-buffered painting
				return cp;
			}
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

		/// <summary>What the header says when the mouse is over nothing in particular.</summary>
		private string restingSubject;

		public void SetHeaderSubject(string text)
		{
			restingSubject = text;
			if (HelpSubjectLabel.Text != text)
				HelpSubjectLabel.Text = text;
		}

		/// <summary>Reports what the mouse is over, without disturbing what it will go back to.</summary>
		/// <remarks>
		/// Separate from SetHeaderInfo, which stamps the time onto what it is given because it
		/// carries status messages. A description of a control is not an event, and dating it
		/// says the control was just now what it has always been.
		/// </remarks>
		public void ShowHelp(string name, string purpose)
		{
			if (!string.IsNullOrEmpty(name) && HelpSubjectLabel.Text != name)
				HelpSubjectLabel.Text = name;
			SetHeaderBody(MessageBoxIcon.None, purpose);
		}

		/// <summary>Puts the header back to what it said before the mouse arrived.</summary>
		public void ClearHelp()
		{
			if (HelpSubjectLabel.Text != restingSubject)
				HelpSubjectLabel.Text = restingSubject ?? "";
			SetHeaderBody(MessageBoxIcon.None);
		}

		public void SetHeaderError(string body, params object[] args)
		{
			// Apply format.
			if (body == null)
				body = defaultBody;
			else if (args.Length > 0)
				body = string.Format(body, args);
			// Set info with time.
			SetHeaderBody(MessageBoxIcon.Error, "{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, body);
		}

		public void SetHeaderInfo(string body, params object[] args)
		{
			// Apply format.
			if (body == null)
				body = defaultBody;
			else if (args.Length > 0)
				body = string.Format(body, args);
			// Set info with time.
			SetHeaderBody(MessageBoxIcon.Information, "{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, body);
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
