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
			InitializeComponent();
			if (IsDesignMode)
				return;
			defaultBody = HelpBodyLabel.Text;
			// What the header goes back to before anything has set a subject of its own.
			restingSubject = HelpSubjectLabel.Text;
			InitLoadingCircle();
			// The header's lines follow its fonts, never its text.
			HelpSubjectLabel.FontChanged += (s, e) => FitHeader();
			HelpBodyLabel.FontChanged += (s, e) => FitHeader();
			FitHeader();
			// The header body's colour is the theme's, so it is taken again in a new one.
			EventHandler themeChanged = (s, e) => ColorHeaderBody();
			JocysCom.ClassLibrary.Controls.Themes.FormsTheme.ThemeChanged += themeChanged;
			Disposed += (s, e) => JocysCom.ClassLibrary.Controls.Themes.FormsTheme.ThemeChanged -= themeChanged;
		}

		internal bool IsDesignMode => JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this);

		/// <summary>
		/// Makes the header one line of its subject and two of its tip tall, in their fonts, whatever they say: a tip longer
		/// than two lines ends with an ellipsis. The header, and every page below it, then stays where it is as the text changes.
		/// </summary>
		/// <remarks>
		/// A line is as tall as Windows draws it: at some scales a pixel taller than the font's height, and Windows leaves out a
		/// last line it cannot draw whole, so the larger of the two is taken.
		/// </remarks>
		void FitHeader()
		{
			var subject = Lines(HelpSubjectLabel, 1) + HelpSubjectLabel.Padding.Vertical;
			if (HelpSubjectLabel.Height != subject)
				HelpSubjectLabel.Height = subject;
			var tip = Lines(HelpBodyLabel, 2) + HelpBodyLabel.Padding.Vertical;
			if (HelpBodyLabel.Height != tip)
				HelpBodyLabel.Height = tip;
		}

		/// <summary>The height of a number of lines of a label's text as Windows draws them.</summary>
		internal static int Lines(Label label, int count)
		{
			var text = string.Join("\n", System.Linq.Enumerable.Repeat("A", count));
			return Math.Max(count * label.Font.Height, TextRenderer.MeasureText(text, label.Font).Height);
		}

		protected override void OnLoad(EventArgs e)
		{
			// Images are drawn at the size they were made, so they are enlarged to the screen's scale
			// before the window is first shown.
			if (!IsDesignMode)
				JocysCom.ClassLibrary.Controls.ControlsHelper.ScaleImages(this);
			base.OnLoad(e);
		}

		/// <summary>
		/// While the window's edge is dragged nothing on it is laid out, as Windows moves each child window on its own, which
		/// takes longer than the mouse takes between two steps. <see cref="OnResizeEnd"/> lays it out once at the size it was left.
		/// </summary>
		protected override void OnResizeBegin(EventArgs e)
		{
			SuspendLayout();
			base.OnResizeBegin(e);
		}

		/// <summary>Lays the window out at the size the drag left it. A drag that only moved the window lays nothing out.</summary>
		protected override void OnResizeEnd(EventArgs e)
		{
			base.OnResizeEnd(e);
			ResumeLayout();
		}

		#region WebService loading circle

		private void InitLoadingCircle()
		{
			BusyLoadingCircle.Visible = false;
			// Over the program's picture, wherever the header's table puts it.
			BusyLoadingCircle.Parent = HeaderPictureBox;
			BusyLoadingCircle.Dock = DockStyle.Fill;
		}

		private readonly object TasksLock = new object();
		private readonly BindingList<TaskName> Tasks = new BindingList<TaskName>();

		/// <summary>Activate busy spinner.</summary>
		public void AddTask(TaskName name)
		{
			lock (TasksLock)
				Tasks.Add(name);
			UpdateIcon();
		}

		/// <summary>Deactivate busy spinner if all tasks are gone.</summary>
		public void RemoveTask(TaskName name)
		{
			lock (TasksLock)
			{
				if (Tasks.Contains(name))
					Tasks.Remove(name);
			}
			UpdateIcon();
		}

		/// <summary>Shows the spinner while any task runs.</summary>
		/// <remarks>
		/// From another thread it is posted, never waited for, and outside the lock, so no thread holds the lock while it
		/// waits for the interface thread, which may itself be waiting for the lock.
		/// </remarks>
		private void UpdateIcon()
		{
			// Update interface from the same thread.
			if (InvokeRequired)
			{
				JocysCom.ClassLibrary.Controls.ControlsHelper.BeginInvoke(UpdateIcon);
				return;
			}
			bool value;
			lock (TasksLock)
				value = Tasks.Count > 0;
			if (value && !BusyLoadingCircle.Active)
			{
				// In pixels, so enlarged with the screen like the box the circle is drawn in.
				var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
				BusyLoadingCircle.Color = Color.SteelBlue;
				BusyLoadingCircle.InnerCircleRadius = (int)Math.Round(12 * scale);
				BusyLoadingCircle.NumberSpoke = 100;
				BusyLoadingCircle.OuterCircleRadius = (int)Math.Round(18 * scale);
				BusyLoadingCircle.RotationSpeed = 10;
				BusyLoadingCircle.SpokeThickness = (int)Math.Round(3 * scale);
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
			headerIcon = icon;
			ColorHeaderBody();
		}

		/// <summary>What the header body says: an error, information, or neither.</summary>
		MessageBoxIcon headerIcon;

		/// <summary>The header body in the theme's warning colour for an error, its information colour for information, and the text colour otherwise.</summary>
		void ColorHeaderBody()
		{
			if (headerIcon == MessageBoxIcon.Error)
				HelpBodyLabel.ForeColor = JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetColor("ForegroundWarning");
			else if (headerIcon == MessageBoxIcon.Information)
				HelpBodyLabel.ForeColor = JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetColor("ForegroundInformation");
			else
				HelpBodyLabel.ForeColor = SystemColors.ControlText;
		}

		#endregion
	}
}
