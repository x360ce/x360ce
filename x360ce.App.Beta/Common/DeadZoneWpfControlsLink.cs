using System;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit;

namespace x360ce.App
{

	/// <summary>
	///  Link 3 controls.
	/// </summary>
	public class DeadZoneWpfControlsLink : IDisposable
	{

		public DeadZoneWpfControlsLink(Slider trackBar, IntegerUpDown numericUpDown, TextBox textBox, int maxValue)
		{
			// Slider will be mapped as main settings control.
			_TrackBar = trackBar;
			_NumericUpDown = numericUpDown;
			_NumericUpDown.Maximum = maxValue;
			_TextBox = textBox;
			// Update values from TrackBar before events attached.
			UpdateValue();
			_TrackBar.ValueChanged += _TrackBar_ValueChanged;
			_NumericUpDown.ValueChanged += _NumericUpDown_ValueChanged;
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
			if (ev != null) ev(this, new EventArgs());
		}

		public string PercentFormat = "{0:0} % ";

		void UpdateValue()
		{
			lock (eventsLock)
			{
				if (IsDisposing) return;
				_NumericUpDown.ValueChanged -= _NumericUpDown_ValueChanged;
				var percent = _TrackBar.Value;
				var percentString = string.Format(PercentFormat, percent);
				// Update percent TextBox.
				if (_TextBox.Text != percentString)
					_TextBox.Text = percentString;
				// Update NumericUpDown.
				var value = (decimal)Math.Round((float)percent / 100f * (float)_NumericUpDown.Maximum);
				if (_NumericUpDown.Value != value) _NumericUpDown.Value = (int)value;
				_NumericUpDown.ValueChanged += _NumericUpDown_ValueChanged;
			}
		}

		void _NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			EventHandler<EventArgs> ev;
			lock (eventsLock)
			{
				if (IsDisposing) return;
				var control = (IntegerUpDown)sender;
				_TrackBar.ValueChanged -= _TrackBar_ValueChanged;
				var percent = (int)Math.Round(((float)control.Value / (float)_NumericUpDown.Maximum) * 100f);
				var percentString = string.Format(PercentFormat, percent);
				// Update percent TextBox.
				if (_TextBox.Text != percentString) _TextBox.Text = percentString;
				// Update TrackBar;
				if (_TrackBar.Value != percent) _TrackBar.Value = percent;
				_TrackBar.ValueChanged += _TrackBar_ValueChanged;
				ev = ValueChanged;
			}
			if (ev != null) ev(this, new EventArgs());
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
