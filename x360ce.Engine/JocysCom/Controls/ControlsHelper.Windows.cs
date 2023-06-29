using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework
using System.Data.Objects.DataClasses;
#endif

namespace JocysCom.ClassLibrary.Controls
{
    public static partial class ControlsHelper
    {

        #region IsDesignMode

        private static bool? _IsDesignMode;

        public static bool IsDesignMode(Component component)
        {
            if (!_IsDesignMode.HasValue)
                _IsDesignMode = IsDesignMode1(component);
            return _IsDesignMode.Value;
        }

        private static bool IsDesignMode2(IComponent component, IComponent parent)
        {
            // Check 1.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return true;
            if (component is null)
                throw new ArgumentNullException(nameof(component));
            // Check 2 (DesignMode).
            var site = component.Site;
            if (site != null && site.DesignMode)
                return true;
            if (parent != null && parent.GetType().FullName.Contains("VisualStudio"))
                return true;
            // Not design mode.
            return false;
        }

        private static bool IsDesignMode1(Component component)
        {
            var form = component as Form;
            if (form != null)
                return IsDesignMode2(form, form.ParentForm ?? form.Owner);
            var control = component as Control;
            if (control != null)
                return IsDesignMode2(control, control.Parent);
            return IsDesignMode2(component, null);
        }

		#endregion

#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework
#endif

		/// <summary>
		/// Raise event on same thread as the target of delegate.
		/// </summary>
		/// <param name="theEvent"></param>
		/// <param name="e"></param>
		//[Obsolete("Use ControlsHelper.BeginInvoke to raise events on main User Interface (UI) Thread.")]
		public static void RaiseEventOnTargetThread(Delegate theEvent, object sender, EventArgs e)
		{
			if (theEvent is null)
				return;
			foreach (var d in theEvent.GetInvocationList())
			{
				var scheduler = d.Target as TaskScheduler;
				var args = new object[] { sender, e };
				if (scheduler != null)
				{
					Task.Factory.StartNew(
						() => { d.DynamicInvoke(args); },
						CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler);
					continue;
				}
				var invoker = d.Target as System.ComponentModel.ISynchronizeInvoke;
				if (invoker != null)
				{
					var c = d.Target as System.Windows.Forms.Control;
					if (c != null && (c.Disposing || c.IsDisposed || !c.IsHandleCreated))
						continue;
					c.BeginInvoke(d, args);
					continue;
				}
				d.DynamicInvoke(args);
			}
		}


		internal class NativeMethods
		{
			/// <summary>
			/// Retrieves a handle to the window that contains the specified point. 
			/// </summary>
			/// <param name="Point">The point to be checked. </param>
			/// <returns>
			/// The return value is a handle to the window that contains the point.
			/// If no window exists at the given point, the return value is NULL.
			/// If the point is over a static text control, the return value is a handle to the window under the static text control. </returns>
			[DllImport("user32.dll", SetLastError = true)]
			public static extern IntPtr WindowFromPoint(System.Windows.Point point);
		}

		private const int WM_SETREDRAW = 0x000B;

		public static void SuspendDrawing(Control control)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var msgSuspendUpdate = Message.Create(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
			var window = NativeWindow.FromHandle(control.Handle);
			window.DefWndProc(ref msgSuspendUpdate);
		}

		public static void ResumeDrawing(Control control)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var wparam = new IntPtr(1);
			var msgResumeUpdate = Message.Create(control.Handle, WM_SETREDRAW, wparam, IntPtr.Zero);
			var window = NativeWindow.FromHandle(control.Handle);
			window.DefWndProc(ref msgResumeUpdate);
			control.Invalidate();
		}

        #region Data Grid Functions

		public static void RebindGrid<T>(DataGridView grid, object data, string keyPropertyName = null, bool selectFirst = true, List<T> selection = null)
		{
			if (grid is null)
				throw new ArgumentNullException(nameof(grid));
			var rowIndex = 0;
			if (grid.Rows.Count > 0)
			{
				var firsCell = grid.FirstDisplayedCell;
				if (firsCell != null)
					rowIndex = firsCell.RowIndex;
			}
			var sel = (selection is null)
				? GetSelection<T>(grid, keyPropertyName)
				: selection;
			grid.DataSource = data;
			if (rowIndex != 0 && rowIndex < grid.Rows.Count)
				grid.FirstDisplayedScrollingRowIndex = rowIndex;
			RestoreSelection(grid, keyPropertyName, sel, selectFirst);
		}

