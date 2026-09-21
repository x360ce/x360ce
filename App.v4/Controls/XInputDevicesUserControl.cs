using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.DInput;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The four XInput places, what is in them, and a way to ask for a different order.
	/// </summary>
	/// <remarks>
	/// A controller tab stands for an XInput place, and its mappings only reach a game if the
	/// controller made for it holds that place. XInput gives out places when devices arrive and
	/// cannot be asked for one, so a real controller plugged in first takes the place a tab expected
	/// and everything mapped there goes nowhere.
	///
	/// This shows what actually holds each place, and lets somebody say what they want instead. The
	/// order is achieved the only way XInput allows: take things away, then bring them back in the
	/// order asked for.
	/// </remarks>
	public partial class XInputDevicesUserControl : UserControl
	{
		public XInputDevicesUserControl()
		{
			InitializeComponent();
			if (ControlsHelperDesignMode())
				return;
			Refresh_Click(null, null);
		}

		static bool ControlsHelperDesignMode()
		{
			return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(new Form());
		}

		readonly List<XInputReorderPlan.Entry> _entries = new List<XInputReorderPlan.Entry>();

		/// <summary>Reads the machine, on a worker, and shows what is there.</summary>
		/// <remarks>
		/// Off this thread, because this is the thread Windows asks whether a device may be removed.
		/// Reading the device tree here while a removal is under way leaves that question unanswered
		/// and the program stuck.
		/// </remarks>
		void Reload()
		{
			System.Threading.Tasks.Task.Run(() => XInputReorderPlan.ReadEntries()).ContinueWith(read =>
			{
				if (read.IsFaulted)
				{
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(read.Exception.GetBaseException());
					return;
				}
				_entries.Clear();
				_entries.AddRange(read.Result);
				Bind();
			}, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
		}


		void Bind()
		{
			var selected = DevicesDataGridView.CurrentRow == null ? -1 : DevicesDataGridView.CurrentRow.Index;
			DevicesDataGridView.Rows.Clear();
			for (var i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];
				// Each row carries its own arrows, blank where there is nowhere to go.
				var index = DevicesDataGridView.Rows.Add(
					i > 0 ? Properties.Resources.nav_up_16x16 : null,
					i < _entries.Count - 1 ? Properties.Resources.nav_down_16x16 : null,
					XInputPlaces.Describe(entry.Place, entry.IsVirtual, entry.IsOurs),
					entry.Name);
				DevicesDataGridView.Rows[index].Tag = entry;
			}
			if (selected >= 0 && selected < DevicesDataGridView.Rows.Count)
				DevicesDataGridView.Rows[selected].Selected = true;
			UpdateButtons();
		}

		void UpdateButtons()
		{
			ApplyButton.Enabled = _entries.Count > 0;
		}

		void Move(int from, int by)
		{
			var to = from + by;
			if (from < 0 || from >= _entries.Count || to < 0 || to >= _entries.Count)
				return;
			var moved = _entries[from];
			_entries.RemoveAt(from);
			_entries.Insert(to, moved);
			Bind();
			DevicesDataGridView.CurrentCell = DevicesDataGridView.Rows[to].Cells[PlaceColumn.Index];
		}

		private void DevicesDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0)
				return;
			if (e.ColumnIndex == MoveUpColumn.Index)
				Move(e.RowIndex, -1);
			else if (e.ColumnIndex == MoveDownColumn.Index)
				Move(e.RowIndex, 1);
		}

		/// <summary>Reads the machine again, for when a controller has arrived or left.</summary>
		public void ReloadPlaces()
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action)ReloadPlaces);
				return;
			}
			Refresh_Click(null, null);
		}

		private void Refresh_Click(object sender, EventArgs e)
		{
			try { Reload(); }
			catch (Exception ex) { JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex); }
		}
		/// <summary>Holds the buttons still while a reorder is under way.</summary>
		void SetBusy(bool busy)
		{
			Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
			ApplyButton.Enabled = !busy;
			DevicesDataGridView.Enabled = !busy;
			if (!busy)
				UpdateButtons();
		}


		private void DevicesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		private void ApplyButton_Click(object sender, EventArgs e)
		{
			var plan = XInputReorderPlan.For(_entries);
			// Shown before anything is touched, because a controller switched off cannot be taken
			// back by pressing Cancel.
			var text = plan.ToString();
			// Every box here is owned by this window, so it opens in front of it even when the window
			// is kept on top. Unowned, the closing report opened behind, with the buttons still held
			// still from the run, and the window looked as if it had stopped.
			if (plan.Refusal != null)
			{
				MessageBox.Show(this, text, "Cannot put them in that order",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if (plan.Steps.Count == 0)
			{
				MessageBox.Show(this, text, "Nothing to do", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			var answer = MessageBox.Show(this,
				"This is what will happen:" + Environment.NewLine + Environment.NewLine + text
				+ Environment.NewLine + Environment.NewLine + "Go ahead?",
				"Put controllers in this order", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (answer != DialogResult.OK)
				return;
			// Nothing is said about Administrator here. Switching a real controller off needs it, and a copy
			// of this program is run for that one step and closes again - so the work happens without losing
			// whatever was open, and without the program carrying that power around afterwards. Windows asks
			// once per step, and only when this program is not Administrator already.
			var runner = new XInputReorderRunner();
			// Said as it happens, on the window, rather than found out afterwards. Each step waits for
			// Windows to build or remove a device - seconds each, and several of them - so a window that
			// says nothing is a window somebody reasonably decides has stopped working, in the middle of
			// their controllers switching off around them.
			runner.Progress = what => BeginInvoke((Action)(() =>
			{
				StatusLabel.Visible = true;
				StatusLabel.Text = what;
			}));
			SetBusy(true);
			var task = System.Threading.Tasks.Task.Run(() => runner.Run(plan));
			// Off this thread, because it is the thread that paints. Run here, nothing is drawn for the
			// half minute this takes and Windows greys the window out as not responding.
			task.ContinueWith(finished =>
			{
				SetBusy(false);
				StatusLabel.Visible = false;
				Reload();
				MessageBox.Show(this, runner.ToString(),
					finished.Result ? "Done" : "Stopped part way",
					MessageBoxButtons.OK, finished.Result ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
			}, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}
