using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
		public List<ImageInfo> Infos;
		MapTo MappedTo;

		public void InitializeImages(List<ImageInfo> imageInfos, PadControlImager imager, MapTo mappedTo)
		{
			Infos = imageInfos;
			Imager = imager;
			MappedTo = mappedTo;

			foreach (var ii in imageInfos)
			{
				ii.Path = FindName(GetNameCode(ii.Code).ToString()) as Path;
				//SetImage(ii.Code, NavImageType.Normal, false);

				TextBox textBox = ii.ControlBindedName as TextBox;
				textBox.MouseEnter += delegate (object sender, MouseEventArgs e) { setNormalOverActiveRecordColor(sender, colorOver); };
				textBox.MouseLeave += delegate (object sender, MouseEventArgs e) { setNormalOverActiveRecordColor(sender, colorNormalPath); };
				// textBox.MouseUp += delegate (object sender, MouseButtonEventArgs e) { setNormalOverActiveRecordColor(sender, colorRecord); };

				Path path = ii.Path;
				path.MouseEnter += delegate (object sender, MouseEventArgs e) { setNormalOverActiveRecordColor(sender, colorOver); };
				path.MouseLeave += delegate (object sender, MouseEventArgs e) { setNormalOverActiveRecordColor(sender, colorNormalPath); };
				path.MouseUp += delegate (object sender, MouseButtonEventArgs e) { setNormalOverActiveRecordColor(sender, colorRecord); };
			}
			SetHelpText();
		}

		public MapCode GetNameCode(MapCode code)
		{
			switch (code)
			{
				case MapCode.LeftThumbAxisX:
					return MapCode.LeftThumbRight;
				case MapCode.LeftThumbAxisY:
					return MapCode.LeftThumbUp;
				case MapCode.RightThumbAxisX:
					return MapCode.RightThumbRight;
				case MapCode.RightThumbAxisY:
					return MapCode.RightThumbUp;
				default:
					return code;
			}
		}

		//public void SetImage(MapCode code, NavImageType type, bool show)
		//{
		//	var nameCode = GetNameCode(code);
		//	var ii = Infos.First(x => x.Code == nameCode);
		//	var resourceName = string.Format("NavColor{0}", type);
		//	var path = FindName(nameCode.ToString()) as Path;
		//	path.Fill = FindResource(resourceName) as SolidColorBrush;
		//}

		//public void SetImage(MapCode code, NavImageType type, bool show)
		//{
		//	var nameCode = GetNameCode(code);
		//	var ii = Infos.First(x => x.Code == nameCode);
		//	var m = GetMiddleImageName(code);
		//	var resourceName = string.Format("Nav{0}{1}", m, type);
		//	var oldResourceName = ii.CurrentImageName;
		//	var isNameSame = ii.CurrentImageName == resourceName;
		//	var isShowSame = ii.CurrentImageShow == show;
		//	// Return if no changes must be made.
		//	// This will fix issue when click on image path is ignored.
		//	if (isNameSame && isShowSame)
		//		return;
		//	ii.CurrentImageName = resourceName;
		//	ii.CurrentImageShow = show;
		//	var path = FindName(nameCode.ToString()) as Button;
		//	if (path == null)
		//		return;
		//	var content = (ContentControl)path.Content;
		//	if (!isNameSame)
		//	{
		//		var vb = FindResource(resourceName) as UIElement;
		//		content.Content = vb;
		//	}
		//	// Opacity must be re-applied if image changed.
		//	if (!isShowSame || !isNameSame)
		//		content.Opacity = type == NavImageType.Record
		//			? (show ? 0.8F : 0.2f)
		//			: (show ? 0.8F : 0.0f);
		//}

		//public string GetMiddleImageName(MapCode code)
		//{
		//	if (code == MapCode.LeftTrigger || code == MapCode.RightTrigger)
		//		return "Up";
		//	var rx = new Regex("(Up|Left|Right|Down)$");
		//	var ms = rx.Matches(code.ToString());
		//	var m = ms.Count > 0 ? ms[0].Value : "";
		//	return m;
		//}

		//public static System.Drawing.Bitmap CopyRegionIntoImage(System.Drawing.Bitmap source, int x, int y, int width, int height)
		//{
		//	var region = new System.Drawing.Rectangle(x, y, width, height);
		//	return source.Clone(region, source.PixelFormat);
		//}

		//public System.Drawing.Bitmap ClipToCircle(System.Drawing.Bitmap original, float x, float y, float radius)
		//{
		//	var copy = new System.Drawing.Bitmap(original);
		//	using (var g = System.Drawing.Graphics.FromImage(copy))
		//	{
		//		var r = new System.Drawing.RectangleF(x - radius, y - radius, radius * 2, radius * 2);
		//		var path = new System.Drawing.Drawing2D.GraphicsPath();
		//		path.AddEllipse(r);
		//		g.Clip = new System.Drawing.Region(path);
		//		g.DrawImage(original, 0, 0);
		//		return copy;
		//	}
		//}

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
				// Record only if different path was clicked.
				record = map != currentMap;
			}
			if (record)
				StartRecording(map);
			//SetImage(code, NavImageType.Record, record);
		}

		public Action<SettingsMapItem> StartRecording;
		public Func<bool> StopRecording;

		//private void MainGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		//{
		//	if (ControlsHelper.IsDesignMode(this))
		//		return;

		//	Imager.ShowLeftThumbButtons = InRange(e, LeftThumbGrid);
		//	Imager.ShowRightThumbButtons = InRange(e, RightThumbGrid);
		//	Imager.ShowDPadButtons = InRange(e, DPadGrid);
		//	Imager.ShowMainButtons = InRange(e, MainButtonsGrid);
		//	Imager.ShowMenuButtons = InRange(e, MenuButtonsGrid);
		//	Imager.ShowTriggerButtons = InRange(e, TriggerButtonsGrid);
		//	Imager.ShowShoulderButtons = InRange(e, ShoulderButtonsGrid);
		//}

		//bool InRange(System.Windows.Input.MouseEventArgs e, FrameworkElement control)
		//{
		//	var path = e.GetPosition(control);
		//	return
		//		Math.Abs(path.X - (control.Width / 2F)) < control.Width / 2F &&
		//		Math.Abs(path.Y - (control.Height / 2F)) < control.Height / 2F;
		//}

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

		public SolidColorBrush colorActive = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF42C765");
		public SolidColorBrush colorNormalPath = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF6699FF");
		public SolidColorBrush colorNormalTextBox = System.Windows.Media.Brushes.White;
		public SolidColorBrush colorOver = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFCC66");
		public SolidColorBrush colorRecord = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFF6B66");

		// Set navigation color.
		public void setNormalOverActiveRecordColor(object sender, SolidColorBrush setColor)
		{	
			SolidColorBrush senderColor = colorNormalPath;
			if (sender is Path) { senderColor = ((Path)sender).Fill as SolidColorBrush; }
			else if (sender is TextBox) { senderColor = ((TextBox)sender).Background as SolidColorBrush; }
			else if (sender is ImageInfo) senderColor = (sender as ImageInfo).Path.Fill as SolidColorBrush;
			else if (sender is Button) { senderColor = colorRecord; sender = (sender as Button).Tag as TextBox; }

			// Only colorRecord can change colorRecord color.
			if (senderColor == colorRecord)
			{
				if (setColor == colorRecord) setColor = colorNormalPath;
				else return;
			}
			// Sender is ImageInfo. Set Active, Normal background color.
			if (sender is ImageInfo)
			{			
				var senderPath = (sender as ImageInfo).Path;
				var senderText = (sender as ImageInfo).ControlBindedName as TextBox;

				if (senderColor.Color != colorRecord.Color && senderColor.Color != colorOver.Color)
				{				
					senderText.Background = setColor == colorNormalPath ? colorNormalTextBox : setColor;
					senderPath.Fill = setColor;
				}
			}
			// Sender is Path, TextBox. Set Normal, Over, Record background color.
			else
			{
				if (Infos == null)
				{
					X360ControllerControl_Viewbox.Opacity = 0.3;
					X360ControllerControl_Viewbox.IsHitTestVisible = false;
					return;
				}
				else
				{
					X360ControllerControl_Viewbox.Opacity = 1;
					X360ControllerControl_Viewbox.IsHitTestVisible = true;

					foreach (var ii in Infos.Where(ii => sender == ii.Path || sender == ii.ControlBindedName as TextBox))
					{
						ii.Path.Fill = setColor;
						(ii.ControlBindedName as TextBox).Background = (setColor.Color == colorNormalPath.Color) ? colorNormalTextBox : setColor;
					}
				}
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			// MainGrid.MouseMove -= MainGrid_MouseMove;
			Infos?.Clear();
			Infos = null;
			Imager = null;
		}

	}
}
