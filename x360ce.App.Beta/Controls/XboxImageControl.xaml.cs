using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for XboxImageControl.xaml
	/// </summary>
	public partial class XboxImageControl : UserControl
	{
		public XboxImageControl()
		{
			InitializeComponent();
		}

		PadControlImager Imager;
		ImageInfos Infos;

		public void InitializeImages(ImageInfos imageInfos, PadControlImager imager)
		{
			Infos = imageInfos;
			Imager = imager;
			foreach (var ii in imageInfos)
			{
				var nameCode = GetNameCode(ii.Code);
				var button = FindName(nameCode.ToString()) as Button;
				ii.ButtonControl = button;
				SetImage(ii.Code, NavImageType.Normal, false);
			}
		}

		public LayoutCode GetNameCode(LayoutCode code)
		{
			if (code == LayoutCode.LeftThumbAxisX)
				return LayoutCode.LeftThumbRight;
			if (code == LayoutCode.LeftThumbAxisY)
				return LayoutCode.LeftThumbUp;
			if (code == LayoutCode.RightThumbAxisX)
				return LayoutCode.RightThumbRight;
			if (code == LayoutCode.RightThumbAxisY)
				return LayoutCode.RightThumbUp;
			return code;
		}

		public void SetImage(LayoutCode code, NavImageType type, bool show)
		{
			var nameCode = GetNameCode(code);
			var ii = Infos.First(x => x.Code == nameCode);
			var m = GetMiddleImageName(code);
			var resourceName = string.Format("Nav{0}{1}", m, type);
			var isNameSame = ii.CurrentImageName == resourceName;
			var isShowSame = ii.CurrentImageShow.HasValue && ii.CurrentImageShow == show;
			// Return if no changes must be made.
			// This will fix issue when click on image button is ignored.
			if (isNameSame && isShowSame)
				return;
			ii.CurrentImageName = resourceName;
			ii.CurrentImageShow = show;
			var button = FindName(nameCode.ToString()) as Button;
			if (button == null)
				return;
			var content = (ContentControl)button.Content;
			if (!isNameSame)
			{
				var vb = FindResource(resourceName) as Viewbox;
				content.Content = vb;
			}
			if (!isShowSame)
				content.Opacity = type == NavImageType.Record
					? (show ? 0.8F : 0.2f)
					: (show ? 0.6F : 0.0f);
		}

		public string GetMiddleImageName(LayoutCode code)
		{
			if (code == LayoutCode.LeftTrigger || code == LayoutCode.RightTrigger)
				return "Up";
			var rx = new Regex("(Up|Left|Right|Down)$");
			var ms = rx.Matches(code.ToString());
			var m = ms.Count > 0 ? ms[0].Value : "";
			return m;
		}

		public static System.Drawing.Bitmap CopyRegionIntoImage(System.Drawing.Bitmap source, int x, int y, int width, int height)
		{
			var region = new System.Drawing.Rectangle(x, y, width, height);
			return source.Clone(region, source.PixelFormat);
		}

		public System.Drawing.Bitmap ClipToCircle(System.Drawing.Bitmap original, float x, float y, float radius)
		{
			var copy = new System.Drawing.Bitmap(original);
			using (var g = System.Drawing.Graphics.FromImage(copy))
			{
				var r = new System.Drawing.RectangleF(x - radius, y - radius, radius * 2, radius * 2);
				var path = new System.Drawing.Drawing2D.GraphicsPath();
				path.AddEllipse(r);
				g.Clip = new System.Drawing.Region(path);
				g.DrawImage(original, 0, 0);
				return copy;
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var control = (Button)sender;
			var name = control.Name;
			LayoutCode code;
			if (!Enum.TryParse(name, false, out code))
				return;
			var comboBox = Infos.Where(x => x.Code == code).Select(x => x.Control).First();
			var map = SettingsManager.Current.SettingsMap.First(x => x.Control == comboBox);
			var record = true;
			// If already recording then stop.
			if (Imager.Recorder.Recording)
			{
				var currentMap = Imager.Recorder.CurrentMap;
				Imager.Recorder.StopRecording();
				// Record only if different button was clicked.
				record = map != currentMap;
			}
			if (record)
				StartRecording(map);
			//SetImage(code, NavImageType.Record, record);
		}

		public Action<SettingsMapItem> StartRecording;

		private void MainGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			Imager.ShowLeftThumbButtons = InRange(e, LeftThumbGrid);
			Imager.ShowRightThumbButtons = InRange(e, RightThumbGrid);
			Imager.ShowDPadButtons = InRange(e, DPadGrid);
			Imager.ShowMainButtons = InRange(e, MainButtonsGrid);
			Imager.ShowMenuButtons = InRange(e, MenuButtonsGrid);
			Imager.ShowTriggerButtons = InRange(e, TriggerButtonsGrid);
			Imager.ShowShoulderButtons = InRange(e, ShoulderButtonsGrid);
		}

		bool InRange(System.Windows.Input.MouseEventArgs e, FrameworkElement control)
		{
			var p = e.GetPosition(control);
			return
				Math.Abs(p.X - (control.Width / 2F)) < control.Width / 2F &&
				Math.Abs(p.Y - (control.Height / 2F)) < control.Height / 2F;
		}
	}

}
