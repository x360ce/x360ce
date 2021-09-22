using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for XboxImageControl.xaml
	/// </summary>
	public partial class PadItem_General_XboxImageControl : UserControl
	{
		public PadItem_General_XboxImageControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		PadControlImager Imager;
		List<ImageInfo> Infos;
		MapTo MappedTo;

		public void InitializeImages(List<ImageInfo> imageInfos, PadControlImager imager, MapTo mappedTo)
		{
			Infos = imageInfos;
			Imager = imager;
			MappedTo = mappedTo;
			foreach (var ii in imageInfos)
			{
				var nameCode = GetNameCode(ii.Code);
				var button = FindName(nameCode.ToString()) as Button;
				ii.ButtonControl = button;
				SetImage(ii.Code, NavImageType.Normal, false);
			}
			SetHelpText();
			MainGrid.MouseMove += MainGrid_MouseMove;
		}

		public MapCode GetNameCode(MapCode code)
		{
			if (code == MapCode.LeftThumbAxisX)
				return MapCode.LeftThumbRight;
			if (code == MapCode.LeftThumbAxisY)
				return MapCode.LeftThumbUp;
			if (code == MapCode.RightThumbAxisX)
				return MapCode.RightThumbRight;
			if (code == MapCode.RightThumbAxisY)
				return MapCode.RightThumbUp;
			return code;
		}

		public void SetImage(MapCode code, NavImageType type, bool show)
		{
			var nameCode = GetNameCode(code);
			var ii = Infos.First(x => x.Code == nameCode);
			var m = GetMiddleImageName(code);
			var resourceName = string.Format("Nav{0}{1}", m, type);
			var oldResourceName = ii.CurrentImageName;
			var isNameSame = ii.CurrentImageName == resourceName;
			var isShowSame = ii.CurrentImageShow == show;
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
			// Opacity must be re-applied if image changed.
			if (!isShowSame || !isNameSame)
				content.Opacity = type == NavImageType.Record
					? (show ? 0.8F : 0.2f)
					: (show ? 0.8F : 0.0f);
		}

		public string GetMiddleImageName(MapCode code)
		{
			if (code == MapCode.LeftTrigger || code == MapCode.RightTrigger)
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
			MapCode code;
			if (!Enum.TryParse(name, false, out code))
				return;
			var map = SettingsManager.Current.SettingsMap.FirstOrDefault(x => x.MapTo == MappedTo && x.Code == code);
			if (map == null)
				return;
			var record = true;
			// If already recording then stop.
			if (Imager.Recorder.Recording)
			{
				var currentMap = Imager.Recorder.CurrentMap;
				StopRecording();
				// Record only if different button was clicked.
				record = map != currentMap;
			}
			if (record)
				StartRecording(map);
			//SetImage(code, NavImageType.Record, record);
		}

		public Action<SettingsMapItem> StartRecording;
		public Func<bool> StopRecording;

		private void MainGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
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

		public string MappingDone = "Mapping Done";

		public void SetHelpText(string text = null)
		{
			HelpTextLabel.Content = text ?? "";
			if (string.IsNullOrEmpty(text))
				return;
			ControlsHelper.BeginInvoke(() =>
			{
				HelpTextLabel.Content = "";
			}, 4000);
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			MainGrid.MouseMove -= MainGrid_MouseMove;
			Infos?.Clear();
			Infos = null;
			Imager = null;
		}
	}

}
