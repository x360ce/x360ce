using System;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;
using JocysCom.ClassLibrary.Controls;

namespace x360ce.App
{

	/// <summary>
	///  Link 3 controls.
	/// </summary>
	public class TrackBarUpDownTextBoxLink : IDisposable
	{

		public TrackBarUpDownTextBoxLink(Slider trackBar, NumericUpDown numericUpDown, TextBox textBox, int minValue, int maxValue)
		{
			eventsLock = new object();
			PercentFormat = "{0:0} % ";
			// Slider will be mapped as main settings control.
			_TrackBar = trackBar;
			_TrackBar.Minimum = minValue;
			_TrackBar.Maximum = maxValue;
			_NumericUpDown = numericUpDown;
			_NumericUpDown.Minimum = minValue;
			_NumericUpDown.Maximum = maxValue;
			_TextBox = textBox;
			_NumericUpDown.Value = 0;
			// Update values from TrackBar before events attached.
			UpdateValue();
			_NumericUpDown.ValueChanged += _NumericUpDown_ValueChanged;
			_TrackBar.ValueChanged += _TrackBar_ValueChanged;

		}

		public event EventHandler<EventArgs> ValueChanged;
		private Slider _TrackBar;
		private NumericUpDown _NumericUpDown;
		private TextBox _TextBox;
		private object eventsLock;

		private void _TrackBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (IsDisposing)
				return;
			UpdateValue();
			ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		public string PercentFormat;

		private void UpdateValue()
		{
			lock (eventsLock)
			{
				if (IsDisposing)
					return;
				_NumericUpDown.ValueChanged -= _NumericUpDown_ValueChanged;
				var sourceValue = (float)_TrackBar.Value;
				var value = (int)ConvertHelper.ConvertRangeF(sourceValue, (float)_TrackBar.Minimum, (float)_TrackBar.Maximum, (float)_NumericUpDown.Minimum, (float)_NumericUpDown.Maximum);
				if (_NumericUpDown.Value != value)
					_NumericUpDown.Value = value;
				_NumericUpDown.ValueChanged += _NumericUpDown_ValueChanged;
			}
		}

		private void _NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal?> e)
		{
			lock (eventsLock)
			{
				if (IsDisposing)
					return;
				_TrackBar.ValueChanged -= _TrackBar_ValueChanged;
				var sourceValue = _NumericUpDown.Value;
				var value = (double)ConvertHelper.ConvertRangeF((float)sourceValue, (float)_NumericUpDown.Minimum, (float)_NumericUpDown.Maximum, (float)_TrackBar.Minimum, (float)_TrackBar.Maximum);
				if (_TrackBar.Value != value)
					_TrackBar.Value = value;
				_TrackBar.ValueChanged += _TrackBar_ValueChanged;
				// Set percent.
				var minPercent = _NumericUpDown.Minimum < 0 ? -100F : 0f;
				var percent = (double)ConvertHelper.ConvertRangeF((float)sourceValue, (float)_NumericUpDown.Minimum, (float)_NumericUpDown.Maximum, minPercent, 100f);
				var percentRound = Math.Round(percent);
				var percentString = string.Format(PercentFormat, percent);
				// Update percent TextBox.
				if (_TextBox.Text != percentString)
					_TextBox.Text = percentString;
			}
			ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		#region ■ IDisposable

		// To detect redundant calls
		private bool _disposed = false;

		// Public implementation of Dispose pattern callable by consumers.
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~TrackBarUpDownTextBoxLink()
			=> Dispose(false);

		private bool IsDisposing;

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;
			if (disposing)
			{
				// Free managed resources.
				IsDisposing = true;
				_TrackBar.ValueChanged -= _TrackBar_ValueChanged;
				_NumericUpDown.ValueChanged -= _NumericUpDown_ValueChanged;
				_TrackBar = null;
				_NumericUpDown = null;
				_TextBox = null;
				ValueChanged = null;
				eventsLock = null;
				PercentFormat = null;
			}
			// Free unmanaged resources.
			_disposed = true;
		}

		#endregion

	}
}
