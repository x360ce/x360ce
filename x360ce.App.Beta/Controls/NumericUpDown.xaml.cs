using JocysCom.ClassLibrary.Controls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for TemplateNumericUpDown.xaml
	/// </summary>
	public partial class NumericUpDown : UserControl
	{
		public NumericUpDown()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			TextBox.Text = Value.ToString();
		}

		#region Properties

		public static readonly DependencyProperty MinimumProperty =
			DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericUpDown));

		public double Minimum
		{
			get { return (double)GetValue(MinimumProperty); }
			set { SetValue(MinimumProperty, value); }
		}

		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericUpDown),	new FrameworkPropertyMetadata(100.0));

		public double Maximum
		{
			get { return (double)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}

		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(double?), typeof(NumericUpDown),
			new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		public double Value
		{
			get { return (double)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		public static readonly DependencyProperty SmallChangeProperty =
			DependencyProperty.Register("SmallChange", typeof(double), typeof(NumericUpDown),
			new FrameworkPropertyMetadata(1.0));

		public double SmallChange
		{
			get { return (double)GetValue(SmallChangeProperty); }
			set { SetValue(SmallChangeProperty, value); }
		}

		public static readonly DependencyProperty LargeChangeProperty =
			DependencyProperty.Register("LargeChange", typeof(double), typeof(NumericUpDown),
			new FrameworkPropertyMetadata(10.0));

		public double LargeChange
		{
			get { return (double)GetValue(LargeChangeProperty); }
			set { SetValue(LargeChangeProperty, value); }
		}

		#endregion

		public event RoutedPropertyChangedEventHandler<object> ValueChanged;

		private void CmdUp_Click(object sender, RoutedEventArgs e)
		{
			Value++;
		}

		private void CmdDown_Click(object sender, RoutedEventArgs e)
		{
			Value--;
		}

		private void TxtNum_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextBox == null)
				return;
			int _value;
			if (!int.TryParse(TextBox.Text, out _value))
			{
				TextBox.Text = _value.ToString();
				Value = _value;
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			ValueChanged = null;
		}

		private void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var box = (TextBox)sender;
			var p = box.Padding;
			box.Padding = new Thickness(p.Left, p.Top, box.ActualHeight + 2.0, p.Bottom);
		}

	}

	public class NumericUpDownConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
			return v < 0 ? value : v;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}


}
