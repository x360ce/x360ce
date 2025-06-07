﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for MessageBoxWindow.xaml
	/// </summary>
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
	public partial class MessageBoxWindow : Window
	{
		/// <summary>
		/// Make sure to set window Owner to properly dispose after closing.
		/// </summary>
		public MessageBoxWindow()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			LinkTextBlock.Visibility = Visibility.Collapsed;
			// Center owner.
			var owner = Application.Current?.MainWindow;
			if (owner != null)
			{
				Owner = owner;
				WindowStartupLocation = WindowStartupLocation.CenterOwner;
			}
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
			var box = new MessageBoxWindow();
			return box.ShowDialog(message, caption, button, icon, defaultResult, options);
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
			MessageTextBlock.Visibility = Visibility.Visible;
			MessageTextBox.Visibility = Visibility.Collapsed;
			LinkLabel.Visibility = Visibility.Collapsed;
			SizeLabel.Visibility = Visibility.Visible;
			Title = caption;
			MessageTextBlock.Text = message;
			_SwitchButton(button, defaultResult);
			_SwitchIcon(icon);
			// Get text size.
			var size = MeasureString(message, MessageTextBlock);
			if (size.Width > 960)
			{
				size = ApplyAspectRatio(size);
				MessageTextBlock.MaxWidth = Math.Round(size.Width, 0);
			}
			if (ControlsHelper.GetMainFormTopMost())
				Topmost = true;
			// Show form.
			var result = ShowDialog();
			return Result;
		}

		/// <summary>Displays a message box that has a message, title bar caption, button, and icon; and that accepts a default message box result, complies with the specified options, and returns a result.</summary>
		/// <param name="message">A that specifies the text to display.</param>
		/// <param name="caption">A that specifies the title bar caption to display.</param>
		/// <param name="button">A value that specifies which button or buttons to display.</param>
		/// <param name="icon">A value that specifies the icon to display.</param>
		/// <param name="defaultResult">A value that specifies the default result of the message box.</param>
		/// <param name="options">A value object that specifies the options.</param>
		public MessageBoxResult ShowPrompt(
			string message,
			string caption = "",
			MessageBoxButton button = MessageBoxButton.OKCancel,
			MessageBoxImage icon = MessageBoxImage.Information,
			MessageBoxResult defaultResult = MessageBoxResult.OK,
			MessageBoxOptions options = MessageBoxOptions.None
		)
		{
			MessageTextBlock.Visibility = Visibility.Collapsed;
			MessageTextBox.Visibility = Visibility.Visible;
			LinkLabel.Visibility = Visibility.Collapsed;
			SizeLabel.Visibility = Visibility.Visible;
			Title = caption;
			MessageTextBox.Text = message;
			_SwitchButton(button, defaultResult);
			_SwitchIcon(icon);
			// Set size.
			Loaded -= MessageBoxWindow_Loaded;
			Loaded += MessageBoxWindow_Loaded;
			// Update size label.
			UpdateSizeLabel();
			if (ControlsHelper.GetMainFormTopMost())
				Topmost = true;
			// Attach a new Loaded event handler specifically for focusing the message text box
			Loaded += FocusMessageTextBox;
			// Show form.
			var result = ShowDialog();
			// Clean up by removing the event handler after the dialog is closed
			Loaded -= FocusMessageTextBox;
			return Result;
		}

		private void FocusMessageTextBox(object sender, RoutedEventArgs e)
		{
			// Set focus to the MessageTextBox control.
			MessageTextBox.Focus();
			// Set the caret position to the end of the text
			MessageTextBox.CaretIndex = MessageTextBox.Text.Length;
		}

		public void SetSize(double width = 0, double height = 0)
		{
			if (width > 0 && height > 0)
			{
				SizeToContent = SizeToContent.Manual;
				Width = width;
				Height = height;
			}
			else
			{
				SizeToContent = SizeToContent.WidthAndHeight;
			}
		}

		private void MessageBoxWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (SizeToContent == SizeToContent.Manual)
			{
				ControlsHelper.CenterWindowOnApplication(this);
			}
			else
			{
				// Get text size (from 256 to 512).
				var measureSize = Math.Min(Math.Max(256, MessageTextBlock.Text.Length), 512);
				var measureMessage = new string('a', measureSize);
				var size = MeasureString(measureMessage, MessageTextBlock);
				size = ApplyAspectRatio(size);
				var boxWidth = Math.Round(size.Width, 0);
				var boxHeight = Math.Round(size.Height, 0);
				// Set window size.
				var winWidthDif = Width - MessageTextBox.ActualWidth;
				var winHeightDif = Height - MessageTextBox.ActualHeight;
				SizeToContent = SizeToContent.Manual;
				Width = boxWidth + winWidthDif;
				Height = boxHeight + winHeightDif;
			}
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

		private void _SwitchButton(MessageBoxButton button, MessageBoxResult defaultResult)
		{
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
			var buttons = new[] { Button1, Button2, Button3 };
			foreach (var b in buttons)
			{
				if ((MessageBoxResult)b.Tag == MessageBoxResult.Cancel)
					b.IsCancel = true;
				if ((MessageBoxResult)b.Tag == defaultResult)
					b.IsDefault = true;
			}
		}

		private void _SwitchIcon(MessageBoxImage icon)
		{
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
		}

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
			LinkLabel.Text = text;
		}

		private void MainHyperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			OpenUrl(e.Uri.AbsoluteUri);
		}

		#region Helper Methods

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

		#endregion



		private void MessageTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			UpdateSizeLabel();
		}

		void UpdateSizeLabel()
		{
			var text = (MessageTextBox.MaxLength - MessageTextBox.Text.Length).ToString();
			ControlsHelper.SetText(SizeLabel, text);
			ControlsHelper.SetVisible(SizeLabel, MessageTextBox.MaxLength > 0);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			// Center message box window in application.
			if (Owner is null)
				ControlsHelper.CenterWindowOnApplication(this);
		}

		private void Window_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Owner = null;
		}
	}
}
