using SharpDX.XInput;
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

		public bool? CurrentImageShow { get; set; }
		public string CurrentImageName { get; set; }

		public int Image { get; set; }
		public Control Label { get; set; }

		public System.Windows.Controls.Button ButtonControl { get; set; }
		public Control Control { get; set; }
		public GamepadButtonFlags Button { get; set; }
		public MapCode Code { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
	}
}
