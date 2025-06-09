using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.App
{
	public partial class SettingsManager
	{

        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        //public Action<Control> NotifySettingsChange;
		public Action<int> NotifySettingsStatus;

		object eventsLock = new object();
		// Events are suspended (not attached by default).
		int eventsSuspendCount = 1;

		public void SuspendEvents()
		{
			lock (eventsLock)
			{
				eventsSuspendCount++;
				NotifySettingsStatus?.Invoke(eventsSuspendCount);
				// If events already suspended then return.
				if (eventsSuspendCount > 1)
					return;
				// Don't allow controls to fire events.
				var controls = Current.SettingsMap.Select(x => x.Control).ToArray();
				foreach (var control in controls)
				{
					if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged -= new EventHandler(Control_ValueChanged);
					if (control is ListBox) ((ListBox)control).SelectedIndexChanged -= new EventHandler(Control_SelectedIndexChanged);
					if (control is TrackBar) ((TrackBar)control).ValueChanged -= new EventHandler(Control_ValueChanged);
					if (control is CheckBox) ((CheckBox)control).CheckedChanged -= new EventHandler(Control_CheckedChanged);
					if (control is ComboBox)
					{
						var cbx = (ComboBox)control;
						if (cbx.DropDownStyle == ComboBoxStyle.DropDownList)
						{
							cbx.SelectedIndexChanged -= new EventHandler(Control_TextChanged);
						}
						else
						{
							cbx.TextChanged -= new EventHandler(Control_TextChanged);
						}
					}
					if (control is DataGridView)
					{
						var grid = (DataGridView)control;
						grid.CellClick -= DataGridView_CellClick;
					}
				}
			}
		}

        public void RaiseSettingsChanged(Control control)
        {
            var ev = SettingChanged;
            if (ev == null)
                return;
            var map = SettingsMap.FirstOrDefault(x => x.Control == control);
            var e = new SettingChangedEventArgs(map);
            ev(this, e);
        }

        public void ResumeEvents()
		{
			lock (eventsLock)
			{
				eventsSuspendCount--;
				NotifySettingsStatus?.Invoke(eventsSuspendCount);
				// If events must be suspended then return.
				if (eventsSuspendCount > 0)
					return;
				if (eventsSuspendCount < 0)
					throw new Exception("ResumeEvents() executed multiple times.");
				// Allow controls to fire events.
				var controls = SettingsManager.Current.SettingsMap.Select(x => x.Control);
				foreach (var control in controls)
				{
					if (control is NumericUpDown) ((NumericUpDown)control).ValueChanged += new EventHandler(Control_ValueChanged);
					if (control is ListBox) ((ListBox)control).SelectedIndexChanged += new EventHandler(Control_SelectedIndexChanged);
					if (control is TrackBar) ((TrackBar)control).ValueChanged += new EventHandler(Control_ValueChanged);
					if (control is CheckBox) ((CheckBox)control).CheckedChanged += new EventHandler(Control_CheckedChanged);
					if (control is ComboBox)
					{
						var cbx = (ComboBox)control;
						if (cbx.DropDownStyle == ComboBoxStyle.DropDownList)
						{
							cbx.SelectedIndexChanged += new EventHandler(Control_TextChanged);
						}
						else
						{
							cbx.TextChanged += new EventHandler(Control_TextChanged);
						}
					}
					if (control is DataGridView)
					{
						var grid = (DataGridView)control;
						grid.CellClick += DataGridView_CellClick;
					}
				}
			}
		}

		Dictionary<string, int> ListBoxCounts = new Dictionary<string, int>();

		/// <summary>Monitor changes remove/add inside ListBoxes.</summary>
		void Control_SelectedIndexChanged(object sender, EventArgs e)
		{
			lock (ListBoxCounts)
			{
				var lb = (ListBox)sender;
				// If list contains count of ListBoxes items.			
				if (ListBoxCounts.ContainsKey(lb.Name))
				{
					// If ListBoxe haven't changed then return;
					if (ListBoxCounts[lb.Name] == lb.Items.Count) return;
					ListBoxCounts[lb.Name] = lb.Items.Count;
				}
				else
				{
					ListBoxCounts.Add(lb.Name, lb.Items.Count);
				}
			}
            // Save setting and notify if value changed.
            RaiseSettingsChanged((Control)sender);
		}

		void Control_TextChanged(object sender, EventArgs e)
		{
            // Notify about form value change.
            RaiseSettingsChanged((Control)sender);
		}

		void Control_ValueChanged(object sender, EventArgs e)
		{
            // Notify about form value change.
            RaiseSettingsChanged((Control)sender);
		}

		void Control_CheckedChanged(object sender, EventArgs e)
		{
            // Notify about form value change.
            RaiseSettingsChanged((Control)sender);
		}

		/// <summary>
		/// This event will fire after similar event attached on the PadControl, because it was attached later.
		/// </summary>
		private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			// If user clicked on the CheckBox column then...
			if (grid.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
			{
                // Notify about form value change.
                RaiseSettingsChanged((Control)sender);
			}
		}

	}
}
