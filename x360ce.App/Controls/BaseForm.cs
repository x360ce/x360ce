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

		internal bool IsDesignMode
		{
			get
			{
				if (DesignMode) return true;
				if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return true;
				var pa = this.ParentForm;
				if (pa != null && pa.GetType().FullName.Contains("VisualStudio")) return true;
				return false;
			}
		}

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

		public bool LoadingCircle
		{
			get { return BusyLoadingCircle.Active; }
			set
			{
				if (value)
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
				else
				{
					LoadinngCircleTimeout.Enabled = true;
				}
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

		public void SetHeaderBody(MessageBoxIcon icon, string body,  params object[] args)
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
