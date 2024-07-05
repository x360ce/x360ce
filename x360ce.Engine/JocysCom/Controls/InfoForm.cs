using JocysCom.ClassLibrary.Processes;
using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{

	public partial class InfoForm
	{
		public InfoForm()
		{
			ControlsHelper.InitInvokeContext();
			Load += InfoForm_Load;
			InitializeComponent();
		}

		public Control SelectedControl { get; set; }

		public bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1306:SetLocaleForDataTypes")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public void InfoForm_Load(object sender, EventArgs e)
		{
			if (IsDesignMode)
				return;
			SelectControl(SelectedControl);
		}

		public void SelectControl(Control c)
		{
			ItemPropertyGrid.SelectedObject = c;
			FormNameValueLabel.Text = c.Name;
			FormLocationValueLabel.Text = c.Location.ToString();
			FormSizeValueLabel.Text = c.Size.ToString();
			var dtControls = new DataTable();
			// List of controls on the form
			var nameColumn = new DataColumn();
			nameColumn.ColumnName = "name";
			dtControls.Columns.Add(nameColumn);
			var typeColumn = new DataColumn();
			typeColumn.ColumnName = "type";
			dtControls.Columns.Add(typeColumn);
			DataRow dr = null;
			dr = dtControls.NewRow();
			Text = c.Name + "Form Info";
			dr["name"] = c.Name;
			dr["type"] = c.GetType().ToString();
			dtControls.Rows.Add(dr);
			foreach (Control c1 in c.Controls)
			{
				dr = dtControls.NewRow();
				dr["name"] = c1.Name;
				dr["type"] = c1.GetType().ToString();
				dtControls.Rows.Add(dr);
			}
			ControlsDataGridView.AutoGenerateColumns = false;
			// WORKAROUND: Remove SelectionChanged event.
			ControlsDataGridView.SelectionChanged -= ControlsDataGridView_SelectionChanged;
			ControlsDataGridView.DataSource = dtControls;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			ControlsHelper.BeginInvoke(() =>
			{
				ControlsDataGridView.SelectionChanged += ControlsDataGridView_SelectionChanged;
				ControlsDataGridView_SelectionChanged(ControlsDataGridView, new EventArgs());
			});
			foreach (Control lbl in Controls)
			{
				if ((lbl) is Label)
				{
					lbl.DoubleClick += new EventHandler(Label_DoubleClick);
				}
			}
			ResetFormButton_Click(null, null);
		}

		public void ControlsDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			if (ControlsDataGridView.SelectedRows.Count == 0)
				return;
			int idx = ControlsDataGridView.SelectedRows[0].Index;
			PropertiesTabPage.Text = string.Format("Properties - {0}", ControlsDataGridView[0, idx].Value.ToString());
			if (idx == 0)
			{
				ItemPropertyGrid.SelectedObject = SelectedControl;
			}
			else
			{
				ItemPropertyGrid.SelectedObject = SelectedControl.Controls[ControlsDataGridView[0, idx].Value.ToString()];
			}
		}

		public void InfoForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				Close();
		}

		public void ControlsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			string sClip = ControlsDataGridView[e.ColumnIndex, e.RowIndex].Value.ToString();
			Clipboard.SetText(sClip);
		}

		public void Label_DoubleClick(object sender, EventArgs e)
		{
			Clipboard.SetText(((Label)sender).Text);
		}

		public void ResetFormButton_Click(object sender, EventArgs e)
		{
			ControlsDataGridView.ClearSelection();
			PropertiesTabPage.Text = string.Format("Properties - {0}", SelectedControl.ToString());
			ItemPropertyGrid.SelectedObject = SelectedControl;
		}


		static InfoForm _infoForm;

		/// <summary>
		/// Displays form's name + some other info
		/// </summary>
		/// <remarks></remarks>
		public static void ShowFormInfo(Control c)
		{
			if (_infoForm != null)
				return;
			_infoForm = new InfoForm();
			_infoForm.SelectedControl = c;
			_infoForm.StartPosition = FormStartPosition.CenterParent;
			ControlsHelper.CheckTopMost(_infoForm);
			_infoForm.ShowDialog();
			_infoForm.Dispose();
			_infoForm = null;
			lock (ShowLock)
				IsVisible = false;
		}

		static MouseHook _mouseHook;
		static object monitorEnabledLock = new object();

		public static bool MonitorEnabled
		{
			get
			{
				lock (monitorEnabledLock)
					return _MonitorEnabled;
			}
			set
			{
				lock (monitorEnabledLock)
				{
					// Reurn if in correct state already.
					if (value == _MonitorEnabled)
						return;
					if (value)
					{
						_mouseHook = new MouseHook();
						_mouseHook.OnMouseDown += _mouseHook_OnMouseDown;
						_mouseHook.Start();
					}
					else
					{
						_mouseHook.OnMouseDown -= _mouseHook_OnMouseDown;
						_mouseHook.Stop();
						_mouseHook.Dispose();
						_mouseHook = null;
					}
					_MonitorEnabled = value;
				}
			}
		}
		static bool _MonitorEnabled;

		static object ShowLock = new object();
		static bool IsVisible;

		private static void _mouseHook_OnMouseDown(object sender, MouseEventArgs e)
		{
			if (ModifierKeys.HasFlag(Keys.Control) && ModifierKeys.HasFlag(Keys.Shift) && e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				var hwnd = NativeMethods.WindowFromPoint(e.Location);
				var other = FromChildHandle(hwnd);
				if (other is null)
					return;
				var relative = other.PointToClient(e.Location);
				var c2 = other.GetChildAtPoint(relative, GetChildAtPointSkip.None);
				var c0 = c2 ?? other;
				if (c0 is null || c0 is InfoForm)
					return;
				if (c0.GetType().IsNested && c0.Parent != null)
					c0 = c0.Parent;
				lock (ShowLock)
				{
					if (IsVisible)
						return;
					IsVisible = true;
				}
				ShowFormInfo(c0);
			}
		}

		public static partial class NativeMethods
		{

			#region user32

			/// <summary>
			/// Retrieves a handle to the window that contains the specified point. 
			/// </summary>
			/// <param name="Point">The point to be checked. </param>
			/// <returns>
			/// The return value is a handle to the window that contains the point.
			/// If no window exists at the given point, the return value is NULL.
			/// If the point is over a static text control, the return value is a handle to the window under the static text control. </returns>
			[DllImport("user32.dll", SetLastError = true)]
			public static extern IntPtr WindowFromPoint(System.Drawing.Point point);

			#endregion

		}


	}
}
