using JocysCom.ClassLibrary.Controls.UiTree;
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
			foreach (var style in new[] { MouseStyle.Position, MouseStyle.Speed, MouseStyle.Screen, MouseStyle.Wheel })
				StyleComboBox.Items.Add(new KeyValuePair("", ((int)style).ToString()));
			foreach (var active in new[] { MouseActive.Always, MouseActive.HeldCentre, MouseActive.HeldStay, MouseActive.Toggle })
				ActiveComboBox.Items.Add(new KeyValuePair("", ((int)active).ToString()));
			for (var i = 0; i <= 32; i++)
				ButtonComboBox.Items.Add(new KeyValuePair("", i.ToString()));
			StyleComboBox.Format += (s, e) =>
			{
				var style = (MouseStyle)ValueOf(e.ListItem);
				e.Value = string.Format(UiLanguage.T("MouseStickUserControl.StyleChoice", "{0}: {1}"), StyleName(style), StyleExplanation(style));
			};
			ActiveComboBox.Format += (s, e) => e.Value = ActiveText((MouseActive)ValueOf(e.ListItem));
			ButtonComboBox.Format += (s, e) => e.Value = ButtonText(ValueOf(e.ListItem));
			foreach (var box in new[] { StyleComboBox, ActiveComboBox, ButtonComboBox })
			{
				box.FormattingEnabled = true;
				box.SelectedIndex = 0;
			}
			EventHandler languageChanged = (s, e) => UiLanguage.Relist(StyleComboBox, ActiveComboBox, ButtonComboBox);
			UiLanguage.Changed += languageChanged;
			Disposed += (s, e) => UiLanguage.Changed -= languageChanged;
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

		internal static string StyleName(MouseStyle style)
		{
			switch (style)
			{
				case MouseStyle.Speed: return UiLanguage.T("MouseStickUserControl.Speed", "Speed");
				case MouseStyle.Screen: return UiLanguage.T("MouseStickUserControl.Screen", "Screen");
				case MouseStyle.Wheel: return UiLanguage.T("MouseStickUserControl.Wheel", "Wheel");
				default: return UiLanguage.T("MouseStickUserControl.Position", "Position");
			}
		}

		internal static string StyleExplanation(MouseStyle style)
		{
			switch (style)
			{
				case MouseStyle.Speed: return UiLanguage.T("MouseStickUserControl.SpeedExplanation", "follows how fast the mouse moves");
				case MouseStyle.Screen: return UiLanguage.T("MouseStickUserControl.ScreenExplanation", "follows where the pointer is");
				case MouseStyle.Wheel: return UiLanguage.T("MouseStickUserControl.WheelExplanation", "like Position, then back to centre");
				default: return UiLanguage.T("MouseStickUserControl.PositionExplanation", "follows how far the mouse moved");
			}
		}

		static string ActiveText(MouseActive active)
		{
			switch (active)
			{
				case MouseActive.HeldCentre: return UiLanguage.T("MouseStickUserControl.HeldCentre", "While the button is held, then centre");
				case MouseActive.HeldStay: return UiLanguage.T("MouseStickUserControl.HeldStay", "While the button is held, then stay");
				case MouseActive.Toggle: return UiLanguage.T("MouseStickUserControl.Toggle", "The button turns it on and off");
				default: return UiLanguage.T("MouseStickUserControl.Always", "Always");
			}
		}

		static string ButtonText(int button)
		{
			if (button == 0)
				return UiLanguage.T("MouseStickUserControl.NoButton", "None");
			var format = button == 1 ? UiLanguage.T("MouseStickUserControl.LeftMouseButton", "Button {0} (mouse left)")
				: button == 2 ? UiLanguage.T("MouseStickUserControl.RightMouseButton", "Button {0} (mouse right)")
				: button == 3 ? UiLanguage.T("MouseStickUserControl.MiddleMouseButton", "Button {0} (mouse middle)")
				: UiLanguage.T("MouseStickUserControl.ButtonNumber", "Button {0}");
			return string.Format(format, button);
		}

		/// <summary>The stick the control sets, shown as the group's title.</summary>
		public string StickName
		{
			get { return StickGroupBox.Text; }
			set { StickGroupBox.Text = value; }
		}

		static int Selected(ComboBox box)
		{
			return ValueOf(box.SelectedItem);
		}

		static int ValueOf(object valueItem)
		{
			var item = valueItem as KeyValuePair;
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
