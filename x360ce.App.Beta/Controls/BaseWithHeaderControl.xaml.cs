using JocysCom.ClassLibrary.Controls;
using System.Windows;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for BaseWithHeaderControl.xaml
	/// </summary>
	public partial class BaseWithHeaderControl : UserControl, IBaseWithHeaderControl<TaskName>
	{

		public BaseWithHeaderControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (IsDesignMode)
				return;
			defaultHead = HelpHeadLabel.Content as string;
			defaultBody = HelpBodyLabel.Text;
			_bwm = new BaseWithHeaderManager<TaskName>(HelpHeadLabel, HelpBodyLabel, LeftIcon, RightIcon, this);
		}

		BaseWithHeaderManager<TaskName> _bwm;


		/// <summary>
		/// Gets or sets additional content for the UserControl
		/// </summary>
		public object MainContent
		{
			get { return GetValue(MainContentProperty); }
			set { SetValue(MainContentProperty, value); }
		}

		public static readonly DependencyProperty MainContentProperty =
			DependencyProperty.Register(nameof(MainContent), typeof(object), typeof(BaseWithHeaderControl),
			  new PropertyMetadata(null));

		private readonly string defaultHead;
		private readonly string defaultBody;

		internal bool IsDesignMode => JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this);

		#region ■ IBaseWithHeaderControl

		public Window Window => System.Windows.Window.GetWindow(this);

		public void AddTask(TaskName name)
			=> _bwm.AddTask(name);

		public void RemoveTask(TaskName name)
			=> _bwm.RemoveTask(name);
		
		public void SetTitle(string format, params object[] args)
			=> _bwm.SetTitle(format, args);

		public void SetHead(string format, params object[] args)
			=> _bwm.SetHead(format, args);

		public void SetBody(MessageBoxImage image, string format, params object[] args)
			=> _bwm.SetBody(image, format, args);

		public void SetBodyError(string format, params object[] args)
			=> _bwm.SetBodyError(format, args);

		public void SetBodyInfo(string format, params object[] args)
			=> _bwm.SetBodyInfo(format, args);

		#endregion

		object _Image;

		public void SetImage(string resource)
		{
			_Image = App.GetResource(resource);
			RightIcon.Content = _Image;
		}

		public void SetImage(Viewbox resource)
		{
			_Image = resource;
			RightIcon.Content = _Image;
		}

		public void SetButton1(string text = null, string image = null)
			=> SetButton(Button1, Button1Label, Button1Content, text, image);

		public void SetButton2(string text = null, string image = null)
			=> SetButton(Button2, Button2Label, Button2Content, text, image);

		public void SetButton3(string text = null, string image = null)
			=> SetButton(Button3, Button3Label, Button3Content, text, image);

		public void SetButton(Button button, Label label, ContentControl content, string text, string image)
		{
			button.Visibility = string.IsNullOrEmpty(text)
				? Visibility.Collapsed
				: Visibility.Visible;
			label.Content = text;
			content.Content = string.IsNullOrEmpty(image)
				? null
				: App.GetResource(image);
		}

		private void Button1_Click(object sender, RoutedEventArgs e)
		{
			Window.DialogResult = true;
		}

		private void Button2_Click(object sender, RoutedEventArgs e)
		{
			Window.DialogResult = false;
		}

		private void Button3_Click(object sender, RoutedEventArgs e)
		{
			Window.DialogResult = null;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_bwm.Dispose();
		}
	}
}
