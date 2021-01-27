using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for MessageBoxWindow.xaml
	/// </summary>
	public partial class MessageBoxWindow : Window
	{
		public MessageBoxWindow()
		{
			InitializeComponent();
			LinkTextBlock.Visibility = Visibility.Collapsed;
		}

		/// <summary>Displays a message box that has a message, title bar caption, button, and icon; and that accepts a default message box result, complies with the specified options, and returns a result.</summary>
		/// <param name="message">A that specifies the text to display.</param>
		/// <param name="caption">A that specifies the title bar caption to display.</param>
		/// <param name="button">A value that specifies which button or buttons to display.</param>
		/// <param name="icon">A value that specifies the icon to display.</param>
		/// <param name="defaultResult">A value that specifies the default result of the message box.</param>
		/// <param name="options">A value object that specifies the options.</param>
		public static MessageBoxResult Show(
			string message,
			string caption = "",
			MessageBoxButton button = MessageBoxButton.OK,
			MessageBoxImage icon = MessageBoxImage.None,
			MessageBoxResult defaultResult = MessageBoxResult.None,
			MessageBoxOptions options = MessageBoxOptions.None
		)
		{
			var win = new MessageBoxWindow();
			//win.Topmost = window.Topmost;
			return win.ShowDialog(message, caption, button, icon, defaultResult, options);
		}

		/// <summary>Displays a message box that has a message, title bar caption, button, and icon; and that accepts a default message box result, complies with the specified options, and returns a result.</summary>
		/// <param name="message">A that specifies the text to display.</param>
		/// <param name="caption">A that specifies the title bar caption to display.</param>
		/// <param name="button">A value that specifies which button or buttons to display.</param>
		/// <param name="icon">A value that specifies the icon to display.</param>
		/// <param name="defaultResult">A value that specifies the default result of the message box.</param>
		/// <param name="options">A value object that specifies the options.</param>
		public MessageBoxResult ShowDialog(
		  string message,
		  string caption = "",
		  MessageBoxButton button = MessageBoxButton.OK,
		  MessageBoxImage icon = MessageBoxImage.None,
		  MessageBoxResult defaultResult = MessageBoxResult.None,
		  MessageBoxOptions options = MessageBoxOptions.None)
		{
			Title = caption;
			MessageTextBlock.Text = message;
			switch (button)
			{
				case MessageBoxButton.OK:
					EnableButtons(MessageBoxResult.OK);
					break;
				case MessageBoxButton.OKCancel:
					EnableButtons(MessageBoxResult.OK, MessageBoxResult.None, MessageBoxResult.Cancel);
					break;
				case MessageBoxButton.YesNoCancel:
					EnableButtons(MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel);
					break;
				case MessageBoxButton.YesNo:
					EnableButtons(MessageBoxResult.Yes, MessageBoxResult.No);
					break;
				default:
					break;
			}
			switch (icon)
			{
				case MessageBoxImage.Error:
					IconContent.Content = Resources["Icon_Error"];
					break;
				case MessageBoxImage.Question:
					IconContent.Content = Resources["Icon_Question"];
					break;
				case MessageBoxImage.Warning:
					IconContent.Content = Resources["Icon_Warning"];
					break;
				default:
					IconContent.Content = Resources["Icon_Information"];
					break;
			}
			var buttons = new[] { Button1, Button2, Button3 };
			foreach (var b in buttons)
			{
				if ((MessageBoxResult)b.Tag == MessageBoxResult.Cancel)
					b.IsCancel = true;
				if ((MessageBoxResult)b.Tag == defaultResult)
					b.IsDefault = true;
			}
			// Get text size.
			var size = MeasureString(message, MessageTextBlock);
			if (size.Width > 960)
			{
				size = ApplyAspectRatio(size);
				MessageTextBlock.MaxWidth = Math.Round(size.Width, 0);
			}
			var result = ShowDialog();
			return Result;
		}

		void EnableButtons(MessageBoxResult r1, MessageBoxResult r2 = MessageBoxResult.None, MessageBoxResult r3 = MessageBoxResult.None)
		{
			Button1.Tag = r1;
			Button1Label.Content = r1.ToString();
			Button1.Visibility = r1 == MessageBoxResult.None ? Visibility.Collapsed : Visibility.Visible;
			Button2.Tag = r2;
			Button2Label.Content = r2.ToString();
			Button2.Visibility = r2 == MessageBoxResult.None ? Visibility.Collapsed : Visibility.Visible;
			Button3.Tag = r3;
			Button3Label.Content = r3.ToString();
			Button3.Visibility = r3 == MessageBoxResult.None ? Visibility.Collapsed : Visibility.Visible;
		}

		private MessageBoxResult Result = MessageBoxResult.None;

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Result = (MessageBoxResult)((Button)sender).Tag;
			DialogResult = true;
		}

		public void SetLink(string text = null, Uri uri = null)
		{
			LinkTextBlock.Visibility = string.IsNullOrEmpty(text)
				? Visibility.Collapsed
				: Visibility.Visible;
			MainHyperLink.NavigateUri = uri;
			LinkLabel.Content = text;
		}

		private void MainHyperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			OpenUrl(e.Uri.AbsoluteUri);
		}

		public static void OpenUrl(string url)
		{
			try
			{
				System.Diagnostics.Process.Start(url);
			}
			catch (System.ComponentModel.Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
					MessageBox.Show(noBrowser.Message);
			}
			catch (Exception other)
			{
				MessageBox.Show(other.Message);
			}
		}

		private static Size ApplyAspectRatio(Size s, double w = 16, double h = 9)
		{
			var area = s.Width * s.Height;
			// w * x * h * x = area;
			var x = Math.Sqrt(area / w / h);
			return new Size(w * x, h * x);
		}

		private static Size MeasureString(string candidate, TextBlock control)
		{
			var formattedText = new FormattedText(
				candidate,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch),
				control.FontSize,
				Brushes.Black,
				new NumberSubstitution(),
				1);
			return new Size(formattedText.Width, formattedText.Height);
		}

	}
}
