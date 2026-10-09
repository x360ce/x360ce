using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App
{

	/// <summary>
	///  Link 3 controls.
	/// </summary>
	public class DeadZoneControlsLink : IDisposable
	{

		public DeadZoneControlsLink(TrackBar trackBar, NumericUpDown numericUpDown, TextBox textBox)
		{
			_TrackBar = trackBar;
			_NumericUpDown = numericUpDown;
			_TextBox = textBox;
			_TrackBar.ValueChanged += _TrackBar_ValueChanged;
			_NumericUpDown.ValueChanged += _NumericUpDown_ValueChanged;
		}

		public event EventHandler<EventArgs> ValueChanged;
		TrackBar _TrackBar;
		NumericUpDown _NumericUpDown;
		TextBox _TextBox;
		object eventsLock = new object();

		void _TrackBar_ValueChanged(object sender, EventArgs e)
		{
			EventHandler<EventArgs> ev;
			lock (eventsLock)
			{
				if (IsDisposing) return;
				var control = (TrackBar)sender;
				_NumericUpDown.ValueChanged -= new System.EventHandler(_NumericUpDown_ValueChanged);
				var percent = control.Value;
				var percentString = string.Format("{0} % ", percent);
				// Update percent TextBox.
				if (_TextBox.Text != percentString) _TextBox.Text = percentString;
				// Update NumericUpDown.
				var value = (decimal)Math.Round((float)percent / 100f * short.MaxValue);
				if (_NumericUpDown.Value != value) _NumericUpDown.Value = value;
				_NumericUpDown.ValueChanged += new System.EventHandler(_NumericUpDown_ValueChanged);
				ev = ValueChanged;
			}
			if (ev != null) ev(this, new EventArgs());
		}

		void _NumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			EventHandler<EventArgs> ev;
			lock (eventsLock)
			{
				if (IsDisposing) return;
				var control = (NumericUpDown)sender;
				_TrackBar.ValueChanged -= new System.EventHandler(_TrackBar_ValueChanged);
				var percent = (int)Math.Round(((float)control.Value / (float)short.MaxValue) * 100f);
				var percentString = string.Format("{0} % ", percent);
				// Update percent TextBox.
				if (_TextBox.Text != percentString) _TextBox.Text = percentString;
				// Update TrackBar;
				if (_TrackBar.Value != percent) _TrackBar.Value = percent;
				_TrackBar.ValueChanged += new System.EventHandler(_TrackBar_ValueChanged);
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

		// The bulk of the clean-up code is implemented in Dispose(bool)
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
