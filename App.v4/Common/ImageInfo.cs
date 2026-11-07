using SharpDX.XInput;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public class ImageInfo
	{
		public ImageInfo(int image, MapCode code, double x, double y, Control label, Control control, GamepadButtonFlags button = GamepadButtonFlags.None)
		{
			Image = image;
			Label = label;
			Control = control;
			Button = button;
			Code = code;
			X = x;
			Y = y;
		}

		/// <summary>What this control maps, in words: "D-Pad Up", "Left Thumb Axis X".</summary>
		/// <remarks>
		/// A combo box reports its selected text as its accessible name, so every mapping control
		/// announced what it currently held instead of what it sets. Two of them answered to the
		/// name of a different control and five answered to the same one, which is enough for a
		/// screen reader or an assistant to change the wrong mapping, and the name moved every time
		/// a mapping changed. The map code is the part that never moves, so the name comes from it.
		/// </remarks>
		public string Name => GetName(Code);

		/// <summary>Turn a map code into the words a person would use for it.</summary>
		public static string GetName(MapCode code)
		{
			var text = code.ToString();
			var sb = new StringBuilder(text.Length + 8);
			foreach (var c in text)
			{
				if (char.IsUpper(c) && sb.Length > 0)
					sb.Append(' ');
				sb.Append(c);
			}
			// "D Pad" is one term rather than two words.
			return sb.Replace("D Pad", "D-Pad").ToString();
		}

		/// <summary>The emulated controller's value for this part, the one the controller picture is drawn from.</summary>
		/// <returns>
		/// A trigger 0 to 255; a stick axis -32768 to 32767; a stick direction the axis while it points that way, otherwise 0;
		/// the D-Pad its directions added up, up 1, down 2, left 4 and right 8, as XInput holds them; a button 1 while pressed,
		/// otherwise 0.
		/// </returns>
		public int GetValue(Gamepad gp)
		{
			switch (Code)
			{
				case MapCode.LeftTrigger: return gp.LeftTrigger;
				case MapCode.RightTrigger: return gp.RightTrigger;
				case MapCode.LeftThumbAxisX: return gp.LeftThumbX;
				case MapCode.LeftThumbAxisY: return gp.LeftThumbY;
				case MapCode.RightThumbAxisX: return gp.RightThumbX;
				case MapCode.RightThumbAxisY: return gp.RightThumbY;
				case MapCode.LeftThumbRight: return Math.Max((int)gp.LeftThumbX, 0);
				case MapCode.LeftThumbLeft: return Math.Min((int)gp.LeftThumbX, 0);
				case MapCode.LeftThumbUp: return Math.Max((int)gp.LeftThumbY, 0);
				case MapCode.LeftThumbDown: return Math.Min((int)gp.LeftThumbY, 0);
				case MapCode.RightThumbRight: return Math.Max((int)gp.RightThumbX, 0);
				case MapCode.RightThumbLeft: return Math.Min((int)gp.RightThumbX, 0);
				case MapCode.RightThumbUp: return Math.Max((int)gp.RightThumbY, 0);
				case MapCode.RightThumbDown: return Math.Min((int)gp.RightThumbY, 0);
				case MapCode.DPad: return (int)(gp.Buttons & DPadButtons);
				default: return Button != GamepadButtonFlags.None && (gp.Buttons & Button) != 0 ? 1 : 0;
			}
		}

		const GamepadButtonFlags DPadButtons = GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadDown
			| GamepadButtonFlags.DPadLeft | GamepadButtonFlags.DPadRight;

		/// <summary>Writes this part's value under its name, only when it differs from the value written there last.</summary>
		public void ShowValue(Gamepad gp)
		{
			var value = GetValue(gp);
			if (ValueLabel == null || value == _ShownValue)
				return;
			_ShownValue = value;
			ValueLabel.Text = value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>The value the label shows, which is 0 until the first other value arrives.</summary>
		int _ShownValue;

		public bool? CurrentImageShow { get; set; }
		public string CurrentImageName { get; set; }

		public int Image { get; set; }
		public Control Label { get; set; }

		/// <summary>The small number under <see cref="Label"/>, which shows <see cref="GetValue"/>.</summary>
		public Label ValueLabel { get; set; }

		public Control Control { get; set; }
		public GamepadButtonFlags Button { get; set; }
		public MapCode Code { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
	}
}
