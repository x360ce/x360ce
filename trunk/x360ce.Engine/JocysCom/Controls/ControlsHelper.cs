using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Objects.DataClasses;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
				val = (T)row[dataPropertyName];
			}
			else
			{
				var pi = item.GetType().GetProperty(dataPropertyName);
				val = (T)pi.GetValue(item, null);
			}
			return (T)val;
		}

	}
}