		/// <summary>
		/// Get list of primary keys of items selected in the grid.
		/// </summary>
		/// <typeparam name="T">Type of Primary key.</typeparam>
		/// <param name="grid">Grid for getting selection</param>
		/// <param name="primaryKeyPropertyName">Primary key name.</param>
		public static List<T> GetSelection<T>(DataGridView grid, string keyPropertyName = null)
		{
			if (grid is null)
				throw new ArgumentNullException(nameof(grid));
			var list = new List<T>();
			var rows = grid.SelectedRows.Cast<DataGridViewRow>().ToArray();
			// If nothing selected then try to get rows from cells.
			if (rows.Length == 0)
				rows = grid.SelectedCells.Cast<DataGridViewCell>().Select(x => x.OwningRow).Distinct().ToArray();
			// If nothing selected then return.
			if (rows.Length == 0)
				return list;
			var pi = GetPropertyInfo(keyPropertyName, rows[0].DataBoundItem);
			for (var i = 0; i < rows.Length; i++)
			{
				var value = GetValue<T>(rows[i].DataBoundItem, keyPropertyName, pi);
				list.Add(value);
			}
			return list;
		}

		public static void RestoreSelection<T>(DataGridView grid, string keyPropertyName, List<T> list, bool selectFirst = true)
		{
			if (grid is null)
				throw new ArgumentNullException(nameof(grid));
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			var rows = grid.Rows.Cast<DataGridViewRow>().ToArray();
			// Return if grid is empty.
			if (rows.Length == 0)
				return;
			// If something to restore then...
			if (list.Count > 0)
			{
				var pi = GetPropertyInfo(keyPropertyName, rows[0].DataBoundItem);
				DataGridViewRow firstVisibleRow = null;
				for (var i = 0; i < rows.Length; i++)
				{
					var row = rows[i];
					if (firstVisibleRow is null && row.Visible)
						firstVisibleRow = row;
					var item = row.DataBoundItem;
					var val = GetValue<T>(item, keyPropertyName, pi);
					if (list.Contains(val) != row.Selected)
					{
						var selected = list.Contains(val);
						// Select visible rows only, because invisible rows can't be selected or they will throw exception:
						// Row associated with the currency manager's position cannot be made invisible.'
						row.Selected = selected && row.Visible;
					}
				}
			}
			// If must select first row and nothing is selected then...
			if (selectFirst && grid.SelectedRows.Count == 0)
			{
				var firstVisibleRow = rows.FirstOrDefault(x => x.Visible);
				// Select first visible row.
				if (firstVisibleRow != null)
					firstVisibleRow.Selected = true;
			}
		}

        #endregion

		/// <summary>
		/// Set form TopMost if one of the application forms is top most.
		/// </summary>
		/// <param name="form"></param>
		public static void CheckTopMost(Form form)
		{
			// If this form is not set as TopMost but one of the application forms is on TopMost then...
			// Make this dialog form TopMost too or user won't be able to access it.
			if (!form.TopMost && Application.OpenForms.Cast<Form>().Any(x => x.TopMost))
				form.TopMost = true;
		}

		#region UserControl is Visible

		public static bool IsControlVisibleOnForm(Control control)
		{
			if (control is null)
				return false;
			if (!control.IsHandleCreated)
				return false;
			if (control.Parent is null)
				return false;
			var pointsToCheck = GetPoints(control, true);
			foreach (var p in pointsToCheck)
			{
				var child = control.Parent.GetChildAtPoint(new Point((int)p.X, (int)p.Y));
				if (child is null)
					continue;
				if (control == child || control.Contains(child))
					return true;
			}
			return false;
		}

