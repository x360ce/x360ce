using SharpDX.XInput;
using x360ce.Engine;

namespace x360ce.App
{
	public class ImageInfo
	{
		// Content control.
		public ImageInfo(int image, TargetType type, MapCode code, double x, double y, object controlName, object controlStackPanel, object controlBindedName, GamepadButtonFlags button = GamepadButtonFlags.None)
		{
			Image = image;
			Type = type;
			Code = code;
			X = x;
			Y = y;
			ControlName = controlName;
			ControlStackPanel = controlStackPanel;
			ControlBindedName = controlBindedName;
			Button = button;
		}

		public int Image { get; set; }
		public TargetType Type { get; set; }
		public MapCode Code { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public object ControlName { get; set; }
		public object ControlStackPanel { get; set; }
		public object ControlBindedName { get; set; }
		public GamepadButtonFlags Button { get; set; }

		public string CurrentImageName { get; set; }
		public bool? CurrentImageShow { get; set; }
		public System.Windows.Shapes.Path Path { get; set; }


	}
}
