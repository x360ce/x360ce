using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Plugins;
using x360ce.App.UiTree;
using x360ce.Plugins;

namespace x360ce.App.Controls
{
	/// <summary>A plugin device's tab on the controller page: its settings, made from its class, and the control its plugin declares.</summary>
	/// <remarks>
	/// Laid out as the Options pages are: a group for each [Category], and in it a row for each setting, its label on the
	/// left and its field on the right, or a check box across both. A change is kept for the device at once, and the
	/// engine gives it to the plugin before its next read. A status is read from the device the engine reads, whenever
	/// <see cref="RefreshStatus"/> is called. The plugin's own control comes after the settings and takes the rest of the
	/// tab; it is made with the device the engine reads, so the tab says so while there is none yet.
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
			SuspendLayout();
			MainTableLayoutPanel = new TableLayoutPanel
			{
				Name = "MainTableLayoutPanel",
				Dock = DockStyle.Fill,
				AutoScroll = true,
				Padding = new Padding(6),
				ColumnCount = 1,
			};
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			foreach (var category in type.Settings.Select(x => x.Category).Distinct())
				AddRow(NewGroup(category, type.Settings.Where(x => x.Category == category).ToArray()));
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
				AddRow(ResetButton);
			}
			NoteLabel = new Label { Name = "NoteLabel", AutoSize = true, Anchor = AnchorStyles.Left, Visible = false };
			AddRow(NoteLabel);
			// The last row takes what is left: the plugin's control, or nothing, which keeps the groups at the top.
			var control = NewPluginControl();
			MainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			MainTableLayoutPanel.RowCount = MainTableLayoutPanel.RowStyles.Count;
			if (control != null)
			{
				control.Dock = DockStyle.Fill;
				MainTableLayoutPanel.Controls.Add(control, 0, MainTableLayoutPanel.RowCount - 1);
			}
			Controls.Add(MainTableLayoutPanel);
			LoadValues();
			RefreshStatus();
			SizeFields();
			ResumeLayout(false);
			PerformLayout();
			UiHelp.Attach(this);
		}

		/// <summary>The device the tab belongs to, as the device list knows it.</summary>
		public Guid InstanceGuid { get; }

		public PluginDeviceType Type { get; }

		/// <summary>The plugin instance the engine reads for the device, or null while it reads none.</summary>
		public IPluginDevice Device { get; }

		readonly PluginSettings _store;
		readonly TableLayoutPanel MainTableLayoutPanel;
		readonly Button ResetButton;
		readonly Label NoteLabel;

		/// <summary>Each setting's field: a check box, a number box, a text box, a drop-down list, or a status label.</summary>
		readonly Dictionary<PluginSetting, Control> _fields = new Dictionary<PluginSetting, Control>();

		/// <summary>True while fields are filled from the kept values, so filling them keeps nothing.</summary>
		bool _loading;

		/// <summary>True once a status getter threw: the plugin is switched off and no status is read again.</summary>
		bool _statusFailed;

		void AddRow(Control control)
		{
			MainTableLayoutPanel.RowStyles.Add(new RowStyle());
			MainTableLayoutPanel.RowCount = MainTableLayoutPanel.RowStyles.Count;
			MainTableLayoutPanel.Controls.Add(control, 0, MainTableLayoutPanel.RowCount - 1);
		}

		#region Fields

		GroupBox NewGroup(string category, PluginSetting[] settings)
		{
			var key = new string(category.Where(char.IsLetterOrDigit).ToArray());
			var group = new GroupBox
			{
				Name = key + "GroupBox",
				Text = category,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
			};
			var table = new TableLayoutPanel
			{
				Name = key + "TableLayoutPanel",
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 2,
			};
			table.ColumnStyles.Add(new ColumnStyle());
			table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			foreach (var setting in settings)
			{
				table.RowStyles.Add(new RowStyle());
				var row = table.RowStyles.Count - 1;
				var field = NewField(setting);
				_fields.Add(setting, field);
				if (setting.Kind == PluginSettingKind.CheckBox)
				{
					table.Controls.Add(field, 0, row);
					table.SetColumnSpan(field, 2);
					continue;
				}
				// A caption: the field beside it carries the name and the help.
				var label = new Label
				{
					Name = setting.Name + "Label",
					Text = setting.DisplayName,
					AutoSize = true,
					Anchor = AnchorStyles.Left,
				};
				table.Controls.Add(label, 0, row);
				table.Controls.Add(field, 1, row);
			}
			table.RowCount = table.RowStyles.Count;
			group.Controls.Add(table);
			return group;
		}

		Control NewField(PluginSetting setting)
		{
			Control field;
			switch (setting.Kind)
			{
				case PluginSettingKind.CheckBox:
					var check = new CheckBox { Name = setting.Name + "CheckBox", Text = setting.DisplayName, AutoSize = true, Anchor = AnchorStyles.Left };
					check.CheckedChanged += (s, e) => Keep(setting, setting.ToText(check.Checked));
					field = check;
					break;
				case PluginSettingKind.Number:
					var number = new NumericUpDown
					{
						Name = setting.Name + "NumericUpDown",
						Minimum = setting.Minimum,
						Maximum = setting.Maximum,
						DecimalPlaces = setting.DecimalPlaces,
						Increment = setting.DecimalPlaces > 0 ? 0.1m : 1m,
						TextAlign = HorizontalAlignment.Right,
						Anchor = AnchorStyles.Left,
					};
					number.ValueChanged += (s, e) => Keep(setting, setting.ToText(Convert.ChangeType(number.Value, setting.Property.PropertyType, System.Globalization.CultureInfo.InvariantCulture)));
					field = number;
					break;
				case PluginSettingKind.List:
					// Sized by itself: the table makes a row as tall as the size a list reports, and a list given a size keeps
					// reporting the height of the font it was made with, while Windows draws it taller, over the row below.
					var list = new ComboBox
					{
						Name = setting.Name + "ComboBox",
						AutoSize = true,
						DropDownStyle = setting.ListOnly ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown,
						Anchor = setting.ListOnly ? AnchorStyles.Left : AnchorStyles.Left | AnchorStyles.Right,
					};
					list.Items.AddRange(setting.GetChoices());
					if (setting.ListOnly)
						list.SelectedIndexChanged += (s, e) =>
						{
							var choice = list.SelectedItem as PluginSettingChoice;
							if (choice != null)
								Keep(setting, choice.Value);
						};
					else
						list.TextChanged += (s, e) => KeepTyped(setting, ValueOf(list));
					field = list;
					break;
				case PluginSettingKind.Text:
					var text = new TextBox { Name = setting.Name + "TextBox", Anchor = AnchorStyles.Left | AnchorStyles.Right };
					text.TextChanged += (s, e) => KeepTyped(setting, text.Text);
					field = text;
					break;
				default:
					// Across the column, so a value of another width moves nothing and the group is not drawn again each second.
					field = new Label { Name = setting.Name + "ValueLabel", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
					break;
			}
			field.AccessibleName = setting.DisplayName;
			field.AccessibleDescription = setting.Description;
			return field;
		}

		/// <summary>The value an editable list holds: the chosen entry's, or the text typed.</summary>
		static string ValueOf(ComboBox list)
		{
			var choice = list.Items.OfType<PluginSettingChoice>().FirstOrDefault(x => x.Text == list.Text);
			return choice == null ? list.Text : choice.Value;
		}

		/// <summary>Number boxes and fixed lists as wide as their widest value, in the tab's font.</summary>
		void SizeFields()
		{
			foreach (var pair in _fields)
			{
				var number = pair.Value as NumericUpDown;
				if (number != null)
				{
					var widest = new[] { number.Minimum, number.Maximum }
						.Max(x => TextRenderer.MeasureText(x.ToString("N" + number.DecimalPlaces), number.Font).Width);
					number.Width = widest + SystemInformation.VerticalScrollBarWidth + number.Margin.Horizontal;
					continue;
				}
				var list = pair.Value as ComboBox;
				if (list != null && list.DropDownStyle == ComboBoxStyle.DropDownList)
				{
					var widest = list.Items.Cast<object>().Select(x => TextRenderer.MeasureText(list.GetItemText(x), list.Font).Width)
						.DefaultIfEmpty(0).Max();
					// The list sizes itself, so its width is a minimum.
					list.MinimumSize = new Size(widest + SystemInformation.VerticalScrollBarWidth + list.Margin.Horizontal, 0);
				}
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			SizeFields();
		}

		#endregion

		#region Values

		/// <summary>Keeps a value for the device, unless the fields are being filled.</summary>
		void Keep(PluginSetting setting, string text)
		{
			if (!_loading)
				_store.Set(InstanceGuid, setting.Name, text);
		}

		/// <summary>Keeps a typed value once it is one the setting's type can hold, so the plugin is never given one it cannot.</summary>
		void KeepTyped(PluginSetting setting, string text)
		{
			try
			{
				setting.FromText(text);
			}
			catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is NotSupportedException || ex is OverflowException)
			{
				return;
			}
			Keep(setting, text);
		}

		/// <summary>Fills every field with the value kept for the device, or the one a new device starts with.</summary>
		void LoadValues()
		{
			_loading = true;
			try
			{
				foreach (var pair in _fields)
				{
					var setting = pair.Key;
					if (setting.Kind == PluginSettingKind.Status)
						continue;
					var text = _store.Get(InstanceGuid, setting.Name) ?? setting.DefaultText;
					var check = pair.Value as CheckBox;
					if (check != null)
						check.Checked = (bool)setting.FromText(text);
					var number = pair.Value as NumericUpDown;
					if (number != null)
					{
						var value = Convert.ToDecimal(setting.FromText(text), System.Globalization.CultureInfo.InvariantCulture);
						number.Value = Math.Min(number.Maximum, Math.Max(number.Minimum, value));
					}
					var list = pair.Value as ComboBox;
					if (list != null)
					{
						var choice = list.Items.OfType<PluginSettingChoice>().FirstOrDefault(x => x.Value == text);
						if (list.DropDownStyle == ComboBoxStyle.DropDownList)
							list.SelectedItem = choice;
						else
							list.Text = choice == null ? text : choice.Text;
					}
					var box = pair.Value as TextBox;
					if (box != null)
						box.Text = text;
				}
			}
			finally
			{
				_loading = false;
			}
		}

		private void ResetButton_Click(object sender, EventArgs e)
		{
			_store.Reset(InstanceGuid);
			LoadValues();
		}

		/// <summary>Shows what each status of the device the engine reads says now.</summary>
		/// <remarks>
		/// Read on the interface thread while the engine may be reading the device, which is what the plugin's help asks a
		/// status property to allow. One that throws switches the plugin off, as any of its members does.
		/// </remarks>
		public void RefreshStatus()
		{
			if (Device == null || _statusFailed)
				return;
			foreach (var pair in _fields)
			{
				if (pair.Key.Kind != PluginSettingKind.Status)
					continue;
				string text;
				try
				{
					text = pair.Key.StatusText(Device);
				}
				catch (Exception ex)
				{
					_statusFailed = true;
					var manager = PluginManager.Current;
					if (manager != null)
						manager.Fault(Type, ex);
					return;
				}
				if (pair.Value.Text != text)
					pair.Value.Text = text;
			}
		}

		#endregion

		/// <summary>The plugin's own control for the device, or null when it declares none or there is no device yet.</summary>
		/// <remarks>A control that cannot be made says why on the tab; the device goes on working without it.</remarks>
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
