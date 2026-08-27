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
				NotifySettingsStatus(eventsSuspendCount);
				// If events already suspended then return.
				if (eventsSuspendCount > 1)
					return;
				// Don't allow controls to fire events.
				var controls = Current.SettingsMap.Select(x => x.Control).ToArray();
				foreach (var control in controls)
					SetControlEvents(control, false);
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
				NotifySettingsStatus(eventsSuspendCount);
				// If events must be suspended then return.
				if (eventsSuspendCount > 0)
					return;
				if (eventsSuspendCount < 0)
					throw new Exception("ResumeEvents() executed multiple times.");
				// Allow controls to fire events.
				var controls = SettingsManager.Current.SettingsMap.Select(x => x.Control).ToArray();
				foreach (var control in controls)
					SetControlEvents(control, true);
			}
		}

		/// <summary>
		/// Attaches or detaches the events one control reports its own changes through.
		/// </summary>
		/// <remarks>
		/// Which events those are depends on the shape the control is in. A list reports a choice, a
		/// box being typed into reports the typing, and the two are not interchangeable.
		///
		/// Detaching removes both of a combo box's events whatever shape it is in now. A box which
		/// changed shape after it was attached would otherwise keep the event it was given and lose one
		/// it never had, and would then report twice, or not at all.
		/// </remarks>
		void SetControlEvents(Control control, bool attach)
		{
			var upDown = control as NumericUpDown;
			if (upDown != null)
			{
				upDown.ValueChanged -= Control_ValueChanged;
				if (attach) upDown.ValueChanged += Control_ValueChanged;
			}
			var listBox = control as ListBox;
			if (listBox != null)
			{
				listBox.SelectedIndexChanged -= Control_SelectedIndexChanged;
				if (attach) listBox.SelectedIndexChanged += Control_SelectedIndexChanged;
			}
			var trackBar = control as TrackBar;
			if (trackBar != null)
			{
				trackBar.ValueChanged -= Control_ValueChanged;
				if (attach) trackBar.ValueChanged += Control_ValueChanged;
			}
			var checkBox = control as CheckBox;
			if (checkBox != null)
			{
				checkBox.CheckedChanged -= Control_CheckedChanged;
				if (attach) checkBox.CheckedChanged += Control_CheckedChanged;
			}
			var comboBox = control as ComboBox;
			if (comboBox != null)
			{
				comboBox.SelectedIndexChanged -= Control_TextChanged;
				comboBox.TextChanged -= Control_TextChanged;
				if (attach)
				{
					if (comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
						comboBox.SelectedIndexChanged += Control_TextChanged;
					else
						comboBox.TextChanged += Control_TextChanged;
				}
			}
			var grid = control as DataGridView;
			if (grid != null)
			{
				grid.CellClick -= DataGridView_CellClick;
				if (attach) grid.CellClick += DataGridView_CellClick;
			}
		}

		/// <summary>
		/// Listens to one control again after its shape has changed.
		/// </summary>
		/// <remarks>
		/// A mapping box becomes a box that is typed into when it is switched to a formula, and back to
		/// a list when it is switched off. The events were chosen when it was attached, long before
		/// that, so without this a formula being typed reports nothing and only reaches the controller
		/// when something else happens to save.
		/// </remarks>
		public void RewireControl(Control control)
		{
			if (control == null)
				return;
			lock (eventsLock)
			{
				SetControlEvents(control, eventsSuspendCount == 0);
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
