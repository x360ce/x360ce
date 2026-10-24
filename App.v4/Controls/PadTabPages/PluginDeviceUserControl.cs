using JocysCom.ClassLibrary.Mcp;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Plugins;
using x360ce.App.UiTree;
using x360ce.Plugins;

namespace x360ce.App.Controls
{
	/// <summary>
	/// A plugin device's tab on the controller page: the control its plugin declares, its MCP server, and its settings,
	/// made from its class, each on a page of its own.
	/// </summary>
	/// <remarks>
	/// The device's help, its class's [Description], heads the tab, so what to know before using the device is in view.
	/// The pages follow, those the device has: the plugin's own control first, made with the device the engine reads, so
	/// the page says so while there is none yet; MCP Server, for a kind that declares one, with the AI page's own rows;
	/// and Properties, the settings in a property grid, as Visual Studio's Properties window shows a component's: a row
	/// for each, grouped by [Category], with the selected row's help under the grid. A change is kept for the
	/// device at once, and the engine gives it to the plugin before its next read. A status is read from the device the
	/// engine reads, and the grid is drawn again whenever <see cref="RefreshStatus"/> finds a value changed.
	/// </remarks>
	public class PluginDeviceUserControl : UserControl
	{
		public PluginDeviceUserControl(Guid instanceGuid, PluginDeviceType type, IPluginDevice device, PluginSettings store)
		{
			InstanceGuid = instanceGuid;
			Type = type;
			Device = device;
			_store = store;
			Name = "PluginDevicePanel";
			// The tab scrolls rather than the table, which would size each row to its control and never shrink the pages.
			AutoScroll = true;
			SuspendLayout();
			MainTableLayoutPanel = new TableLayoutPanel
			{
				Name = "MainTableLayoutPanel",
				Dock = DockStyle.Fill,
				Padding = new Padding(6),
				ColumnCount = 1,
			};
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			if (!string.IsNullOrWhiteSpace(type.Description))
				AddRow(new Label
				{
					Name = "DescriptionLabel",
					Text = type.Description.Trim(),
					AutoSize = true,
					// Across the column, so the table wraps it at the tab's width.
					Anchor = AnchorStyles.Left | AnchorStyles.Right,
				});
			PluginTabControl = new TabControl { Name = "PluginTabControl", Dock = DockStyle.Fill };
			if (type.ControlType != null)
			{
				NoteLabel = new Label { Name = "NoteLabel", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(3), Visible = false };
				var control = NewPluginControl();
				if (control != null)
					control.Dock = DockStyle.Fill;
				// The plugin names the page with its control's Text.
				var text = control == null || string.IsNullOrWhiteSpace(control.Text) ? "Control" : control.Text;
				AddPage("ControlTabPage", text, "The control the plugin adds for the device.", control ?? NoteLabel);
			}
			var door = PluginMcpDoors.Get(instanceGuid, type, store);
			if (door != null)
			{
				McpServerUserControl = new AiAccessUserControl
				{
					Name = "McpServerUserControl",
					Dock = DockStyle.Fill,
					AccessibleName = "MCP server",
					AccessibleDescription = "The device's own MCP server, which a program or an AI assistant reaches this device alone through.",
				};
				McpServerUserControl.Bind(door);
				AddPage("McpServerTabPage", "MCP Server", "The device's own MCP server: whether it is open, its level, address, port and token.", McpServerUserControl);
			}
			if (type.Settings.Length > 0)
			{
				View = new PluginSettingsView(instanceGuid, type, device, store);
				SettingsPropertyGrid = new PropertyGrid
				{
					Name = "SettingsPropertyGrid",
					Dock = DockStyle.Fill,
					PropertySort = PropertySort.Categorized,
					AccessibleName = "Settings",
					AccessibleDescription = "The device's settings, grouped. Each row's help shows under the grid; a value kept for the device is bold.",
					SelectedObject = View,
				};
				_statuses = View.Statuses();
				_kept = View.Kept();
				var properties = new TableLayoutPanel { Name = "PropertiesTableLayoutPanel", Dock = DockStyle.Fill, ColumnCount = 1 };
				properties.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
				properties.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
				properties.Controls.Add(SettingsPropertyGrid, 0, 0);
				if (type.Settings.Any(x => x.Kind != PluginSettingKind.Status))
				{
					ResetButton = new Button
					{
						Name = "ResetButton",
						Text = "Reset",
						AutoSize = true,
						Anchor = AnchorStyles.Left,
						AccessibleName = "Reset",
						AccessibleDescription = "Gives every setting of this device the value a new device starts with.",
						UseVisualStyleBackColor = true,
					};
					ResetButton.Click += ResetButton_Click;
					properties.RowStyles.Add(new RowStyle());
					properties.Controls.Add(ResetButton, 0, 1);
				}
				properties.RowCount = properties.RowStyles.Count;
				AddPage("PropertiesTabPage", "Properties", "The device's settings, as a property grid.", properties);
			}
			AddRow(PluginTabControl, 100F);
			Controls.Add(MainTableLayoutPanel);
			ResumeLayout(false);
			PerformLayout();
			UiHelp.Attach(this);
		}

