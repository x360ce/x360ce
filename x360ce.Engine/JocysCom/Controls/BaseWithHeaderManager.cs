using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JocysCom.ClassLibrary.Controls
{
	public class BaseWithHeaderManager<T> : IBaseWithHeaderControl<T>, IDisposable
	{

		public BaseWithHeaderManager(Label headLabel, TextBlock bodyLabel, ContentControl leftIcon, ContentControl rightIcon, ContentControl control)
		{
			_Control = control;
			_HeadLabel = headLabel;
			defaultHead = headLabel.Content as string;
			_BodyLabel = bodyLabel;
			defaultBody = bodyLabel.Text;
			_LeftIcon = leftIcon;
			_RightIcon = rightIcon;
			_RightIconOriginalContent = _RightIcon.Content;
			_RotateTransform = new RotateTransform();
			_RightIcon.RenderTransform = _RotateTransform;
			_RightIcon.RenderTransformOrigin = new Point(0.5, 0.5);
			RotateTimer = new System.Timers.Timer();
			RotateTimer.Interval = 25;
			RotateTimer.Elapsed += RotateTimer_Elapsed;
		}

		string defaultHead;
		Label _HeadLabel;
		string defaultBody;
		TextBlock _BodyLabel;
		ContentControl _LeftIcon;
		ContentControl _RightIcon;
		object _RightIconOriginalContent;
		ContentControl _Control;
		private readonly object TasksLock = new object();
		private readonly BindingList<T> Tasks = new BindingList<T>();

		/// <summary>Activate busy spinner.</summary>
		public void AddTask(T name)
		{
			lock (TasksLock)
			{
				if (!Tasks.Contains(name))
					Tasks.Add(name);
				UpdateIcon();
			}
		}

		/// <summary>Deactivate busy spinner if all tasks are gone.</summary>
		public void RemoveTask(T name)
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
			if (Tasks.Count > 0)
			{
				_RightIcon.Content = Icons.Current[Icons.Icon_ProcessRight];
				_RightIcon.RenderTransform = _RotateTransform;
				RotateTimer.Start();
			}
			else
			{
				RotateTimer.Stop();
				_RightIcon.RenderTransform = null;
				_RotateTransform.Angle = 0;
				_RightIcon.Content = _RightIconOriginalContent;
			}
		}

		RotateTransform _RotateTransform;
		System.Timers.Timer RotateTimer;

		private void RotateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
			_RightIcon.Dispatcher.Invoke(() =>
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
			{
				var angle = (_RotateTransform.Angle + 2) % 360;
				_RotateTransform.Angle = angle;
			});
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
					_LeftIcon.Content = Icons.Current[Icons.Icon_Error];
					break;
				case MessageBoxImage.Question:
					_BodyLabel.Foreground = new SolidColorBrush(Colors.DarkBlue);
					_LeftIcon.Content = Icons.Current[Icons.Icon_Question];
					break;
				case MessageBoxImage.Warning:
					_BodyLabel.Foreground = new SolidColorBrush(Colors.DarkOrange);
					_LeftIcon.Content = Icons.Current[Icons.Icon_Warning];
					break;
				default:
					_BodyLabel.Foreground = SystemColors.ControlTextBrush;
					_LeftIcon.Content = Icons.Current[Icons.Icon_Information];
					break;
			}
		}

		#region ■ IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public bool IsDisposing;

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
				// Free managed resources.
				_Control = null;
				_HeadLabel = null;
				_BodyLabel = null;
				_LeftIcon = null;
			}
		}

		#endregion
	}
}
