using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for InfoControl.xaml
	/// </summary>
	public partial class InfoControl : UserControl
	{
		public InfoControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (!ControlsHelper.IsDesignMode(this))
			{
				var assembly = Assembly.GetEntryAssembly();
				var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
				var description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute))).Description;
				DefaultHead = product;
				DefaultBody = description;
				SetHead(DefaultHead);
				SetBodyInfo(DefaultBody);
				InitRotation();
			}
		}

		#region ■ Properties

		public string DefaultHead { get; set; }
		public string DefaultBody { get; set; }

		public object RightIconContent
		{
			get => RightIcon.GetValue(ContentProperty);
			set => RightIcon.SetValue(ContentProperty, value);
		}

		object _Image;

		public void SetImage(object resource)
		{
			_Image = resource;
			RightIcon.Content = _Image;
		}

		#endregion

		#region Set Text

		public void SetTitle(string format, params object[] args)
		{
			var win = System.Windows.Window.GetWindow(this);
			if (win == null)
				return;
			win.Title = (args.Length == 0)
				? format
				: string.Format(format, args);
		}

		public void SetHead(string format, params object[] args)
		{
			// Apply format.
			if (format == null)
				format = DefaultHead;
			else if (args.Length > 0)
				format = string.Format(format, args);
			if (HeadLabel.Content as string != format)
				HeadLabel.Content = format;
		}

		public void SetBodyError(string content, params object[] args)
		{
			// Apply format.
			if (content == null)
				content = DefaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			// Set info with time.
			SetBody(MessageBoxImage.Error, "{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, content);
		}

		public void SetBodyInfo(string content, params object[] args)
		{
			// Apply format.
			if (content == null)
				content = DefaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			// Set info with time.
			SetBody(MessageBoxImage.Information, content);
		}

		public void SetBody(MessageBoxImage image, string content = null, params object[] args)
		{
			if (content == null)
				content = DefaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			BodyLabel.Text = content;
			// Update body colors.
			switch (image)
			{
				case MessageBoxImage.Error:
					BodyLabel.Foreground = new SolidColorBrush(Colors.DarkRed);
					LeftIcon.Content = Icons.Current[Icons.Icon_Error];
					break;
				case MessageBoxImage.Question:
					BodyLabel.Foreground = new SolidColorBrush(Colors.DarkBlue);
					LeftIcon.Content = Icons.Current[Icons.Icon_Question];
					break;
				case MessageBoxImage.Warning:
					BodyLabel.Foreground = new SolidColorBrush(Colors.DarkOrange);
					LeftIcon.Content = Icons.Current[Icons.Icon_Warning];
					break;
				default:
					BodyLabel.Foreground = SystemColors.ControlTextBrush;
					LeftIcon.Content = Icons.Current[Icons.Icon_Information];
					break;
			}
		}

		#endregion

		#region Task and Rotating Icon

		object _RightIconOriginalContent;
		private readonly object TasksLock = new object();
		public readonly BindingList<object> Tasks = new BindingList<object>();

		private void InitRotation()
		{
			// Initialize rotation
			_RotateTransform = new RotateTransform();
			RightIcon.RenderTransform = _RotateTransform;
			RightIcon.RenderTransformOrigin = new Point(0.5, 0.5);
			RotateTimer = new System.Timers.Timer();
			RotateTimer.Interval = 25;
			RotateTimer.Elapsed += RotateTimer_Elapsed;
		}

		/// <summary>Activate busy spinner.</summary>
		public void AddTask(object name)
		{
			lock (TasksLock)
			{
				if (!Tasks.Contains(name))
					Tasks.Add(name);
				UpdateIcon();
			}
		}

		/// <summary>Deactivate busy spinner if all tasks are gone.</summary>
		public void RemoveTask(object name)
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
				_RightIconOriginalContent = RightIcon.Content;
				RightIcon.Content = Icons.Current[Icons.Icon_ProcessRight];
				RightIcon.RenderTransform = _RotateTransform;
				RotateTimer.Start();
			}
			else
			{
				RotateTimer.Stop();
				RightIcon.RenderTransform = null;
				_RotateTransform.Angle = 0;
				RightIcon.Content = _RightIconOriginalContent;
			}
		}

		RotateTransform _RotateTransform;
		System.Timers.Timer RotateTimer;

		private void RotateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
			RightIcon.Dispatcher.Invoke(() =>
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
			{
				var angle = (_RotateTransform.Angle + 2) % 360;
				_RotateTransform.Angle = angle;
			});
		}

		#endregion

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_Image = null;
		}

	}
}
