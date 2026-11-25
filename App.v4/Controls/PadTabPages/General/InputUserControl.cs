using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The INPUT column of the General tab: every button, axis, slider and POV the selected
	/// device has, lit while it is used, with its reading, before anything is mapped.
	/// </summary>
	/// <remarks>
	/// Until a control was mapped nothing on the General tab moved when the device did, so the
	/// program looked dead. This shows the device itself. It is fed by the same interface tick as
	/// the rest of the tab, reads the state the engine last published, and takes no lock the
	/// engine takes. The layout follows v5's General tab so the two lines read alike.
	/// </remarks>
	public class InputUserControl : UserControl
	{
		public InputUserControl()
		{
			SuspendLayout();
			Name = "InputUserControl";
			// The control scrolls, not the table: a table that scrolls stretches its last row to the height it last scrolled
			// to, and keeps its scroll bar when a device with fewer controls is shown. The table is as tall as its rows.
			AutoScroll = true;
			var table = new TableLayoutPanel
			{
				Name = "LayoutTable",
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 1,
				Margin = new Padding(0),
			};
			table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			SourceLabel = new Label
			{
				Name = nameof(SourceLabel),
				AutoSize = true,
				Dock = DockStyle.Top,
				AutoEllipsis = true,
				Margin = new Padding(8, 8, 0, 0),
				Text = SourceNone,
			};
			table.Controls.Add(SourceLabel, 0, 0);
			table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			ButtonsGroupBox = AddGroup(table, "ButtonsGroupBox", "BUTTON", "ButtonChips", out ButtonChips, false);
			AxesGroupBox = AddGroup(table, "AxesGroupBox", "AXIS", "AxisChips", out AxisChips, true);
			SlidersGroupBox = AddGroup(table, "SlidersGroupBox", "SLIDER", "SliderChips", out SliderChips, true);
			PovsGroupBox = AddGroup(table, "PovsGroupBox", "POV", "PovChips", out PovChips, true);
			table.RowCount = table.RowStyles.Count;
			Controls.Add(table);
			ResumeLayout(false);
			if (ControlsHelper.IsDesignMode(this))
				return;
			foreach (var group in Groups)
			{
				group.ChipClicked += Group_ChipClicked;
				group.DragStarted += Group_DragStarted;
				group.DragEnded += Group_DragEnded;
			}
		}

		/// <summary>The header names the device the chips belong to, the way the grid names it.</summary>
		public const string SourcePrefix = "Source: ";
		public const string SourceNone = SourcePrefix + "no device selected";

		public readonly Label SourceLabel;
		public readonly GroupBox ButtonsGroupBox, AxesGroupBox, SlidersGroupBox, PovsGroupBox;
		public readonly InputChipGroup ButtonChips, AxisChips, SliderChips, PovChips;

		IEnumerable<InputChipGroup> Groups
		{
			get { return new[] { ButtonChips, AxisChips, SliderChips, PovChips }; }
		}

		/// <summary>Raised when a chip is clicked or dropped; its payload is the text that maps it.</summary>
		public event EventHandler<EventArgs<InputChip>> ChipClicked;

		/// <summary>Raised with true when a chip starts travelling and false when it lands, so the places it may land can show themselves.</summary>
		public event EventHandler<EventArgs<bool>> DraggingChanged;

		void Group_DragStarted(object sender, EventArgs e)
		{
			var handler = DraggingChanged;
			if (handler != null)
				handler(this, new EventArgs<bool>(true));
		}

		void Group_DragEnded(object sender, EventArgs e)
		{
			var handler = DraggingChanged;
			if (handler != null)
				handler(this, new EventArgs<bool>(false));
		}

		static GroupBox AddGroup(TableLayoutPanel table, string name, string caption, string chipsName, out InputChipGroup chips, bool showsValues)
		{
			// The group and its chips are hidden while they hold no chip, as they do until a device is shown (UpdateFrom). The
			// chips' strip is an item of the group, one gap inside it, in a table of its own, which keeps its margin; the page
			// scales these sizes, made at 100 %.
			chips = new InputChipGroup
			{
				Name = chipsName,
				Dock = DockStyle.Top,
				Margin = new Padding(8, 8, 0, 0),
				ShowsValues = showsValues,
				Visible = false,
			};
			var layout = new TableLayoutPanel
			{
				Name = chipsName + "LayoutPanel",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				ColumnCount = 1,
				Margin = new Padding(0),
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.Controls.Add(chips, 0, 0);
			var group = new GroupBox
			{
				Name = name,
				Text = caption,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				Margin = new Padding(8, 8, 0, 0),
				Visible = false,
			};
			group.Controls.Add(layout);
			table.Controls.Add(group, 0, table.RowStyles.Count);
			table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			return group;
		}

		/// <summary>Whether the chip with this payload is lit: its control moving or pressed. False for null or a chip the device lacks.</summary>
		/// <param name="payload">What maps the control, as <see cref="InputChips.PayloadOf"/> gives it.</param>
		public bool IsLit(string payload)
		{
			if (payload == null)
				return false;
			foreach (var group in Groups)
				foreach (var chip in group.Chips)
					if (chip.Payload == payload)
						return chip.Lit;
			return false;
		}

		void Group_ChipClicked(object sender, EventArgs<InputChip> e)
		{
			var handler = ChipClicked;
			if (handler != null)
				handler(this, e);
		}

		Guid _InstanceGuid;
		int _ButtonCount, _AxisMask, _SliderMask, _PovCount;

		static string SourceName(UserDevice ud)
		{
			return string.IsNullOrEmpty(ud.ProductName) ? ud.InstanceName : ud.ProductName;
		}

		/// <summary>
		/// Brings the panel in line with the device: rebuilds the chips when the device or what it
		/// reports has changed, then lights them from its last published state and the presses the
		/// engine counted since the last tick.
		/// </summary>
		/// <remarks>
		/// Called on every interface tick from the pad, on the interface thread. The device's
		/// state is read as a whole reference, the way the rest of the tab reads it; nothing here
		/// waits on the engine.
		/// </remarks>
		public void UpdateFrom(UserDevice ud)
		{
			var guid = ud == null ? Guid.Empty : ud.InstanceGuid;
			var buttons = ud == null ? 0 : ud.CapButtonCount;
			var axes = ud == null ? 0 : ud.DiAxeMask;
			var sliders = ud == null ? 0 : ud.DiSliderMask;
			var povs = ud == null ? 0 : ud.CapPovCount;
			if (guid != _InstanceGuid || buttons != _ButtonCount || axes != _AxisMask || sliders != _SliderMask || povs != _PovCount)
			{
				_InstanceGuid = guid;
				_ButtonCount = buttons;
				_AxisMask = axes;
				_SliderMask = sliders;
				_PovCount = povs;
				var chips = InputChips.Create(buttons, axes, sliders, povs);
				ButtonChips.Chips = chips.Where(c => c.Kind == InputChipKind.Button).ToList();
				AxisChips.Chips = chips.Where(c => c.Kind == InputChipKind.Axis).ToList();
				SliderChips.Chips = chips.Where(c => c.Kind == InputChipKind.Slider).ToList();
				PovChips.Chips = chips.Where(c => c.Kind == InputChipKind.Pov || c.Kind == InputChipKind.PovDirection).ToList();
				// A device shows a group for each kind of control it has, and the groups below one it lacks close up; with
				// no device there is none, and the source line stands alone. The chips are hidden with their box, as the AI
				// door reads each control's own flag and would call them shown.
				foreach (var group in Groups)
				{
					var shown = group.Chips.Count > 0;
					group.Visible = shown;
					// The chips' box, round the table that holds them.
					group.Parent.Parent.Visible = shown;
				}
				ControlsHelper.SetText(SourceLabel, ud == null ? SourceNone : SourcePrefix + SourceName(ud));
			}
			var state = ud == null ? null : ud.SourceState;
			var presses = ud == null ? null : ud.ButtonPresses;
			foreach (var group in Groups)
				group.Refresh(state, presses);
		}
	}
}
