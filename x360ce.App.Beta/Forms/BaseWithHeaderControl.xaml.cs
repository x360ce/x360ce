using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for BaseWithHeaderControl.xaml
	/// </summary>
	public partial class BaseWithHeaderControl : UserControl
	{

		public BaseWithHeaderControl()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			defaultHead = HelpHeadLabel.Content as string;
			defaultBody = HelpBodyLabel.Content as string;
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

		private readonly string defaultHead;
		private readonly string defaultBody;

		internal bool IsDesignMode => JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this);

		#region WebService loading circle

		private readonly object TasksLock = new object();
		private readonly BindingList<TaskName> Tasks = new BindingList<TaskName>();

		public Window Window => System.Windows.Window.GetWindow(this);

		/// <summary>Activate busy spinner.</summary>
		public void AddTask(TaskName name)
		{
			lock (TasksLock)
			{
				Tasks.Add(name);
				UpdateIcon();
			}
		}

		/// <summary>Deactivate busy spinner if all tasks are gone.</summary>
		public void RemoveTask(TaskName name)
		{
			lock (TasksLock)
			{
				if (Tasks.Contains(name))
					Tasks.Remove(name);
				UpdateIcon();
			}
		}

		public void UpdateIcon()
		{
		}

		#endregion

		public void SetTitle(string format, params object[] args)
		{
			Window.Title = (args.Length == 0)
				? format
				: string.Format(format, args);
		}

			public void SetHead(string format, params object[] args)
		{
			// Apply format.
			if (format == null)
				format = defaultHead;
			else if (args.Length > 0)
				format = string.Format(format, args);
			if (HelpHeadLabel.Content as string != format)
			{
				HelpHeadLabel.Content = format;
			}
		}

		object _Image;

		public void SetImage(string resource)
		{
			_Image = Resources[resource];
			RightIcon.Content = _Image;
		}

		public void SetImage(Viewbox resource)
		{
			_Image = resource;
			RightIcon.Content = _Image;
		}

		public void SetBodyError(string content, params object[] args)
		{
			// Apply format.
			if (content == null)
				content = defaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			// Set info with time.
			SetBody(MessageBoxImage.Error, "{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, content);
		}

		public void SetBodyInfo(string content, params object[] args)
		{
			// Apply format.
			if (content == null)
				content = defaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			// Set info with time.
			SetBody(MessageBoxImage.Information, content);
		}

		public void SetBody(MessageBoxImage image, string content = null, params object[] args)
		{
			if (content == null)
				content = defaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			HelpBodyLabel.Content = content;
			// Update body colors.
			switch (image)
			{
				case MessageBoxImage.Error:
					HelpBodyLabel.Foreground = new SolidColorBrush(Colors.DarkRed);
					LeftIcon.Content = Resources[Icons_Default.Icon_error];
					break;
				case MessageBoxImage.Question:
					HelpBodyLabel.Foreground = new SolidColorBrush(Colors.DarkBlue);
					LeftIcon.Content = Resources[Icons_Default.Icon_question];
					break;
				case MessageBoxImage.Warning:
					HelpBodyLabel.Foreground = new SolidColorBrush(Colors.DarkOrange);
					LeftIcon.Content = Resources[Icons_Default.Icon_sign_warning];
					break;
				default:
					HelpBodyLabel.Foreground = SystemColors.ControlTextBrush;
					LeftIcon.Content = Resources[Icons_Default.Icon_information];
					break;
			}
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
				: Resources[image];
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