		/// <summary>The device the tab belongs to, as the device list knows it.</summary>
		public Guid InstanceGuid { get; }

		public PluginDeviceType Type { get; }

		/// <summary>The plugin instance the engine reads for the device, or null while it reads none.</summary>
		public IPluginDevice Device { get; }

		/// <summary>The device's settings, as the grid shows them; null for a device without settings.</summary>
		public PluginSettingsView View { get; }

		readonly PluginSettings _store;
		readonly TableLayoutPanel MainTableLayoutPanel;
		readonly TabControl PluginTabControl;
		readonly AiAccessUserControl McpServerUserControl;
		readonly PropertyGrid SettingsPropertyGrid;
		readonly Button ResetButton;
		readonly Label NoteLabel;

		/// <summary>What the status rows said, and the values kept for the other rows, when the grid last read them.</summary>
		string[] _statuses;
		string[] _kept;

		/// <summary>Adds a row, sized to its control or, with a percentage, sharing what is left with the other such rows.</summary>
		void AddRow(Control control, float percent = 0)
		{
			MainTableLayoutPanel.RowStyles.Add(percent > 0 ? new RowStyle(SizeType.Percent, percent) : new RowStyle());
			MainTableLayoutPanel.RowCount = MainTableLayoutPanel.RowStyles.Count;
			if (control != null)
				MainTableLayoutPanel.Controls.Add(control, 0, MainTableLayoutPanel.RowCount - 1);
		}

		/// <summary>Adds a page holding one control, which fills it.</summary>
		void AddPage(string name, string text, string description, Control content)
		{
			var page = new TabPage { Name = name, Text = text, AccessibleName = text, AccessibleDescription = description, UseVisualStyleBackColor = true };
			page.Controls.Add(content);
			PluginTabControl.TabPages.Add(page);
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			KeepMinimumHeight();
			base.OnLayout(e);
		}

		/// <summary>
		/// Keeps the pages at least a few lines tall: a row of the table cannot have a minimum, so the tab scrolls once it
		/// is shorter than the help and those few lines.
		/// </summary>
		void KeepMinimumHeight()
		{
			var table = MainTableLayoutPanel;
			if (table == null)
				return;
			var width = ClientSize.Width - table.Padding.Horizontal;
			var height = table.Padding.Vertical;
			foreach (Control control in table.Controls)
			{
				var style = table.RowStyles[table.GetRow(control)];
				height += style.SizeType == SizeType.Percent
					? Font.Height * 16
					: control.GetPreferredSize(new System.Drawing.Size(width - control.Margin.Horizontal, 0)).Height + control.Margin.Vertical;
			}
			if (AutoScrollMinSize.Height != height)
				AutoScrollMinSize = new System.Drawing.Size(0, height);
		}

		private void ResetButton_Click(object sender, EventArgs e)
		{
			// The settings alone: the MCP server's are the person's door, kept until they change them.
			foreach (var setting in Type.Settings)
				_store.Remove(InstanceGuid, setting.Name);
			if (SettingsPropertyGrid != null)
				SettingsPropertyGrid.Refresh();
		}

		/// <summary>Shows what a status the device reports, or a value kept for it, says now, when it has changed since the grid read it.</summary>
		/// <remarks>
		/// A status changes while the device works, and is only drawn again, which reads it afresh and leaves a row being
		/// typed in as it is. A kept value changes outside the grid when a program sets it over MCP; the grid then reads
		/// every row again, so the value and its bold are both current.
		/// </remarks>
		public void RefreshStatus()
		{
			if (View == null)
				return;
			var kept = View.Kept();
			if (!kept.SequenceEqual(_kept))
			{
				_kept = kept;
				_statuses = View.Statuses();
				SettingsPropertyGrid.Refresh();
				return;
			}
			var statuses = View.Statuses();
			if (statuses.SequenceEqual(_statuses))
				return;
			_statuses = statuses;
			SettingsPropertyGrid.Invalidate(true);
		}

		/// <summary>The plugin's own control for the device, or null when it declares none or there is no device yet.</summary>
		/// <remarks>A control that cannot be made says why on its page; the device goes on working without it.</remarks>
		Control NewPluginControl()
		{
			if (Type.ControlType == null)
				return null;
			if (Device == null)
			{
				ShowNote("The plugin's own controls appear here while x360ce reads the device: map it to this controller.");
				return null;
			}
			try
			{
				return (Control)Activator.CreateInstance(Type.ControlType, Device);
			}
			catch (Exception ex)
			{
				var inner = ex is System.Reflection.TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
				ShowNote("The plugin's own controls could not be made: " + inner.GetType().Name + ": " + inner.Message);
				return null;
			}
		}

		void ShowNote(string text)
		{
			NoteLabel.Text = text;
			NoteLabel.AccessibleName = text;
			NoteLabel.Visible = true;
		}
	}
}
