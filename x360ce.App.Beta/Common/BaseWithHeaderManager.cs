using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using x360ce.App.Controls;

namespace x360ce.App
{
	public class BaseWithHeaderManager : IBaseWithHeaderControl
	{

		public BaseWithHeaderManager(Label headLabel, TextBlock bodyLabel, ContentControl leftIcon, ContentControl control)
		{
			_Control = control;
			_HeadLabel = headLabel;
			defaultHead = headLabel.Content as string;
			_BodyLabel = bodyLabel;
			defaultBody = bodyLabel.Text;
			_LeftIcon = leftIcon;
		}

		string defaultHead;
		Label _HeadLabel;
		string defaultBody;
		TextBlock _BodyLabel;
		ContentControl _LeftIcon;
		ContentControl _Control;
		private readonly object TasksLock = new object();
		private readonly BindingList<TaskName> Tasks = new BindingList<TaskName>();

		/// <summary>Activate busy spinner.</summary>
		public void AddTask(TaskName name)
		{
			lock (TasksLock)
			{
				if (!Tasks.Contains(name))
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

		public void SetHead(string format, params object[] args)
		{
			// Apply format.
			if (format == null)
				format = defaultHead;
			else if (args.Length > 0)
				format = string.Format(format, args);
			if (_HeadLabel.Content as string != format)
				_HeadLabel.Content = format;
		}

		public void SetTitle(string format, params object[] args)
		{
			if (_Control is Window w)
			{
				w.Title = (args.Length == 0)
					? format
					: string.Format(format, args);
			}
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
			_BodyLabel.Text = content;
			// Update body colors.
			switch (image)
			{
				case MessageBoxImage.Error:
					_BodyLabel.Foreground = new SolidColorBrush(Colors.DarkRed);
					_LeftIcon.Content = Icons_Default.Current[Icons_Default.Icon_error];
					break;
				case MessageBoxImage.Question:
					_BodyLabel.Foreground = new SolidColorBrush(Colors.DarkBlue);
					_LeftIcon.Content = Icons_Default.Current[Icons_Default.Icon_question];
					break;
				case MessageBoxImage.Warning:
					_BodyLabel.Foreground = new SolidColorBrush(Colors.DarkOrange);
					_LeftIcon.Content = Icons_Default.Current[Icons_Default.Icon_sign_warning];
					break;
				default:
					_BodyLabel.Foreground = SystemColors.ControlTextBrush;
					_LeftIcon.Content = Icons_Default.Current[Icons_Default.Icon_information];
					break;
			}
		}

	}
}
