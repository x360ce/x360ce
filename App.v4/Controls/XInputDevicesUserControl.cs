using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
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

		/// <summary>Reads the machine and shows what is there.</summary>
		void Reload()
		{
			_entries.Clear();
			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
			var byId = all.ToDictionary(x => x.DeviceId, x => x, StringComparer.OrdinalIgnoreCase);
			var places = XInputPlaces.Resolve(all, byId);

			// One row per piece of hardware, not per face. A controller is several devices and a
			// person thinks of it as one thing.
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var device in all.Where(XInputPlaces.IsXInputCapable))
			{
				var hardware = XInputPlaces.HardwareOf(device, byId);
				if (!seen.Add(hardware))
					continue;
				int place;
				if (!places.TryGetValue(hardware, out place))
					place = XInputPlaces.Unknown;
				DeviceInfo hardwareInfo;
				var name = byId.TryGetValue(hardware, out hardwareInfo) && !string.IsNullOrEmpty(hardwareInfo.Description)
					? hardwareInfo.Description
					: device.Description;
				_entries.Add(new XInputReorderPlan.Entry
				{
					HardwareId = hardware,
					Name = name,
					IsVirtual = VirtualDriverInstaller.IsVirtualPad(device, byId),
					IsOurs = VirtualDriverInstaller.IsOneOfOurs(device, byId),
					Pad = PadHolding(place),
					Place = place,
				});
			}
			// Shown in the order XInput has them, with anything unplaced after. That is the order a
			// game sees, which is the order worth arguing with.
			_entries.Sort((a, b) =>
			{
				var pa = a.Place < 0 ? int.MaxValue : a.Place;
				var pb = b.Place < 0 ? int.MaxValue : b.Place;
				return pa != pb ? pa.CompareTo(pb) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			});
			Bind();
		}
		/// <summary>Which controller tab has its controller in this place, or zero.</summary>
		/// <remarks>
		/// Asked of what was watched rather than worked out from the number: the place a tab's controller
		/// is in is not the tab's own number, which is the whole reason this page exists.
		/// </remarks>
		static int PadHolding(int place)
		{
			var helper = Global.DHelper;
			if (helper == null || place < 0)
				return 0;
			for (var pad = 0; pad < helper.XiPlaceForPad.Length; pad++)
				if (helper.XiPlaceForPad[pad] == place)
					return pad + 1;
			return 0;
		}


		void Bind()
		{
			var selected = DevicesDataGridView.CurrentRow == null ? -1 : DevicesDataGridView.CurrentRow.Index;
			DevicesDataGridView.Rows.Clear();
			foreach (var entry in _entries)
			{
				var index = DevicesDataGridView.Rows.Add(
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
			var row = DevicesDataGridView.CurrentRow;
			var index = row == null ? -1 : row.Index;
			MoveUpButton.Enabled = index > 0;
			MoveDownButton.Enabled = index >= 0 && index < DevicesDataGridView.Rows.Count - 1;
			ApplyButton.Enabled = _entries.Count > 0;
		}

		void Move(int by)
		{
			var row = DevicesDataGridView.CurrentRow;
			if (row == null)
				return;
			var from = row.Index;
			var to = from + by;
			if (to < 0 || to >= _entries.Count)
				return;
			var moved = _entries[from];
			_entries.RemoveAt(from);
			_entries.Insert(to, moved);
			Bind();
			DevicesDataGridView.CurrentCell = DevicesDataGridView.Rows[to].Cells[0];
		}

		private void MoveUpButton_Click(object sender, EventArgs e) { Move(-1); }

		private void MoveDownButton_Click(object sender, EventArgs e) { Move(1); }

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
			MoveUpButton.Enabled = !busy;
			MoveDownButton.Enabled = !busy;
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
			if (plan.Refusal != null)
			{
				MessageBox.Show(text, "Cannot put them in that order",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if (plan.Steps.Count == 0)
			{
				MessageBox.Show(text, "Nothing to do", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			var answer = MessageBox.Show(
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
				MessageBox.Show(runner.ToString(),
					finished.Result ? "Done" : "Stopped part way",
					MessageBoxButtons.OK, finished.Result ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
			}, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}
