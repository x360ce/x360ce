using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The small "fx" switch that turns a mapping box into an expression, in the manner of a
	/// spreadsheet formula bar.
	/// </summary>
	/// <remarks>
	/// It is nearly invisible until it is wanted: faint while off, plain while hovered, and fully drawn
	/// with a border while on. Somebody who never writes an expression should barely notice a row of
	/// them, and somebody looking for one should find it immediately.
	/// </remarks>
	public class MapExpressionToggle : CheckBox
	{

		/// <summary>Width taken from the box it sits beside, so the row still ends where it did.</summary>
		public const int ToggleWidth = 20;

		/// <summary>The two letters on the face, which are the same on every one of these.</summary>
		/// <remarks>
		/// Kept apart from <see cref="Control.Text"/>, which has to say which row this switch belongs
		/// to so that it can be told from the other twenty nine.
		/// </remarks>
		public const string Label = "fx";

		public MapExpressionToggle()
		{
			Appearance = Appearance.Button;
			FlatStyle = FlatStyle.Flat;
			// Windows reports a button of this kind by its text, and by nothing else: the accessible
			// name and the control name are both ignored for it. So the text carries who this switch
			// belongs to, and the two letters on the face are drawn separately.
			Text = "Formula";
			Font = new Font("Segoe UI", 7f, FontStyle.Regular);
			TextAlign = ContentAlignment.MiddleCenter;
			Margin = Padding.Empty;
			Padding = Padding.Empty;
			TabStop = false;
			FlatAppearance.BorderSize = 0;
			FlatAppearance.CheckedBackColor = Color.Transparent;
			FlatAppearance.MouseOverBackColor = Color.Transparent;
			FlatAppearance.MouseDownBackColor = Color.Transparent;
			AccessibleRole = AccessibleRole.CheckButton;
		}

		/// <summary>
		/// Describes this switch to a screen reader, and to anything driving the window by name.
		/// </summary>
		/// <remarks>
		/// Given outright rather than left to the default, which reports a button that draws its own
		/// label as an unnamed blank area with no role. What a person hears, and what a test asks for,
		/// then does not exist.
		/// </remarks>
		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new ToggleAccessibleObject(this);
		}

		private sealed class ToggleAccessibleObject : Control.ControlAccessibleObject
		{
			public ToggleAccessibleObject(MapExpressionToggle owner) : base(owner) { }

			private MapExpressionToggle Toggle { get { return (MapExpressionToggle)Owner; } }

			public override string Name
			{
				get { return Owner.AccessibleName ?? "Formula"; }
				set { Owner.AccessibleName = value; }
			}

			public override string Description { get { return Owner.AccessibleDescription; } }

			public override AccessibleRole Role { get { return AccessibleRole.CheckButton; } }

			public override AccessibleStates State
			{
				get
				{
					var state = AccessibleStates.Focusable;
					if (Toggle.Checked)
						state |= AccessibleStates.Checked;
					if (!Toggle.Enabled)
						state |= AccessibleStates.Unavailable;
					return state;
				}
			}

			/// <summary>What pressing it does now, which depends on which way it is set.</summary>
			public override string DefaultAction
			{
				get { return Toggle.Checked ? "Use a single control" : "Write a formula"; }
			}

			public override void DoDefaultAction()
			{
				Toggle.Checked = !Toggle.Checked;
			}
		}

		/// <summary>
		/// Names this switch after the row it belongs to, for anything driving the window without a
		/// mouse.
		/// </summary>
		/// <param name="rowName">The row, as a person reads it, such as "Right Trigger".</param>
		/// <remarks>
		/// Three separate names are needed and they are not interchangeable.
		///
		/// The control's own <see cref="Control.Name"/> is what Windows reports as the automation
		/// identifier. Left unset it reports the window handle instead, which is a different number
		/// every run, so nothing can ever ask for the same switch twice.
		///
		/// The accessible name is what a screen reader says and what a test asks for by name. One name
		/// shared by thirty switches identifies none of them, so it carries the row.
		///
		/// The description says what pressing it does, which the name deliberately does not.
		/// </remarks>
		public void NameAfterRow(string rowName)
		{
			var plain = string.IsNullOrEmpty(rowName) ? "Mapping" : rowName;
			Name = plain.Replace(" ", "") + "ExpressionToggle";
			// The text is what Windows actually reports, so it is the one that has to be distinct.
			Text = plain + " formula";
			AccessibleName = Text;
			AccessibleDescription = "Writes the " + plain + " mapping as a formula instead of choosing one control.";
		}

		private bool _hovered;

		protected override void OnMouseEnter(EventArgs e)
		{
			_hovered = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_hovered = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnCheckedChanged(EventArgs e)
		{
			// A switch that is on has to be unmistakable, because everything the box accepts changes.
			FlatAppearance.BorderSize = Checked ? 1 : 0;
			Invalidate();
			base.OnCheckedChanged(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			// Drawn entirely here, without the button underneath. The button pads its own label, which
			// at this width clips a two letter word down to a stray mark, and letting it draw as well
			// would put that mark behind this one.
			var background = Parent == null ? SystemColors.Control : Parent.BackColor;
			using (var brush = new SolidBrush(background))
				e.Graphics.FillRectangle(brush, ClientRectangle);
			// Faint until it is wanted. Blending towards the background rather than using a fixed grey
			// keeps it faint under any theme, including a dark one.
			var colour = Checked
				? SystemColors.WindowText
				: Blend(background, SystemColors.WindowText, _hovered ? 0.55 : 0.22);
			if (Checked)
			{
				// A switch that is on has to be unmistakable, because everything the box accepts changes.
				using (var pen = new Pen(colour))
					e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
			}
			TextRenderer.DrawText(e.Graphics, Label, Font, ClientRectangle, colour,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}

		private static Color Blend(Color from, Color to, double amount)
		{
			return Color.FromArgb(
				(int)(from.R + (to.R - from.R) * amount),
				(int)(from.G + (to.G - from.G) * amount),
				(int)(from.B + (to.B - from.B) * amount));
		}

		#region Attaching to a mapping box

		private static readonly Dictionary<ComboBox, MapExpressionToggle> Attached =
			new Dictionary<ComboBox, MapExpressionToggle>();

		/// <summary>
		/// The row a mapping box belongs to, in the words a person reads on screen.
		/// </summary>
		/// <remarks>
		/// Taken from the box's own accessible name where it has one, because that is already the row
		/// as a person reads it. Otherwise from the control name, which is written as one word.
		/// </remarks>
		public static string RowNameFor(ComboBox box)
		{
			if (box == null)
				return null;
			if (!string.IsNullOrEmpty(box.AccessibleName))
				return box.AccessibleName;
			var name = box.Name ?? "";
			if (name.EndsWith("ComboBox", StringComparison.Ordinal))
				name = name.Substring(0, name.Length - "ComboBox".Length);
			// "RightTrigger" reads as "Right Trigger".
			var spaced = new System.Text.StringBuilder(name.Length + 8);
			for (int i = 0; i < name.Length; i++)
			{
				if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
					spaced.Append(' ');
				spaced.Append(name[i]);
			}
			return spaced.ToString();
		}

		/// <summary>Switch belonging to a mapping box, or null when it has none.</summary>
		public static MapExpressionToggle For(ComboBox box)
		{
			MapExpressionToggle toggle;
			return box != null && Attached.TryGetValue(box, out toggle) ? toggle : null;
		}

		/// <summary>True when this box is currently holding an expression rather than one control.</summary>
		public static bool IsWritingExpression(ComboBox box)
		{
			var toggle = For(box);
			return toggle != null && toggle.Checked;
		}

		/// <summary>
		/// Puts a switch immediately before a mapping box, taking its width from the box so that the
		/// row still ends exactly where it did.
		/// </summary>
		/// <remarks>
		/// Narrowing the box rather than moving it is what keeps this safe on a dense pane: nothing to
		/// the left or right of the row moves, and the two mirrored columns need no separate treatment.
		/// </remarks>
		public static MapExpressionToggle AttachTo(ComboBox box)
		{
			if (box == null || box.Parent == null || Attached.ContainsKey(box))
				return For(box);
			var toggle = new MapExpressionToggle
			{
				Location = new Point(box.Left, box.Top),
				Size = new Size(ToggleWidth, box.Height),
				Anchor = box.Anchor,
			};
			// Named after the box it belongs to, which already carries the row. "RightTriggerComboBox"
			// becomes "Right Trigger", so the switch answers to the same words a person reads on screen.
			toggle.NameAfterRow(RowNameFor(box));
			box.Left += ToggleWidth;
			box.Width -= ToggleWidth;
			box.Parent.Controls.Add(toggle);
			toggle.BringToFront();
			Attached.Add(box, toggle);
			toggle.CheckedChanged += (sender, e) => Switch(box, toggle.Checked);
			// A row that chose from the list already tells the program the moment the choice is made.
			// A row being typed into has nothing that says when the typing is finished, so leaving the
			// box is taken as finished. Without this the formula stays on screen, looking saved, and
			// is thrown away because nothing ever asked the box what it now held.
			box.Leave += (sender, e) => Commit(box);
			// Hovering shows the formula this row's settings already amount to, before anything is
			// switched. That is where somebody first learns the feature exists, and what their own dead
			// zone and sensitivity actually do, without having to commit to anything.
			toggle.MouseEnter += (sender, e) => ShowWhatTheSettingsAmountTo(toggle, box);
			return toggle;
		}

		/// <summary>
		/// Puts a box into expression mode showing a stored expression, when the value is one.
		/// </summary>
		/// <returns>True when the value was an expression and the box now shows it.</returns>
		/// <remarks>
		/// Loading has to restore the switch as well as the text. A row holding a function whose switch
		/// was off would show an empty dropdown, tell the person nothing is mapped, and replace their
		/// function with nothing the next time anything was saved.
		/// </remarks>
		public static bool ShowExpression(ComboBox box, string value)
		{
			if (!MapExpression.IsExpression(value))
			{
				// Coming back to a plain mapping, the switch has to come back off, or the row would
				// stay editable while holding a value chosen from a list.
				var current = For(box);
				if (current != null && current.Checked)
					current.Checked = false;
				return false;
			}
			// The text is put in first and the switch is set afterwards. Setting the switch first runs
			// the change-over against whatever the box still held, which seeds a formula from the old
			// mapping only to overwrite it a line later.
			box.DropDownStyle = ComboBoxStyle.DropDown;
			box.Text = value;
			// A switch that is not there yet does not stop the formula being shown. Saying otherwise
			// sent the value back down the path that reads it as the name of a control, which turns a
			// formula into nothing and loses what the person wrote.
			var toggle = For(box);
			if (toggle != null)
				toggle.Checked = true;
			return true;
		}

		private static readonly ToolTip Hint = new ToolTip { AutoPopDelay = 20000, InitialDelay = 400 };

		/// <summary>
		/// Puts this row's dead zone, sensitivity and anti dead zone on the switch as the formula that
		/// produces them.
		/// </summary>
		private static void ShowWhatTheSettingsAmountTo(MapExpressionToggle toggle, ComboBox box)
		{
			var seed = SeedFor(box);
			Hint.SetToolTip(toggle, string.IsNullOrEmpty(seed)
				? "Write this mapping as a formula."
				: "Write this mapping as a formula. These settings are the same as:" +
					Environment.NewLine + seed +
					Environment.NewLine + Environment.NewLine +
					"Switching over replaces the dead zone, anti dead zone and sensitivity on this row " +
					"with the formula, which starts out doing exactly what they do now.");
		}

		/// <summary>
		/// This row's current settings written as an expression, or null when there is nothing to write.
		/// </summary>
		/// <remarks>
		/// Only a stick or a trigger is seeded. The shaping settings count in the destination's own
		/// units, and a button has no travel to shape, so seeding one would invent a number rather than
		/// preserve a setting.
		/// </remarks>
		private static string SeedFor(ComboBox box)
		{
			var map = SettingsManager.Current.SettingsMap.FirstOrDefault(x => x.Control == box);
			if (map == null || string.IsNullOrEmpty(map.IniKey))
				return null;
			// Written the way a formula names the control, not the way it is stored. The two disagree
			// about buttons: storage keeps button one as "1", which inside a formula is the number one.
			var source = MapExpressionSeed.AsExpressionSource(SettingsConverter.ToIniValue(box.Text));
			if (string.IsNullOrEmpty(source))
				return null;
			var destinationMax = DestinationMax(map.Code);
			if (destinationMax <= 0f)
				return MapExpression.Prefix + source;
			return MapExpressionSeed.FromSettings(source,
				NumberFor(map, "DeadZone"), NumberFor(map, "AntiDeadZone"), NumberFor(map, "Linear"),
				destinationMax);
		}

		/// <summary>Range the row drives, or nought when it is not one that gets shaped.</summary>
		private static float DestinationMax(MapCode code)
		{
			switch (code)
			{
				case MapCode.LeftTrigger:
				case MapCode.RightTrigger:
					return MapExpressionSeed.TriggerMax;
				case MapCode.LeftThumbAxisX:
				case MapCode.LeftThumbAxisY:
				case MapCode.RightThumbAxisX:
				case MapCode.RightThumbAxisY:
					return MapExpressionSeed.ThumbMax;
				default:
					return 0f;
			}
		}

		/// <summary>
		/// Value of one of this row's companion settings, read from the control that shows it.
		/// </summary>
		/// <remarks>
		/// Read from the interface rather than from storage so the formula matches what the person is
		/// looking at, including a change they have made but not yet saved.
		/// </remarks>
		private static float NumberFor(SettingsMapItem map, string suffix)
		{
			var path = map.IniPath + suffix;
			var companion = SettingsManager.Current.SettingsMap
				.FirstOrDefault(x => string.Equals(x.IniPath, path, StringComparison.OrdinalIgnoreCase));
			if (companion == null)
				return 0f;
			var trackBar = companion.Control as TrackBar;
			if (trackBar != null)
				return trackBar.Value;
			var number = companion.Control as NumericUpDown;
			if (number != null)
				return (float)number.Value;
			float parsed;
			return companion.Control != null
				&& float.TryParse(companion.Control.Text, System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out parsed)
				? parsed : 0f;
		}

		/// <summary>Tells the program a typed formula is finished, so that it reaches storage.</summary>
		/// <remarks>
		/// Deliberately narrow. Only a box holding a formula does this, so no other row changes when
		/// it gains or loses focus, and nothing that already worked starts behaving differently.
		/// </remarks>
		private static void Commit(ComboBox box)
		{
			if (box == null || !MapExpression.IsExpression(box.Text))
				return;
			SettingsManager.Current.RaiseSettingsChanged(box);
		}

		/// <summary>Turns writing an expression on or off for one mapping box.</summary>
		private static void Switch(ComboBox box, bool on)
		{
			if (on)
			{
				// Editable, because an expression is typed. The list still works, and now inserts a
				// control name where the cursor is rather than replacing everything.
				box.DropDownStyle = ComboBoxStyle.DropDown;
				if (!MapExpression.IsExpression(box.Text))
					// Seeded from what the row already does, so switching over changes nothing until
					// the person edits it, and they are shown their own tuning in the new syntax.
					box.Text = SeedFor(box) ?? MapExpression.Prefix;
				box.Focus();
				box.SelectionStart = box.Text.Length;
				box.SelectionLength = 0;
			}
			else
			{
				// Leaving expression mode discards the expression: a half-written one is not a mapping,
				// and keeping it while the box says otherwise would be worse than losing it.
				if (MapExpression.IsExpression(box.Text))
					box.Text = "";
				box.DropDownStyle = ComboBoxStyle.DropDownList;
			}
		}

		/// <summary>
		/// Writes a control name into a box that is holding an expression, at the cursor.
		/// </summary>
		/// <returns>True when the box was holding an expression and the name was inserted.</returns>
		/// <remarks>
		/// This is what lets the existing list and the recorder keep working while an expression is
		/// being written: both already produce a control name, and both call the same place to deliver
		/// it. Inserting rather than replacing is the only difference.
		/// </remarks>
		public static bool InsertControlName(ComboBox box, string displayText)
		{
			if (!IsWritingExpression(box) || string.IsNullOrEmpty(displayText))
				return false;
			// The list shows "Axis 1"; an expression is written in the stored form, "a1".
			var name = SettingsConverter.ToIniValue(displayText);
			if (string.IsNullOrEmpty(name))
				return false;
			var text = box.Text ?? "";
			var at = box.SelectionStart;
			if (at < 0 || at > text.Length)
				at = text.Length;
			var remove = Math.Min(box.SelectionLength, text.Length - at);
			box.Text = text.Substring(0, at) + name + text.Substring(at + remove);
			// The cursor lands after what was inserted, and the box keeps focus, so a name can be
			// followed straight away by the arithmetic it belongs to.
			box.SelectionStart = at + name.Length;
			box.SelectionLength = 0;
			box.Focus();
			return true;
		}

		#endregion

	}
}
