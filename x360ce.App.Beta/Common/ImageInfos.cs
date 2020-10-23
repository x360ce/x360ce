using SharpDX.XInput;
using System.Collections.Generic;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public class ImageInfos : List<ImageInfo>
	{
		public void Add(int image, LayoutCode code, double x, double y, Control label, Control control, GamepadButtonFlags button = GamepadButtonFlags.None)
			=> Add(new ImageInfo(image, code, x, y, label, control, button));
	}
}
