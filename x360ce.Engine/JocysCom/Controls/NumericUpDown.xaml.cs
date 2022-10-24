using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for TemplateNumericUpDown.xaml
	/// </summary>
	public partial class NumericUpDown : UserControl
	{
		public NumericUpDown()
		{
			InitializeComponent();
			_NumericUpDownStyleConverter = (NumericUpDownStyleConverter)Resources["_NumericUpDownStyleConverter"];
			_NumericUpDownStyleConverter.Control = this;
			_NumericUpDownValueConverter = (NumericUpDownValueConverter)Resources["_NumericUpDownValueConverter"];
			_NumericUpDownValueConverter.Control = this;
		}

		NumericUpDownStyleConverter _NumericUpDownStyleConverter;
		NumericUpDownValueConverter _NumericUpDownValueConverter;

		#region Properties

		public static readonly DependencyProperty MinimumProperty =
			DependencyProperty.Register(nameof(Minimum), typeof(decimal?), typeof(NumericUpDown),
			new FrameworkPropertyMetadata((decimal?)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		[DefaultValue(typeof(decimal?), "0")]
		public decimal? Minimum
		{
			get => (decimal?)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register(nameof(Maximum), typeof(decimal?), typeof(NumericUpDown),
			new FrameworkPropertyMetadata((decimal?)100, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		[DefaultValue(typeof(decimal?), "100")]
		public decimal? Maximum
		{
			get => (decimal?)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		// Value

		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(nameof(Value), typeof(decimal?), typeof(NumericUpDown),
			new FrameworkPropertyMetadata((decimal?)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		[DefaultValue(typeof(decimal?), "0")]
		public decimal? Value
		{
			get => (decimal?)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static readonly DependencyProperty SmallChangeProperty =
			DependencyProperty.Register(nameof(SmallChange), typeof(decimal?), typeof(NumericUpDown),
			new FrameworkPropertyMetadata((decimal?)1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		[DefaultValue(typeof(decimal?), "1")]
		public decimal? SmallChange
		{
			get => (decimal?)GetValue(SmallChangeProperty);
			set => SetValue(SmallChangeProperty, value);
		}

		public static readonly DependencyProperty LargeChangeProperty =
			DependencyProperty.Register(nameof(LargeChange), typeof(decimal?), typeof(NumericUpDown),
			new FrameworkPropertyMetadata((decimal?)10, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		[DefaultValue(typeof(decimal?), "10")]
		public decimal? LargeChange
		{
			get => (decimal?)GetValue(LargeChangeProperty);
			set => SetValue(LargeChangeProperty, value);
		}

		#endregion

		public event RoutedPropertyChangedEventHandler<decimal?> ValueChanged;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Name == nameof(Value))
			{
				var e1 = new RoutedPropertyChangedEventArgs<decimal?>((decimal?)e.OldValue, (decimal?)e.NewValue);
				ValueChanged?.Invoke(this, e1);
			}
		}

		private void UpButton_Click(object sender, RoutedEventArgs e)
			=> ChangeValue(SmallChange);

		private void DownButton_Click(object sender, RoutedEventArgs e)
			=> ChangeValue(-SmallChange);

		private void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var box = (TextBox)sender;
			var p = box.Padding;
			box.Padding = new Thickness(p.Left, p.Top, box.ActualHeight + 2.0, p.Bottom);
		}

		private void ChangeValue(decimal? delta, int multiplier = 1)
		{
			var v = Value;
			if (!v.HasValue)
				return;
			if (delta.HasValue)
			{
				v += delta.Value * multiplier;
			}
			// Set put into the range.
			if (Minimum.HasValue && v < Minimum)
				v = Minimum;
			if (Maximum.HasValue && v > Maximum)
				v = Maximum;
			SetValue(v);
		}

		private void TBox_MouseWheel(object sender, MouseWheelEventArgs e)
			=> ChangeValue(SmallChange, e.Delta / Mouse.MouseWheelDeltaForOneLine);

		string oldValue;

		private void TBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var s = TBox.Text;
			if (!NumericUpDownValidationRule.IsValid(s))
			{
				TBox.Text = oldValue;
				return;
			}
			var v = NumericUpDownValidationRule.GetValue(s);
			SetValue(v);
			oldValue = s;
		}

		private void SetValue(string v)
		{
			// Save cursor position from the end.
			var i = TBox.Text.Length - TBox.SelectionStart;
			if (v != TBox.Text)
				TBox.Text = v;
			// Restore cursor position from the end.
			TBox.SelectionStart = System.Math.Max(0, TBox.Text.Length - i);
		}

		private void SetValue(decimal? v)
		{
			// Save cursor position from the end.
			var i = TBox.Text.Length - TBox.SelectionStart;
			if (v != Value)
				Value = v;
			// Restore cursor position from the end.
			TBox.SelectionStart = System.Math.Max(0, TBox.Text.Length - i);
		}

		private void TBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Up:
					ChangeValue(SmallChange);
					e.Handled = true;
					break;
				case Key.Down:
					ChangeValue(-SmallChange);
					e.Handled = true;
					break;
				case Key.PageUp:
					ChangeValue(LargeChange);
					e.Handled = true;
					break;
				case Key.PageDown:
					ChangeValue(-LargeChange);
					e.Handled = true;
					break;
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		public void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return; 
			ValueChanged = null;
			if (_NumericUpDownStyleConverter != null)
			{
				_NumericUpDownStyleConverter.Control = null;
				_NumericUpDownStyleConverter = null;
			}
			if (_NumericUpDownValueConverter != null)
			{
				_NumericUpDownValueConverter.Control = null;
				_NumericUpDownValueConverter = null;
			}
		}


	}
}
