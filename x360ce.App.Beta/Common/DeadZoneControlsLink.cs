using System;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;
using Xceed.Wpf.Toolkit;

namespace x360ce.App
{

	/// <summary>
	///  Link 3 controls.
	/// </summary>
	public class DeadZoneControlsLink : IDisposable
	{

		public DeadZoneControlsLink(Slider trackBar, IntegerUpDown numericUpDown, TextBox textBox, int minValue, int maxValue)
		{
			// Slider will be mapped as main settings control.
			_TrackBar = trackBar;
			_TrackBar.Minimum = minValue;
			_TrackBar.Maximum = maxValue;
			_NumericUpDown = numericUpDown;
			_NumericUpDown.Minimum = minValue;
			_NumericUpDown.Maximum = maxValue;
			_NumericUpDown.Value = 0;
			_TextBox = textBox;
			// Update values from TrackBar before events attached.
			UpdateValue();
			_NumericUpDown.ValueChanged += _NumericUpDown_ValueChanged;
			_TrackBar.ValueChanged += _TrackBar_ValueChanged;
		}

		public event EventHandler<EventArgs> ValueChanged;
		Slider _TrackBar;
		IntegerUpDown _NumericUpDown;
		TextBox _TextBox;
		object eventsLock = new object();

		void _TrackBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			UpdateValue();
			var ev = ValueChanged;
			if (ev != null)
				ev(this, new EventArgs());
		}

		public string PercentFormat = "{0:0} % ";

		void UpdateValue()
		{
			lock (eventsLock)
			{
				if (IsDisposing)
					return;
				_NumericUpDown.ValueChanged -= _NumericUpDown_ValueChanged;
				var sourceValue = (float)_TrackBar.Value;
				var value = (int)ConvertHelper.ConvertRangeF((float)_TrackBar.Minimum, (float)_TrackBar.Maximum, (float)_NumericUpDown.Minimum, (float)_NumericUpDown.Maximum, sourceValue);
				if (_NumericUpDown.Value != value)
					_NumericUpDown.Value = value;
				_NumericUpDown.ValueChanged += _NumericUpDown_ValueChanged;
			}
		}

		void _NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			EventHandler<EventArgs> ev;
			lock (eventsLock)
			{
				if (IsDisposing)
					return;
				_TrackBar.ValueChanged -= _TrackBar_ValueChanged;
				var sourceValue = (float)(_NumericUpDown.Value ?? 0);
				var value = (double)ConvertHelper.ConvertRangeF((float)_NumericUpDown.Minimum, (float)_NumericUpDown.Maximum, (float)_TrackBar.Minimum, (float)_TrackBar.Maximum, sourceValue);
				if (_TrackBar.Value != value)
					_TrackBar.Value = value ;
				_TrackBar.ValueChanged += _TrackBar_ValueChanged;
				// Set percent.
				var minPercent = (_NumericUpDown.Minimum ?? 0) < 0 ? -100F : 0f;
				var percent = (double)ConvertHelper.ConvertRangeF((float)_NumericUpDown.Minimum, (float)_NumericUpDown.Maximum, minPercent, 100f, sourceValue);
				var percentRound = Math.Round(percent);
				var percentString = string.Format(PercentFormat, percent);
				// Update percent TextBox.
				if (_TextBox.Text != percentString)
					_TextBox.Text = percentString;
			}
			ev = ValueChanged;
			if (ev != null)
				ev(this, new EventArgs());
		}

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool IsDisposing;

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
				// Free managed resources.
				lock (eventsLock)
				{
					_TrackBar.ValueChanged -= _TrackBar_ValueChanged;
					_NumericUpDown.ValueChanged -= _NumericUpDown_ValueChanged;
					_TrackBar = null;
					_NumericUpDown = null;
					_TextBox = null;
				}
			}
		}

		#endregion

	}
}
