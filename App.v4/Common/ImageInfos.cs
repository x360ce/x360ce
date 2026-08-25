using SharpDX.XInput;
using System.Collections.Generic;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public class ImageInfos : List<ImageInfo>
	{
		/// <summary>
		/// Pair a control with the pad control it maps, and give it that name.
		/// </summary>
		/// <remarks>
		/// This is the one place a control meets its map code, so it is the one place that can
		/// name it. Naming here keeps every mapping control described the same way without a
		/// second table to keep in step with this one.
		/// </remarks>
		public void Add(int image, MapCode code, double x, double y, Control label, Control control, GamepadButtonFlags button = GamepadButtonFlags.None)
		{
			var info = new ImageInfo(image, code, x, y, label, control, button);
			if (control != null)
				control.AccessibleName = info.Name;
			Add(info);
		}
	}
}