		public static System.Windows.Point[] GetPoints(Control control, bool relative = false)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var pos = relative
				? Point.Empty
				// Get control position on the screen
				: control.PointToScreen(System.Drawing.Point.Empty);
			var pointsToCheck =
				new Point[]
					{
						// Top-Left.
						pos,
						// Top-Right.
						new Point(pos.X + control.Width - 1, pos.Y),
						// Bottom-Left.
						new Point(pos.X, pos.Y + control.Height - 1),
						// Bottom-Right.
						new Point(pos.X + control.Width - 1, pos.Y + control.Height - 1),
						// Middle-Centre.
						new Point(pos.X + control.Width/2, pos.Y + control.Height/2)
					};
			return pointsToCheck.Select(x => new System.Windows.Point(x.X, x.Y)).ToArray();
		}

		public static bool IsControlVisibleToUser(Control control)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			if (!control.IsHandleCreated)
				return false;
			var pointsToCheck = GetPoints(control);
			foreach (var p in pointsToCheck)
			{
				var hwnd = NativeMethods.WindowFromPoint(p);
				var other = Control.FromChildHandle(hwnd);
				if (other is null)
					continue;
				if (GetAll(control, null, true).Contains(other))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Get parent control of specific type.
		/// </summary>
		public static T GetParent<T>(Control control, bool includeTop = false) where T : class
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var parent = control;
			while (parent != null)
			{
				if (parent is T && (includeTop || parent != control))
					return (T)(object)parent;
				parent = parent.Parent;
			}
			return null;
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static IEnumerable<Control> GetAll(Control control, Type type = null, bool includeTop = false)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			// Create new list.
			var controls = new List<Control>();
			// Add top control if required.
			if (includeTop && !controls.Contains(control))
				controls.Add(control);
			// If control contains children then...
			if (control.HasChildren)
			{
				foreach (var child in control.Controls.Cast<Control>())
				{
					var children = GetAll(child, null, true);
					controls.AddRange(children.Except(controls));
				}
			}
			// If type filter is not set then...
			return (type is null)
				? controls
				: controls.Where(x => type.IsInterface ? x.GetType().GetInterfaces().Contains(type) : type.IsAssignableFrom(x.GetType()));
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(Control control, bool includeTop = false)
		{
			if (control is null)
				return new T[0];
			return GetAll(control, typeof(T), includeTop).Cast<T>().ToArray();
		}

		public static void GetActiveControl(Control control, out Control activeControl, out string activePath)
		{
			// Return current control by default.
			activePath = string.Format("/{0}", control.Name);
			activeControl = control;
			// If control can contains active controls.
			var container = control as ContainerControl;
			while (container != null)
			{
				control = container.ActiveControl;
				if (control is null)
					break;
				activePath += string.Format("/{0}", control.Name);
				activeControl = control;
				container = control as ContainerControl;
			}
		}

        #endregion

        #region Set Visible, Enabled and Text

		internal const int STATE_VISIBLE = 0x00000002;
		internal const int STATE_ENABLED = 0x00000004;
		private static MethodInfo _GetState;

		// Check control.Visible state.
		public static bool IsVisible(Control control)
		{
			_GetState = _GetState ?? typeof(Control).GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic);
			// Can't check property directly, because it will return false if parent is not visible.
			var stateValue = (bool)_GetState.Invoke(control, new object[] { STATE_VISIBLE });
			return stateValue;
		}

		public static void SetVisible(Control control, bool visible)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var stateValue = IsVisible(control);
			if (stateValue != visible)
				control.Visible = visible;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		public static void SetEnabled(ToolStripItem control, bool enabled)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			if (control.Enabled != enabled)
				control.Enabled = enabled;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		public static void SetEnabled(Control control, bool enabled)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			_GetState = _GetState ?? typeof(Control).GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic);
			// Can't check property directly, because it will return false if parent is not enabled.
			var stateValue = (bool)_GetState.Invoke(control, new object[] { STATE_ENABLED });
			if (stateValue != enabled)
				control.Enabled = enabled;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(Control control, string format, params object[] args)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var text = (args is null)
				? format
				: string.Format(format, args);
			if (control.Text != text)
				control.Text = text;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(ToolStripItem control, string format, params object[] args)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var text = (args is null)
				? format
				: string.Format(format, args);
			if (control.Text != text)
				control.Text = text;
		}

		public static void SetTextFromResource(RichTextBox box, string resourceName)
		{
			var rtf = Helper.FindResource<string>(resourceName, Assembly.GetEntryAssembly());
			box.Rtf = rtf;
			box.SelectAll();
			box.SelectionIndent = 8;
			box.SelectionRightIndent = 8;
			box.DeselectAll();
			box.LinkClicked += (object sender, System.Windows.Forms.LinkClickedEventArgs e) =>
				OpenUrl(e.LinkText);
		}

		public static void SetReadOnly(Control control, bool readOnly)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var p = control.GetType().GetProperty("ReadOnly");
			if (p is null || !p.CanWrite)
				return;
			var value = (bool)p.GetValue(control, null);
			if (value != readOnly)
			{
				p.SetValue(control, readOnly, null);
			}
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetChecked(CheckBox control, bool check)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			if (control.Checked != check)
				control.Checked = check;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetChecked(ToolStripButton control, bool check)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			if (control.Checked != check)
				control.Checked = check;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetSelectedItem<T>(ComboBox control, T value)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			if (typeof(T).IsEnum && !Enum.IsDefined(typeof(T), value))
				value = default(T);
			if (!Equals(control.SelectedItem, value))
				control.SelectedItem = value;
		}

		/// <summary>
		/// Select item on DataGridView control.
		/// Select item first to make sure that no items are unselected.
		/// </summary>
		/// <param name="control">DataGridView, Control</param>
		/// <param name="item">Data bound object to select</param>
		public static void SetSelectedItem<T>(DataGridView control, params T[] items)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			// Select rows first.
			foreach (DataGridViewRow row in control.Rows)
				if (items.Any(x => row.DataBoundItem.Equals(x)) && !row.Selected)
					row.Selected = true;
			// Unselect rows.
			foreach (DataGridViewRow row in control.Rows)
				if (!items.Any(x => row.DataBoundItem.Equals(x)) && row.Selected)
					row.Selected = false;
		}

        #endregion

        #region Add Grip to SplitContainer 

		public static void ApplySplitterStyle(SplitContainer control)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			// Paint 3 dots on the splitter.
			control.Paint += SplitContainer_Paint;
			// Remove focus from splitter after it moved.
			control.SplitterMoved += SplitContainer_SplitterMoved;
		}

		private static void SplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
		{
			var s = sender as Control;
			if (s.CanFocus)
			{
				while (true)
				{
					s = s.Parent;
					if (s is null)
						return;
					if (s.CanFocus)
						s.Focus();
				}
			}
		}

		private static void SplitContainer_Paint(object sender, PaintEventArgs e)
		{
			// base.OnPaint(e);
			var s = sender as SplitContainer;
			// Paint the three dots.
			var points = new Point[3];
			var w = s.Width;
			var h = s.Height;
			var d = s.SplitterDistance;
			var sW = s.SplitterWidth;
			int x;
			int y;
			var spacing = 10;
			// Calculate the position of the points.
			if (s.Orientation == Orientation.Horizontal)
			{
				x = (w / 2);
				y = d + (sW / 2);
				points[0] = new Point(x, y);
				points[1] = new Point(x - spacing, y);
				points[2] = new Point(x + spacing, y);
			}
			else
			{
				x = d + (sW / 2);
				y = (h / 2);
				points[0] = new Point(x, y);
				points[1] = new Point(x, y - spacing);
				points[2] = new Point(x, y + spacing);
			}
			foreach (var p in points)
			{
				p.Offset(-2, -2);
				e.Graphics.FillEllipse(SystemBrushes.ControlDark, new Rectangle(p, new Size(3, 3)));
				p.Offset(1, 1);
				e.Graphics.FillEllipse(SystemBrushes.ControlLight, new Rectangle(p, new Size(3, 3)));
			}
		}

        #endregion

        #region Apply Grid Border Style

		public static void ApplyBorderStyle(DataGridView grid, bool updateEnabledProperty = false)
		{
			if (grid is null)
				throw new ArgumentNullException(nameof(grid));
			grid.BackgroundColor = Color.White;
			grid.BorderStyle = BorderStyle.None;
			grid.EnableHeadersVisualStyles = false;
			grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
			grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
			grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			grid.RowHeadersDefaultCellStyle.BackColor = SystemColors.Control;
			grid.BackColor = SystemColors.Window;
			grid.DefaultCellStyle.BackColor = SystemColors.Window;
			grid.CellPainting += Grid_CellPainting;
			grid.SelectionChanged += Grid_SelectionChanged;
			grid.CellFormatting += Grid_CellFormatting;
			if (updateEnabledProperty)
				grid.CellClick += Grid_CellClick;
		}

		private static void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			// If add new record row.
			if (grid.AllowUserToAddRows && e.RowIndex + 1 == grid.Rows.Count)
				return;
			var column = grid.Columns[e.ColumnIndex];
			var item = grid.Rows[e.RowIndex].DataBoundItem;
			if (column.DataPropertyName == "Enabled" || column.DataPropertyName == "IsEnabled")
			{
				SetEnabled(item, !GetEnabled(item));
				grid.Invalidate();
			}
		}

		private static void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			// If add new record row.
			if (grid.AllowUserToAddRows && e.RowIndex + 1 == grid.Rows.Count)
				return;
			var row = grid.Rows[e.RowIndex];
			if (e.RowIndex > -1 && e.ColumnIndex > -1)
			{
				var item = row.DataBoundItem;
				// If grid is virtual then...
				if (item is null)
				{
					var list = grid.DataSource as IBindingList;
					if (list != null)
						item = list[e.RowIndex];
				}
				var enabled = true;
				if (item != null)
					enabled = GetEnabled(item);
				var fore = enabled ? grid.DefaultCellStyle.ForeColor : SystemColors.ControlDark;
				var selectedBack = enabled ? grid.DefaultCellStyle.SelectionBackColor : SystemColors.ControlDark;
				// Apply style to row header.
				if (row.HeaderCell.Style.ForeColor != fore)
					row.HeaderCell.Style.ForeColor = fore;
				if (row.HeaderCell.Style.SelectionBackColor != selectedBack)
					row.HeaderCell.Style.SelectionBackColor = selectedBack;
				// Apply style to cell
				var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
				if (cell.Style.ForeColor != fore)
					cell.Style.ForeColor = fore;
				if (cell.Style.SelectionBackColor != selectedBack)
					cell.Style.SelectionBackColor = selectedBack;
			}
		}

		private static void Grid_SelectionChanged(object sender, EventArgs e)
		{
			// Sort issue with paint artifcats.
			var grid = (DataGridView)sender;
			grid.Invalidate();
		}

		private static void SetEnabled(object item, bool enabled)
		{
			var enabledProperty = item.GetType().GetProperties().FirstOrDefault(x => x.Name == "Enabled" || x.Name == "IsEnabled");
			if (enabledProperty != null)
			{
				enabledProperty.SetValue(item, enabled, null);
			}
		}

		private static bool GetEnabled(object item)
		{
			var enabledProperty = item.GetType().GetProperties().FirstOrDefault(x => x.Name == "Enabled" || x.Name == "IsEnabled");
			var enabled = enabledProperty is null ? true : (bool)enabledProperty.GetValue(item, null);
			return enabled;
		}

		private static void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// Header and cell borders must be set to "Single" style.
			var grid = (DataGridView)sender;
			var firstVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Min(x => x.Index);
			var lastVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Max(x => x.Index);
			var selected = e.RowIndex > -1 ? grid.Rows[e.RowIndex].Selected : false;
			e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);
			var bounds = e.CellBounds;
			var tl = new Point(bounds.X, bounds.Y);
			var tr = new Point(bounds.X + bounds.Width - 1, bounds.Y);
			var bl = new Point(bounds.X, bounds.Y + bounds.Height - 1);
			var br = new Point(bounds.X + bounds.Width - 1, bounds.Y + bounds.Height - 1);
			Color backColor;
			// If top left corner and column header then...
			if (e.RowIndex == -1)
			{
				backColor = selected
					? grid.ColumnHeadersDefaultCellStyle.SelectionBackColor
					: grid.ColumnHeadersDefaultCellStyle.BackColor;
			}
			// If row header then...
			else if (e.ColumnIndex == -1 && e.RowIndex > -1)
			{
				var row = grid.Rows[e.RowIndex];
				backColor = selected
					? row.HeaderCell.Style.SelectionBackColor
					: grid.RowHeadersDefaultCellStyle.BackColor;
			}
			// If normal cell then...
			else
			{
				var row = grid.Rows[e.RowIndex];
				var cell = row.Cells[e.ColumnIndex];
				backColor = selected
					? cell.InheritedStyle.SelectionBackColor
					: cell.InheritedStyle.BackColor;
			}
			// Cell background colour.
			var back = new Pen(backColor, 1);
			// Border colour.
			var border = new Pen(SystemColors.Control, 1);
			// Do not draw borders for selected device.
			Pen c;
			// Top
			e.Graphics.DrawLine(back, tl, tr);
			// Left (only if not first)
			c = !selected && e.ColumnIndex > firstVisibleColumn ? border : back;
			e.Graphics.DrawLine(c, bl, tl);
			// Right (always)
			c = back;
			e.Graphics.DrawLine(c, tr, br);
			// Bottom (always)
			c = border;
			e.Graphics.DrawLine(c, bl, br);
			back.Dispose();
			border.Dispose();
			e.Handled = true;
		}

        #endregion

        #region  Apply ToolStrip Border Style

		public static void ApplyBorderStyle(ToolStrip control)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			control.Renderer = new ToolStripBorderlessRenderer();
		}

        #endregion


        #region Apply TabControl Image Style

        private const string ApplyImageStyleDisabledSuffix = "_DisabledStyle";

		public static void ApplyImageStyle(TabControl control)
		{
			if (control is null)
				throw new ArgumentNullException(nameof(control));
			var list = control.ImageList;
			var keys = list.Images.Keys.Cast<string>().ToArray();
			for (var i = 0; i < keys.Length; i++)
			{
				var key = keys[i];
				var image = (Bitmap)list.Images[key];
				var disabledImage = (Bitmap)image.Clone();
				MakeImageTransparent(disabledImage, 128);
				list.Images.Add(key + ApplyImageStyleDisabledSuffix, disabledImage);
			}
			control.SelectedIndexChanged += ApplyImageStyle_TabControl_SelectedIndexChanged;
			ApplyImageStyle_TabControl_SelectedIndexChanged(control, new EventArgs());
		}

		/// <summary>Make bitmap transparent.</summary>
		/// <param name="b"></param>
		/// <param name="alpha">256 max</param>
		private static void MakeImageTransparent(Bitmap b, int alpha)
		{
			var w = b.Width;
			var h = b.Height;
			int a;
			Color p;
			for (var y = 0; y < h; y++)
			{
				for (var x = 0; x < w; x++)
				{
					p = b.GetPixel(x, y);
					a = (int)(p.A * (float)alpha / byte.MaxValue);
					if (a >= byte.MaxValue)
						a = byte.MaxValue;
					if (a <= byte.MinValue)
						a = byte.MinValue;
					b.SetPixel(x, y, Color.FromArgb(a, p.R, p.G, p.B));
				}
			}
		}

		private static void ApplyImageStyle_TabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			var control = (TabControl)sender;
			var list = control.ImageList;
			foreach (TabPage item in control.TabPages)
			{
				var en = item == control.SelectedTab;
				var key = item.ImageKey.Replace(ApplyImageStyleDisabledSuffix, "");
				if (!en)
					key += ApplyImageStyleDisabledSuffix;
				item.ImageKey = key;
			}
		}

        #endregion

        #region Binding

        public static Binding AddDataBinding<TD, TDp>(
            IBindableComponent control,
            TD data, Expression<Func<TD, TDp>> dataProperty)
        {
            if (control is null)
                throw new ArgumentNullException(nameof(control));
            if (dataProperty is null)
                throw new ArgumentNullException(nameof(dataProperty));
            var dataMemberBody = (MemberExpression)dataProperty.Body;
            var dataMemberName = dataMemberBody.Member.Name;
            string name = null;
            // Add TextBox.
            var textBox = control as TextBox;
            if (textBox != null)
                name = nameof(textBox.Text);
            // Add ComboBox.
            var comboBox = control as ComboBox;
            if (comboBox != null)
            {
                name = string.IsNullOrEmpty(comboBox.ValueMember)
                    ? nameof(comboBox.SelectedItem)
                    : nameof(comboBox.SelectedValue);
            }
            // Add CheckBox.
            var checkBox = control as CheckBox;
            if (checkBox != null)
                name = nameof(checkBox.Checked);
            // Add NumericUpDown.
            var upDown = control as NumericUpDown;
            if (upDown != null)
                name = nameof(upDown.Value);
            // If type is missing then throw error.
            if (string.IsNullOrEmpty(name))
                throw new Exception(string.Format("Add control Type '{0}' to ControlsHelper.AddDataBinding(control, data, dataProperty) method!", control.GetType()));
            // Add data binding.
            return control.DataBindings.Add(name, data, dataMemberName,
                false,
                DataSourceUpdateMode.OnPropertyChanged,
                null,
                null,
                null);
        }

        /// <summary>
        /// To avoid validation problems, make sure to add DataBindings inside "Load" event and not inside Constructor.
        /// </summary>
        public static Binding AddDataBinding<TC, TCp, TD, TDp>(
                TC control, Expression<Func<TC, TCp>> controlProperty,
                TD data, Expression<Func<TD, TDp>> dataProperty,
                bool formattingEnabled = false,
                DataSourceUpdateMode updateMode = DataSourceUpdateMode.OnPropertyChanged,
                object nullValue = null,
                string formatString = null,
                IFormatProvider formatInfo = null
            ) where TC : IBindableComponent
        {
            if (controlProperty is null)
                throw new ArgumentNullException(nameof(controlProperty));
            if (dataProperty is null)
                throw new ArgumentNullException(nameof(dataProperty));
            var propertyBody = (MemberExpression)controlProperty.Body;
            var propertyName = propertyBody.Member.Name;
            var dataMemberBody = (MemberExpression)dataProperty.Body;
            var dataMemberName = dataMemberBody.Member.Name;
            return control.DataBindings.Add(propertyName, data, dataMemberName,
                formattingEnabled,
                updateMode,
                nullValue,
                formatString,
                formatInfo);
        }

        /// <summary>
        /// Bing Enum to ComboBox.
        /// </summary>
        /// <typeparam name="TE">enum</typeparam>
        /// <param name="box">Combo box control</param>
        /// <param name="format">{0} - name, {1} - numeric value, {2} - description attribute.</param>
        /// <param name="addEmpty"></param>
        public static void BindEnum<TE>(System.Windows.Forms.ComboBox box, string format = null, bool addEmpty = false, bool sort = false, TE? selected = null, TE[] exclude = null)
            // Declare TE as same as Enum.
            where TE : struct, IComparable, IFormattable, IConvertible
        {
            if (box is null)
                throw new ArgumentNullException(nameof(box));
            var list = new List<DictionaryEntry>();
            if (string.IsNullOrEmpty(format))
                format = "{0}";
            string display;
            foreach (var value in (TE[])Enum.GetValues(typeof(TE)))
            {
                if (exclude != null && exclude.Contains(value))
                    continue;
                display = string.Format(format, value, System.Convert.ToInt64(value), Runtime.Attributes.GetDescription(value));
                list.Add(new DictionaryEntry(display, value));
            }
            if (sort)
                list = list.OrderBy(x => x.Key).ToList();
            if (addEmpty && !list.Any(x => string.IsNullOrEmpty((string)x.Key)))
                list.Insert(0, new DictionaryEntry("", null));
            // Make sure sorted is disabled, because it is not allowed when using DataSource.
            if (box.Sorted)
                box.Sorted = false;
            box.DataSource = list;
            box.DisplayMember = "Key";
            box.ValueMember = "Value";
            if (selected.HasValue)
                SelectEnumValue(box, selected.Value);
        }

        public static void SelectEnumValue<TE>(ComboBox box, TE value)
            // Declare TE as same as Enum.
            where TE : struct, IComparable, IFormattable, IConvertible
        {
            if (box is null)
                throw new ArgumentNullException(nameof(box));
            for (var i = 0; i < box.Items.Count; i++)
            {
                var val = ((DictionaryEntry)box.Items[i]).Value;
                if (Equals(val, value))
                {
                    box.SelectedIndex = i;
                    return;
                }
            }
        }

        #endregion



    }
}

