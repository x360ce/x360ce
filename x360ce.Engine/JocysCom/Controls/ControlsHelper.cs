using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Objects.DataClasses;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace JocysCom.ClassLibrary.Controls
{
	public class ControlsHelper
	{
		private const int WM_SETREDRAW = 0x000B;

		public static void SuspendDrawing(Control control)
		{
			var msgSuspendUpdate = Message.Create(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
			var window = NativeWindow.FromHandle(control.Handle);
			window.DefWndProc(ref msgSuspendUpdate);
		}

		public static void ResumeDrawing(Control control)
		{
			var wparam = new IntPtr(1);
			var msgResumeUpdate = Message.Create(control.Handle, WM_SETREDRAW, wparam, IntPtr.Zero);
			var window = NativeWindow.FromHandle(control.Handle);
			window.DefWndProc(ref msgResumeUpdate);
			control.Invalidate();
		}

		public static void RebindGrid<T>(DataGridView grid, object data, string primaryKeyPropertyName = null, bool selectFirst = true, List<T> selection = null)
		{
			int rowIndex = 0;
			if (grid.Rows.Count > 0) rowIndex = grid.FirstDisplayedCell.RowIndex;
			var sel = (selection == null)
				? GetSelection<T>(grid, primaryKeyPropertyName)
				: selection;
			grid.DataSource = data;
			if (rowIndex != 0 && rowIndex < grid.Rows.Count)
			{
				grid.FirstDisplayedScrollingRowIndex = rowIndex;
			}
			RestoreSelection(grid, primaryKeyPropertyName, sel, selectFirst);
		}

		/// <summary>
		/// Get list of primary keys of items selected in the grid.
		/// </summary>
		/// <typeparam name="T">Type of Primary key.</typeparam>
		/// <param name="grid">Grid for geting selection</param>
		/// <param name="primaryKeyPropertyName">Primary key name.</param>
		public static List<T> GetSelection<T>(DataGridView grid, string primaryKeyPropertyName = null)
		{
			List<T> list = new List<T>();
			for (int i = 0; i <= grid.SelectedRows.Count - 1; i++)
			{
				var item = grid.SelectedRows[i].DataBoundItem;
				// If primary key was not specified then...
				if (primaryKeyPropertyName == null)
				{
					if (typeof(EntityObject).IsAssignableFrom(item.GetType()))
					{
						var eo = (EntityObject)item;
						primaryKeyPropertyName = eo.EntityKey.EntityKeyValues[0].Key;
					}
				}
				var val = GetValue<T>(item, primaryKeyPropertyName);
				list.Add(val);
			}
			return list;
		}

		public static void RestoreSelection<T>(DataGridView grid, string primaryKeyPropertyName, List<T> list, bool selectFirst = true)
		{
			// Restore selections
			if (list.Count == 0)
				return;
			DataGridViewRow firstVisibleRow = null;
			for (int i = 0; i <= grid.Rows.Count - 1; i++)
			{
				var row = grid.Rows[i];
				if ((firstVisibleRow == null && row.Visible))
					firstVisibleRow = row;
				var item = row.DataBoundItem;
				// If primary key was not specified then...
				if (primaryKeyPropertyName == null)
				{
					if (typeof(EntityObject).IsAssignableFrom(item.GetType()))
					{
						var eo = (EntityObject)item;
						primaryKeyPropertyName = eo.EntityKey.EntityKeyValues[0].Key;
					}
				}
				var val = GetValue<T>(item, primaryKeyPropertyName);
				if (list.Contains(val) != row.Selected)
				{
					var selected = list.Contains(val);
					// Select visible rows only, because invisible rows can't be selected or they will throw exception:
					// Row associated with the currency manager's position cannot be made invisible.'
					row.Selected = selected && row.Visible;
				}
			}
			if (selectFirst && grid.Rows.Count > 0 && grid.SelectedRows.Count == 0 && firstVisibleRow != null)
			{
				firstVisibleRow.Selected = true;
			}
		}

		private static T GetValue<T>(object item, string dataPropertyName)
		{
			object val = null;
			if (item is DataRowView)
			{
				var row = ((DataRowView)item).Row;
				if (!row.IsNull(dataPropertyName))
				{
					val = (T)row[dataPropertyName];
				}
			}
			else
			{
				var pi = item.GetType().GetProperty(dataPropertyName);
				val = (T)pi.GetValue(item, null);
			}
			return (T)val;
		}

		#region "UserControl is Visible"

		public static bool IsControlVisibleOnForm(Control control)
		{
			if (control == null) return false;
			if (!control.IsHandleCreated) return false;
			if (control.Parent == null) return false;
			var pointsToCheck = GetPoints(control, true);
			foreach (var p in pointsToCheck)
			{
				var child = control.Parent.GetChildAtPoint(p);
				if (child == null) continue;
				if (control == child || control.Contains(child)) return true;
			}
			return false;
		}

		public static POINT[] GetPoints(Control control, bool relative = false)
		{
			var pos = relative
				? System.Drawing.Point.Empty
				// Get control position on the screen
				: control.PointToScreen(System.Drawing.Point.Empty);
			var pointsToCheck =
				new POINT[]
					{
						// Top-Left.
						pos,
						// Top-Right.
						new POINT(pos.X + control.Width - 1, pos.Y),
						// Bottom-Left.
						new POINT(pos.X, pos.Y + control.Height - 1),
						// Bottom-Right.
						new POINT(pos.X + control.Width - 1, pos.Y + control.Height - 1),
						// Middle-Centre.
						new POINT(pos.X + control.Width/2, pos.Y + control.Height/2)
					};
			return pointsToCheck;
		}

		public static bool IsControlVisibleToUser(Control control)
		{
			if (!control.IsHandleCreated) return false;
			var pointsToCheck = GetPoints(control);
			foreach (var p in pointsToCheck)
			{
				var hwnd = JocysCom.ClassLibrary.Win32.NativeMethods.WindowFromPoint(p);
				var other = Control.FromChildHandle(hwnd);
				if (other == null) continue;
				if (GetAll(control, null, true).Contains(other)) return true;
			}
			return false;
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static IEnumerable<Control> GetAll(Control control, Type type = null, bool includeTop = false)
		{
			// Get all child controls.
			var controls = control.Controls.Cast<Control>();
			return controls
				// Get children controls and flatten resulting sequences into one sequence.
				.SelectMany(x => GetAll(x))
				// Merge controls with their children.
				.Concat(controls)
				// Include top control if required.
				.Concat(includeTop ? new[] { control } : new Control[0])
				// Filter controls by type.
				.Where(x => type == null || (type.IsInterface ? x.GetType().GetInterfaces().Contains(type) : type.IsAssignableFrom(x.GetType())));
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(Control control, bool includeTop = false)
		{
			if (control == null) return new T[0];
			var type = typeof(T);
			// Get all child controls.
			var controls = control.Controls.Cast<Control>();
			// Get children of controls and flatten resulting sequences into one sequence.
			var result = controls.SelectMany(x => GetAll(x)).ToArray();
			// Merge controls with their children.
			result = result.Concat(controls).ToArray();
			// Include top control if required.
			if (includeTop) result = result.Concat(new[] { control }).ToArray();
			// Filter controls by type.
			result = type.IsInterface
				? result.Where(x => x.GetType().GetInterfaces().Contains(type)).ToArray()
				: result.Where(x => type.IsAssignableFrom(x.GetType())).ToArray();
			// Cast to required type.
			var result2 = result.Select(x => (T)(object)x).ToArray();
			return result2;
		}

		#endregion

		#region Web Controls

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static IEnumerable<System.Web.UI.Control> GetAll(System.Web.UI.Control control, Type type = null, bool includeTop = false)
		{
			// Get all child controls.
			var controls = control.Controls.Cast<System.Web.UI.Control>();
			return controls
				// Get children controls and flatten resulting sequences into one sequence.
				.SelectMany(x => GetAll(x))
				// Merge controls with their children.
				.Concat(controls)
				// Include top control if required.
				.Concat(includeTop ? new[] { control } : new System.Web.UI.Control[0])
				// Filter controls by type.
				.Where(x => type == null || (type.IsInterface ? x.GetType().GetInterfaces().Contains(type) : type.IsAssignableFrom(x.GetType())));
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(System.Web.UI.Control control, bool includeTop = false)
		{
			if (control == null) return new T[0];
			var type = typeof(T);
			// Get all child controls.
			var controls = control.Controls.Cast<System.Web.UI.Control>();
			// Get children of controls and flatten resulting sequences into one sequence.
			var result = controls.SelectMany(x => GetAll(x)).ToArray();
			// Merge controls with their children.
			result = result.Concat(controls).ToArray();
			// Include top control if required.
			if (includeTop) result = result.Concat(new[] { control }).ToArray();
			// Filter controls by type.
			result = type.IsInterface
				? result.Where(x => x.GetType().GetInterfaces().Contains(type)).ToArray()
				: result.Where(x => type.IsAssignableFrom(x.GetType())).ToArray();
			// Cast to required type.
			var result2 = result.Select(x => (T)(object)x).ToArray();
			return result2;
		}

		#endregion

	}
}
