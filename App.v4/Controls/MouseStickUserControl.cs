using System;
using System.Windows.Forms;
using x360ce.App.DInput;

namespace x360ce.App.Controls
{
	/// <summary>
	/// How a mouse, or any control that reports movement, moves one stick: the style, the sensitivity across and up or down,
	/// the smoothing, the return to the centre, and the button that turns it on and off. One for each stick on a
	/// controller's Mouse page.
	/// </summary>
	/// <remarks>A setting that does nothing in the style or activation chosen is greyed out, not hidden, so the page keeps its shape.</remarks>
	public partial class MouseStickUserControl : UserControl
	{
		public MouseStickUserControl()
		{
			InitializeComponent();
			// Scaled by the font once, after the designer's tables have resumed their layout: scaled in the designer, Windows
			// Forms scales a table as it resumes and then again with the control. The mode first, as changing it clears the
			// dimensions.
			SuspendLayout();
			AutoScaleMode = AutoScaleMode.Font;
			AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			ResumeLayout(false);
			foreach (var box in new[] { StyleComboBox, ActiveComboBox, ButtonComboBox })
			{
				box.DisplayMember = nameof(KeyValuePair.Key);
				box.ValueMember = nameof(KeyValuePair.Value);
			}
			StyleComboBox.Items.Add(new KeyValuePair("Position: follows how far the mouse moved", ((int)MouseStyle.Position).ToString()));
			StyleComboBox.Items.Add(new KeyValuePair("Speed: follows how fast the mouse moves", ((int)MouseStyle.Speed).ToString()));
			StyleComboBox.Items.Add(new KeyValuePair("Screen: follows where the pointer is", ((int)MouseStyle.Screen).ToString()));
			StyleComboBox.Items.Add(new KeyValuePair("Wheel: like Position, then back to centre", ((int)MouseStyle.Wheel).ToString()));
			StyleComboBox.SelectedIndex = 0;
			ActiveComboBox.Items.Add(new KeyValuePair("Always", ((int)MouseActive.Always).ToString()));
			ActiveComboBox.Items.Add(new KeyValuePair("While the button is held, then centre", ((int)MouseActive.HeldCentre).ToString()));
			ActiveComboBox.Items.Add(new KeyValuePair("While the button is held, then stay", ((int)MouseActive.HeldStay).ToString()));
			ActiveComboBox.Items.Add(new KeyValuePair("The button turns it on and off", ((int)MouseActive.Toggle).ToString()));
			ActiveComboBox.SelectedIndex = 0;
			ButtonComboBox.Items.Add(new KeyValuePair("None", "0"));
			for (var i = 1; i <= 32; i++)
				ButtonComboBox.Items.Add(new KeyValuePair("Button " + i + (i == 1 ? " (mouse left)" : i == 2 ? " (mouse right)" : i == 3 ? " (mouse middle)" : ""), i.ToString()));
			ButtonComboBox.SelectedIndex = 0;
			StyleComboBox.SelectedIndexChanged += (s, e) => UpdateEnabled();
			ActiveComboBox.SelectedIndexChanged += (s, e) => UpdateEnabled();
			foreach (var box in new[] { StyleComboBox, ActiveComboBox, ButtonComboBox })
				box.SelectedIndexChanged += (s, e) => SettingsChanged?.Invoke(this, EventArgs.Empty);
			foreach (var slider in new[] { SensitivityXTrackBar, SensitivityYTrackBar, SmoothingTrackBar, ReturnTrackBar })
				slider.ValueChanged += (s, e) => SettingsChanged?.Invoke(this, EventArgs.Empty);
			// Only a choice the person makes, not a setting loaded.
			StyleComboBox.SelectionChangeCommitted += (s, e) => StyleChosen?.Invoke(this, EventArgs.Empty);
			// Each slider's value beside it, as on the stick and force feedback pages.
			ShowValue(SensitivityXTrackBar, SensitivityXTextBox, "%");
			ShowValue(SensitivityYTrackBar, SensitivityYTextBox, "%");
			ShowValue(SmoothingTrackBar, SmoothingTextBox, "ms");
			ShowValue(ReturnTrackBar, ReturnTextBox, "ms");
			LayoutPanel.SizeChanged += (s, e) => FitHeight();
			UpdateEnabled();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			FitHeight();
		}

		/// <summary>Fits the control's height to where its settings end, whatever the font and the display scale.</summary>
		/// <remarks>A group box that sizes itself counts its caption twice and leaves a line's worth of space below.</remarks>
		void FitHeight()
		{
			Height = LayoutPanel.Bottom + StickGroupBox.Height - StickGroupBox.DisplayRectangle.Bottom;
		}

		/// <summary>Raised when a setting of this stick changes, whoever changes it.</summary>
		public event EventHandler SettingsChanged;

		/// <summary>Raised when the person chooses a style in the Style list.</summary>
		public event EventHandler StyleChosen;

		/// <summary>The settings as the controls hold them now, as the engine reads them from a preset.</summary>
		public MouseStickSettings ReadSettings()
		{
			return new MouseStickSettings
			{
				Style = (MouseStyle)Selected(StyleComboBox),
				SensitivityX = SensitivityXTrackBar.Value,
				SensitivityY = SensitivityYTrackBar.Value,
				SmoothingMs = SmoothingTrackBar.Value,
				ReturnMs = ReturnTrackBar.Value,
				Active = (MouseActive)Selected(ActiveComboBox),
				Button = Selected(ButtonComboBox),
			};
		}

		/// <summary>The style chosen, in the words of the Style list: its name, and what it does.</summary>
		public string StyleText
		{
			get { return StyleComboBox.Text; }
		}

		/// <summary>The stick the control sets, shown as the group's title.</summary>
		public string StickName
		{
			get { return StickGroupBox.Text; }
			set { StickGroupBox.Text = value; }
		}

		static int Selected(ComboBox box)
		{
			var item = box.SelectedItem as KeyValuePair;
			int value;
			return item != null && int.TryParse(item.Value, out value) ? value : 0;
		}

		/// <summary>Keeps a slider's value, with its unit, in the box beside it.</summary>
		static void ShowValue(TrackBar slider, TextBox box, string unit)
		{
			slider.ValueChanged += (s, e) => box.Text = slider.Value + " " + unit;
			box.Text = slider.Value + " " + unit;
		}

		/// <summary>Greys out what the style and the activation chosen do not use.</summary>
		void UpdateEnabled()
		{
			var style = (MouseStyle)Selected(StyleComboBox);
			var active = (MouseActive)Selected(ActiveComboBox);
			Enable(style != MouseStyle.Position && style != MouseStyle.Wheel, SmoothingLabel, SmoothingTrackBar, SmoothingTextBox);
			// A wheel goes back to the centre only once the mouse lets it go.
			var returns = style == MouseStyle.Wheel ? active == MouseActive.HeldCentre || active == MouseActive.Toggle : style != MouseStyle.Screen;
			Enable(returns, ReturnLabel, ReturnTrackBar, ReturnTextBox);
			Enable(active != MouseActive.Always, ButtonLabel, ButtonComboBox);
		}

		static void Enable(bool enabled, params Control[] controls)
		{
			foreach (var control in controls)
				control.Enabled = enabled;
		}
	}
}
