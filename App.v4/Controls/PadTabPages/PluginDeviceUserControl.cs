using JocysCom.ClassLibrary.Mcp;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Plugins;
using x360ce.App.UiTree;
using x360ce.Plugins;

namespace x360ce.App.Controls
{
	/// <summary>
	/// A plugin device's tab on the controller page: the control its plugin declares, its MCP server, its settings and its
	/// help, each on a page of its own, in that order.
	/// </summary>
	/// <remarks>
	/// The pages are those the device has. The plugin's own control comes first, made with the instance the engine reads,
	/// or, while it reads none, as when the device is unticked, with an instance of the tab's own: given the device's kept
	/// values, never read, and disposed with the tab, so the control stays in view and can still set the device up. MCP
	/// Server follows for a kind that declares one, with the AI page's own rows; then Properties, the settings in a
	/// property grid, as Visual Studio's Properties window shows a component's: a row for each, grouped by [Category], with
	/// the selected row's help under the grid; and last Help, its class's [Description], shown as the program's help is.
	///
	/// A change is kept for the device at once, and the engine gives it to the plugin before its next read: one made in the
	/// grid, and one the plugin makes itself, from its own control, and announces with PropertyChanged. A status is read from
	/// the instance the tab shows, and the grid is drawn again whenever <see cref="RefreshStatus"/> finds a value changed.
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
			// 8 from the page's edges with the page's own padding; the tab is made once the Controller page has been scaled, so
			// its room is given at the screen's scale.
			Padding = AppHelper.ScaledPadding(8, 8, 0, 0);
			SuspendLayout();
			PluginTabControl = new TabControl { Name = nameof(PluginTabControl), Dock = DockStyle.Fill };
			Shown = device;
			if (type.ControlType != null)
			{
				NoteLabel = new Label { Name = nameof(NoteLabel), AutoSize = true, Dock = DockStyle.Top, Padding = AppHelper.ScaledPadding(12, 12, 12, 12), Visible = false };
				if (Shown == null)
					Shown = NewTabInstance();
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
					Name = nameof(McpServerUserControl),
					Dock = DockStyle.Fill,
					AccessibleName = "MCP server",
					AccessibleDescription = "The device’s own MCP server, which a program or an AI assistant reaches this device alone through.",
				};
				McpServerUserControl.Bind(door);
				AddPage("McpServerTabPage", "MCP Server", "The device’s own MCP server: whether it is open, its level, address, port and token.", McpServerUserControl);
			}
			if (type.Settings.Length > 0)
			{
				View = new PluginSettingsView(instanceGuid, type, Shown, store);
				SettingsPropertyGrid = new PropertyGrid
				{
					Name = nameof(SettingsPropertyGrid),
					Dock = DockStyle.Fill,
					PropertySort = PropertySort.Categorized,
					AccessibleName = "Settings",
					AccessibleDescription = "The device’s settings, grouped. Each row’s help shows under the grid; a value kept for the device is bold.",
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
						Name = nameof(ResetButton),
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
				AddPage("PropertiesTabPage", "Properties", "The device’s settings, as a property grid.", properties);
			}
			if (!string.IsNullOrWhiteSpace(type.Description))
			{
				// As the Help tab shows the program's help.
				HelpRichTextBox = new RichTextBox
				{
					Name = nameof(HelpRichTextBox),
					Dock = DockStyle.Fill,
					BackColor = System.Drawing.SystemColors.Window,
					BorderStyle = BorderStyle.None,
					ReadOnly = true,
					AccessibleName = "Help",
					AccessibleDescription = "The device’s help, from its plugin: what it is, its controls, and how to use it.",
				};
				AppHelper.ShowHelp(HelpRichTextBox, type.Description.Trim());
				AddPage("HelpTabPage", "Help", "The device’s help, from its plugin.", HelpRichTextBox);
			}
			Controls.Add(PluginTabControl);
			_notifying = Shown as INotifyPropertyChanged;
			if (_notifying != null)
				_notifying.PropertyChanged += Shown_PropertyChanged;
			_identity = Shown as IPluginDeviceIdentity;
			if (_identity != null)
				_identity.DisconnectRequested += Shown_DisconnectRequested;
			ResumeLayout(false);
			PerformLayout();
			UiHelp.Attach(this);
		}

		/// <summary>The device the tab belongs to, as the device list knows it.</summary>
		public Guid InstanceGuid { get; }

		public PluginDeviceType Type { get; }

		/// <summary>The plugin instance the engine reads for the device, or null while it reads none.</summary>
		public IPluginDevice Device { get; }

		/// <summary>The plugin instance the tab shows: the one the engine reads, or the tab's own while it reads none; null for a device without a control of its own that the engine does not read.</summary>
		public IPluginDevice Shown { get; }

		/// <summary>The device's settings, as the grid shows them; null for a device without settings.</summary>
		public PluginSettingsView View { get; }

		readonly PluginSettings _store;
		readonly TabControl PluginTabControl;
		readonly AiAccessUserControl McpServerUserControl;
		readonly PropertyGrid SettingsPropertyGrid;
		readonly Button ResetButton;
		readonly RichTextBox HelpRichTextBox;
		readonly Label NoteLabel;

		/// <summary>The instance the tab made for itself and gives the kept values to; null while it shows the one the engine reads.</summary>
		PluginDeviceReader _tabReader;

		/// <summary>The instance shown, while it announces the settings it changes itself.</summary>
		readonly INotifyPropertyChanged _notifying;

		/// <summary>The instance shown, when it reports real external hardware identity and can request a safety disconnect.</summary>
		readonly IPluginDeviceIdentity _identity;

		/// <summary>The currently selected sub-tab index (Connection, MCP Server, Properties, Help).</summary>
		internal int SelectedSubTabIndex { get { return PluginTabControl == null ? 0 : PluginTabControl.SelectedIndex; } }

		/// <summary>Restores the selected sub-tab index after an in-place panel refresh.</summary>
		internal void SelectSubTab(int index)
		{
			if (PluginTabControl != null && index >= 0 && index < PluginTabControl.TabPages.Count)
				PluginTabControl.SelectedIndex = index;
		}

		/// <summary>What the status rows said, and the values kept for the other rows, when the grid last read them.</summary>
		string[] _statuses;
		string[] _kept;

		/// <summary>Adds a page holding one control, which fills it.</summary>
		void AddPage(string name, string text, string description, Control content)
		{
			var page = new TabPage { Name = name, Text = text, AccessibleName = text, AccessibleDescription = description, UseVisualStyleBackColor = true };
			page.Controls.Add(content);
			PluginTabControl.TabPages.Add(page);
		}

		private void ResetButton_Click(object sender, EventArgs e)
		{
			// The settings alone: the MCP server's are the person's door, kept until they change them.
			foreach (var setting in Type.Settings)
				_store.Remove(InstanceGuid, setting.Name);
			if (SettingsPropertyGrid != null)
				SettingsPropertyGrid.Refresh();
		}

		/// <summary>Keeps a setting the plugin changed itself, as from its own control, for the device, as the grid keeps one the person changed.</summary>
		/// <remarks>Raised by the plugin on the interface thread. A status, or a property that is no setting, is left alone.</remarks>
		void Shown_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var setting = Type.Settings.FirstOrDefault(x => x.Name == e.PropertyName && x.Kind != PluginSettingKind.Status);
			if (setting == null)
				return;
			_store.Set(InstanceGuid, setting.Name, setting.ToText(setting.Property.GetValue(sender)));
		}

		void Shown_DisconnectRequested(object sender, EventArgs e)
		{
			if (IsDisposed)
				return;
			if (InvokeRequired)
			{
				if (IsHandleCreated)
					BeginInvoke((Action)(() => PluginDevices.DisableDevice(InstanceGuid)));
				return;
			}
			PluginDevices.DisableDevice(InstanceGuid);
		}

		/// <summary>Shows what a status the device reports, or a value kept for it, says now, when it has changed since the grid read it.</summary>
		/// <remarks>
		/// A status changes while the device works, and is only drawn again, which reads it afresh and leaves a row being
		/// typed in as it is. A kept value changes outside the grid when a program sets it over MCP; the grid then reads
		/// every row again, so the value and its bold are both current. The tab's own instance is given the kept values first.
		/// </remarks>
		public void RefreshStatus()
		{
			if (_tabReader != null)
				ApplySettings();
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

		/// <summary>An instance of the device for the tab alone, given the values kept for the device; null when it cannot be made, which the page says.</summary>
		IPluginDevice NewTabInstance()
		{
			try
			{
				_tabReader = new PluginDeviceReader(Type, Type.NewInstance());
			}
			catch (Exception ex)
			{
				ShowFault(ex);
				return null;
			}
			return ApplySettings() ? _tabReader.Device : null;
		}

		/// <summary>Gives the tab's own instance the values kept for the device; false, and the page says why, when the plugin refuses one.</summary>
		bool ApplySettings()
		{
			try
			{
				_tabReader.ApplySettings(_store, InstanceGuid);
				return true;
			}
			catch (Exception ex)
			{
				ShowFault(ex);
				return false;
			}
		}

		/// <summary>The plugin's own control for the device, or null when it declares none or it cannot be made.</summary>
		/// <remarks>A control that cannot be made says why on its page; the device goes on working without it.</remarks>
		Control NewPluginControl()
		{
			if (Type.ControlType == null || Shown == null)
				return null;
			try
			{
				return (Control)Activator.CreateInstance(Type.ControlType, Shown);
			}
			catch (Exception ex)
			{
				ShowFault(ex);
				return null;
			}
		}

		void ShowFault(Exception ex)
		{
			var inner = ex is System.Reflection.TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
			var text = "The plugin’s own controls could not be made: " + inner.GetType().Name + ": " + inner.Message;
			NoteLabel.Text = text;
			NoteLabel.AccessibleName = text;
			NoteLabel.Visible = true;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_notifying != null)
					_notifying.PropertyChanged -= Shown_PropertyChanged;
				if (_identity != null)
					_identity.DisconnectRequested -= Shown_DisconnectRequested;
				// The tab's own instance goes with the tab; the engine's is the engine's to let go of.
				if (_tabReader != null)
				{
					_tabReader.Dispose();
					_tabReader = null;
				}
			}
			base.Dispose(disposing);
		}
	}
}
