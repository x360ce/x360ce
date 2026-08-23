using SharpDX.XInput;
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

		public bool? CurrentImageShow { get; set; }
		public string CurrentImageName { get; set; }

		public int Image { get; set; }
		public Control Label { get; set; }

		public Control Control { get; set; }
		public GamepadButtonFlags Button { get; set; }
		public MapCode Code { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
	}
}
