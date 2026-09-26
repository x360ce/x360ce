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
			SettingsManager.UserSettings.Items.ListChanged += UserSettings_ListChanged;
			SettingsManager.CurrentGame_PropertyChanged += CurrentGame_PropertyChanged;
			SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
			// Let go of, because the lists outlive every panel that listens to them.
			Disposed += (sender, e) =>
			{
				SettingsManager.UserSettings.Items.ListChanged -= UserSettings_ListChanged;
				SettingsManager.CurrentGame_PropertyChanged -= CurrentGame_PropertyChanged;
				SettingsManager.Options.PropertyChanged -= Options_PropertyChanged;
			};
		}

		/// <summary>A device was mapped or unmapped, so the names are shown again. Nothing on the machine moved.</summary>
		void UserSettings_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
		{
			var property = e.PropertyDescriptor?.Name;
			if (e.ListChangedType == System.ComponentModel.ListChangedType.ItemChanged
				&& property != nameof(Engine.Data.UserSetting.MapTo)
				&& property != nameof(Engine.Data.UserSetting.InstanceGuid))
				return;
			if (IsDisposed)
				return;
			if (InvokeRequired)
				BeginInvoke((Action)Bind);
			else
				Bind();
		}

		/// <summary>The game, or which tabs it emulates, changed, so which tabs are waiting is worked out again.</summary>
		void CurrentGame_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var property = e?.PropertyName;
			if (property == null || property == nameof(Engine.Data.UserGame.EnableMask)
				|| property == nameof(Engine.Data.UserGame.EmulationType))
				ReloadPlaces();
		}

		/// <summary>The master switch for emulated controllers decides which tabs are waiting too.</summary>
		void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Options.XInputEnabled))
				ReloadPlaces();
		}

		/// <summary>The devices mapped to the tab a virtual controller of ours is made for, or nothing for any other row.</summary>
		static string MappedText(XInputReorderPlan.Entry entry)
		{
			if (!entry.IsVirtual || !entry.IsOurs || entry.Pad < 1 || entry.Pad > 4)
				return "";
			var names = XInputReorderPlan.ProductNames(entry.Pad);
			return names.Length == 0 ? "Nothing mapped" : names;
		}

		/// <summary>The shield Windows puts on anything that asks for Administrator.</summary>
		static readonly System.Drawing.Image AdministratorImage =
			new System.Drawing.Icon(System.Drawing.SystemIcons.Shield, 16, 16).ToBitmap();

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
			// Not while controllers are being put in order: reading the places asks XInput, which opens
			// every controller it answers about, and one held open cannot be switched off cleanly. The
			// list is read once the order is done.
			if (_running)
				return;
			System.Threading.Tasks.Task.Run(() => XInputReorderPlan.ReadEntries()).ContinueWith(read =>
			{
				if (read.IsFaulted)
				{
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(read.Exception.GetBaseException());
					return;
				}
				// The order waiting for OK stays in view. Replaced by what is there now, the list would no
				// longer show what OK does.
				if (_proposed != null)
					return;
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
					entry.Waiting
						? string.Format("Virtual {0} (waiting)", entry.Pad)
						: XInputPlaces.Describe(entry.Place, entry.IsVirtual, entry.IsOurs),
					entry.Controller > 0 ? string.Format("Controller {0}", entry.Controller) : "",
					entry.Name,
					MappedText(entry));
				DevicesDataGridView.Rows[index].Tag = entry;
			}
			if (selected >= 0 && selected < DevicesDataGridView.Rows.Count)
				DevicesDataGridView.Rows[selected].Selected = true;
			UpdateButtons();
		}

		/// <summary>Holds the list and its buttons still while the notice under it is open.</summary>
		void UpdateButtons()
		{
			var idle = !_noticeOpen;
			ApplyButton.Enabled = idle && _entries.Count > 0;
			AutoOrderButton.Enabled = idle && _entries.Count > 0;
			RefreshButton.Enabled = idle;
			DevicesDataGridView.Enabled = idle;
		}

		void MoveRow(int from, int by)
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
				MoveRow(e.RowIndex, -1);
			else if (e.ColumnIndex == MoveDownColumn.Index)
				MoveRow(e.RowIndex, 1);
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

		private void DevicesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		/// <summary>Lists the controllers in the order of their tabs, then offers to put them in it.</summary>
		private void AutoOrderButton_Click(object sender, EventArgs e)
		{
			var ordered = XInputReorderPlan.ByController(_entries);
			_entries.Clear();
			_entries.AddRange(ordered);
			Bind();
			ApplyButton_Click(sender, e);
		}

		private void ApplyButton_Click(object sender, EventArgs e)
		{
			var plan = XInputReorderPlan.For(_entries);
			if (plan.Refusal != null)
				ShowNotice("Cannot put them in that order", plan.ToString(), false);
			else if (plan.Steps.Count == 0)
				ShowNotice("Nothing to do", plan.ToString(), false);
			else
			{
				// Shown before anything is touched, because a controller switched off cannot be taken
				// back by pressing Cancel.
				_proposed = plan;
				ShowNotice("Put controllers in this order?", plan.ToString(), true);
				// The shield says, the way it does everywhere in Windows, that OK asks for Administrator.
				if (plan.NeedsElevation && !JocysCom.ClassLibrary.Security.PermissionHelper.IsElevated)
					PlanOkButton.Image = AdministratorImage;
			}
		}

		#region Notice under the list

		/// <summary>The order waiting for OK, or null.</summary>
		XInputReorderPlan _proposed;

		/// <summary>Whether controllers are being put in order now.</summary>
		bool _running;

		/// <summary>The height without the notice, to go back to when it closes.</summary>
		int _restingHeight;

		/// <summary>Whether the notice is open.</summary>
		/// <remarks>
		/// Kept here rather than read from the panel, which reports itself hidden whenever another tab is
		/// in front. Read that way, a reorder watched from another tab took the grown height for the
		/// resting one at every step, and the page grew each time.
		/// </remarks>
		bool _noticeOpen;

		/// <summary>Opens the notice under the list, or changes what it says.</summary>
		/// <remarks>
		/// Part of the page rather than a window of its own. A separate box opened behind the program
		/// when it was kept on top, and held up anything driving it until somebody found the box; here
		/// the order being asked about stays in view above it, and its buttons are reached the same way
		/// as every other button in the program.
		/// </remarks>
		/// <param name="asking">True offers Cancel beside OK. OK then carries out <see cref="_proposed"/>.</param>
		/// <param name="closable">False hides both buttons, for while the order is being made.</param>
		void ShowNotice(string subject, string body, bool asking, bool closable = true)
		{
			if (!_noticeOpen)
				_restingHeight = Height;
			_noticeOpen = true;
			PlanOkButton.Image = Properties.Resources.ok_16x16;
			PlanSubjectLabel.Text = subject;
			PlanBodyLabel.Text = body;
			PlanOkButton.Visible = closable;
			PlanCancelButton.Visible = closable && asking;
			PlanPanel.Visible = true;
			UpdateButtons();
			FitNotice();
		}

		void CloseNotice()
		{
			_noticeOpen = false;
			PlanPanel.Visible = false;
			Height = _restingHeight;
			UpdateButtons();
		}

		/// <summary>Grows the whole panel by the notice, so the list above it keeps its height.</summary>
		/// <remarks>
		/// Asked of the notice at the page's width rather than read off the notice after it is laid out.
		/// A height set while the page is being laid out is not laid out again, and the notice was left
		/// drawn over the list with the room made for it empty below.
		/// </remarks>
		void FitNotice()
		{
			if (!_noticeOpen)
				return;
			var wanted = _restingHeight + PlanPanel.GetPreferredSize(new System.Drawing.Size(ClientSize.Width, 0)).Height;
			if (Height != wanted)
				Height = wanted;
		}

		/// <summary>A narrower page wraps the notice onto more lines, so it is fitted again once the width has settled.</summary>
		protected override void OnClientSizeChanged(EventArgs e)
		{
			base.OnClientSizeChanged(e);
			if (_noticeOpen && IsHandleCreated)
				BeginInvoke((Action)FitNotice);
		}

		private void PlanCancelButton_Click(object sender, EventArgs e)
		{
			_proposed = null;
			CloseNotice();
			// The list showed the order asked for, which Cancel has just thrown away.
			Reload();
		}

		private void PlanOkButton_Click(object sender, EventArgs e)
		{
			var plan = _proposed;
			_proposed = null;
			if (plan == null)
			{
				CloseNotice();
				return;
			}
			// Nothing is said about Administrator here. Switching a real controller off needs it, and a copy
			// of this program does the switching and closes when the order is done - so the work happens
			// without losing whatever was open, and without the program carrying that power around
			// afterwards. Windows asks once, and only when this program is not Administrator already.
			var runner = new XInputReorderRunner();
			// Said as it happens, rather than found out afterwards. Each step waits for Windows to build or
			// remove a device - seconds each, and several of them - so a page that says nothing is a page
			// somebody reasonably decides has stopped working, in the middle of their controllers switching
			// off around them.
			var said = new List<string>();
			runner.Progress = what => BeginInvoke((Action)(() =>
			{
				said.Add(what);
				ShowNotice("Putting controllers in order...", string.Join(Environment.NewLine, said), false, false);
			}));
			ShowNotice("Putting controllers in order...", "", false, false);
			_running = true;
			var task = System.Threading.Tasks.Task.Run(() => runner.Run(plan));
			// Off this thread, because it is the thread that paints. Run here, nothing is drawn for the
			// half minute this takes and Windows greys the window out as not responding.
			task.ContinueWith(finished =>
			{
				_running = false;
				var done = !finished.IsFaulted && finished.Result;
				if (finished.IsFaulted)
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(finished.Exception.GetBaseException());
				var report = finished.IsFaulted
					? runner.ToString() + Environment.NewLine + finished.Exception.GetBaseException().Message
					: runner.ToString();
				ShowNotice(done ? "Done" : "Stopped part way", report.TrimEnd(), false);
				Reload();
			}, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
		}

		#endregion
	}
}
