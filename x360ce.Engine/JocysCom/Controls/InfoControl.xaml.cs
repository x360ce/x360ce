﻿using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

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
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Get assemblies which will be used to select default (fists) and search for resources.
			var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			//var company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute)))?.Company;
			var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)))?.Product;
			var description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute)))?.Description;
			DefaultHead = product;
			DefaultBody = description;
			Reset();
			CreateBusyIconAnimation();
			// InitRotation();
			HelpProvider.OnMouseEnter += HelpProvider_OnMouseEnter;
			HelpProvider.OnMouseLeave += HelpProvider_OnMouseLeave;

		}

		public DoubleAnimationUsingKeyFrames animationBusyIcon = new DoubleAnimationUsingKeyFrames();
		public Storyboard storyboardBusyIcon = new Storyboard();
		private void CreateBusyIconAnimation()
		{
			// Animation properties.
			Storyboard.SetTarget(animationBusyIcon, BusyIcon);
			Storyboard.SetTargetProperty(animationBusyIcon, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
			animationBusyIcon.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
			animationBusyIcon.KeyFrames.Add(new LinearDoubleKeyFrame(360, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(6))));
			// Storyboard properties.
			storyboardBusyIcon.RepeatBehavior = RepeatBehavior.Forever;
			storyboardBusyIcon.Children.Add(animationBusyIcon);
			storyboardBusyIcon.Begin();
		}

		private void HelpProvider_OnMouseEnter(object sender, EventArgs e)
		{
			var control = (Control)sender;
			var head = HelpProvider.GetHelpHead(control);
			var body = HelpProvider.GetHelpBody(control, BodyMaxLength, true);
			var image = HelpProvider.GetHelpImage(control);
			SetHead(head);
			SetBody(image, body);
		}
		public int BodyMaxLength { get; set; } = int.MaxValue;

		private void HelpProvider_OnMouseLeave(object sender, EventArgs e)
		{
			Reset();
		}

		public InfoHelpProvider HelpProvider { get; set; } = new InfoHelpProvider();

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

		public void Reset()
		{
			SetHead(DefaultHead);
			SetBodyInfo(DefaultBody);
		}

		public void SetTitle(string format, params object[] args)
		{
			var win = System.Windows.Window.GetWindow(this);
			if (win is null)
				return;
			win.Title = (args.Length == 0)
				? format
				: string.Format(format, args);
		}

		public void SetHead(string format, params object[] args)
		{
			// Apply format.
			if (format is null)
				format = DefaultHead;
			else if (args.Length > 0)
				format = string.Format(format, args);
			if (HeadLabel.Content as string != format)
				HeadLabel.Content = format;
		}

		public void SetBodyError(string content, params object[] args)
		{
			// Apply format.
			if (content is null)
				content = DefaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			// Set info with time.
			SetBody(MessageBoxImage.Error, "{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, content);
		}

		public void SetBodyInfo(string content, params object[] args)
		{
			// Apply format.
			if (content is null)
				content = DefaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			// Set info with time.
			SetBody(MessageBoxImage.Information, content);
		}

		public async void SetWithTimeout(MessageBoxImage image, string content = null, params object[] args)
		{
			SetBody(image, content, args);
			var bodyText = BodyLabel.Text;
			// The average minimal reading speed for adults is 16 characters per second.
			// Use reading speed for adults as 14 characters per second.
			// Add 4 extra seconds for realization and focus.
			var waitSeconds = 4 + bodyText.Length / 14.0;
			// Task code which waits for waitSeconds and executes code below.
			await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
			if (bodyText == BodyLabel.Text)
				Reset();
		}


		public void SetBody(MessageBoxImage image, string content = null, params object[] args)
		{
			if (content is null)
				content = DefaultBody;
			else if (args.Length > 0)
				content = string.Format(content, args);
			BodyLabel.Text = content;
			// Set body color and icon.
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

		private readonly object TasksLock = new object();
		public readonly BindingList<object> Tasks = new BindingList<object>();

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
			BusyCount.Content = Tasks.Count > 1 ? $"{Tasks.Count}" : "";
			if (Tasks.Count > 0)
			{
				BusyIcon.Visibility = Visibility.Visible;
				RightIcon.Visibility = Visibility.Hidden;
				storyboardBusyIcon.Begin();
			}
			else
			{
				storyboardBusyIcon.Pause();
				BusyIcon.Visibility = Visibility.Hidden;
				RightIcon.Visibility = Visibility.Visible;
			}
		}

		#endregion

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			_Image = null;
		}

	}
}
