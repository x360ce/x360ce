using JocysCom.ClassLibrary.Controls;
using System.Windows;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for BaseWithHeaderControl.xaml
	/// </summary>
	public partial class BaseWithHeaderControl : UserControl
	{

		public BaseWithHeaderControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (IsDesignMode)
				return;
		}

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

		internal bool IsDesignMode => JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this);

		#region ■ IBaseWithHeaderControl

		public Window Window => System.Windows.Window.GetWindow(this);

		#endregion

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
				: Application.Current.Resources[image];
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

	}
}
