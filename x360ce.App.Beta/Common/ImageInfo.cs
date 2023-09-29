using SharpDX.XInput;
using x360ce.Engine;

namespace x360ce.App
{
	public class ImageInfo
	{
		// Content control.
		public ImageInfo(int image, MapCode code, double x, double y, object label, object control, GamepadButtonFlags button = GamepadButtonFlags.None)
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
		public object Label { get; set; }

		public System.Windows.Shapes.Path Path { get; set; }

		public object Control { get; set; }
		public GamepadButtonFlags Button { get; set; }
		public MapCode Code { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
	}
}
