using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public class Recorder : IDisposable
	{

		public Recorder(float horizontalResolution, float verticalResolution)
		{
			markR = new Bitmap(EngineHelper.GetResourceStream("Images.bullet_ball_glass_red_16x16.png"));
			markR.SetResolution(horizontalResolution, verticalResolution);
		}

		Bitmap markR;
		public bool Recording;
		Regex dPadRx = new Regex("(DPad [0-9]+)");
		public bool drawRecordingImage
		{
			get
			{
				// Make image flash: 250 ms - ON, 250 ms - OFF.
				var milliseconds = (int)DateTime.Now.Subtract(DateTime.Now.Date).TotalMilliseconds;
				var show = (milliseconds / 250) % 2 == 0;
				return Recording && show;
			}
		}
		object recordingLock = new object();
		SettingsMapItem _Map;

		public void drawMarkR(PaintEventArgs e, Point position)
		{
			int rW = -markR.Width / 2;
			int rH = -markR.Height / 2;
			e.Graphics.DrawImage(markR, position.X + rW, position.Y + rH);
		}


		public void StartRecording(SettingsMapItem map)
		{
			lock (recordingLock)
			{
				// If recording is already in progress then return.
				if (Recording)
					return;
				_Map = map;
				Recording = true;
				recordingSnapshot = null;
				_Map.Control.ForeColor = SystemColors.GrayText;
				MainForm.Current.StatusTimerLabel.Text = (_Map.PropertyName == SettingName.DPad)
					 ? "Recording - press any D-Pad button on your direct input device. Press ESC to cancel..."
					 : "Recording - press button, move axis or slider on your direct input device. Press ESC to cancel...";
			}
		}

		/// <summary>Initial Direct Input activity state</summary>
		CustomDiState recordingSnapshot;

		/// <summary>
		/// Called when recording is in progress.
		/// </summary>
		/// <param name="state">Current direct input activity.</param>
		/// <returns>True if recording stopped, otherwise false.</returns>
		public bool StopRecording(CustomDiState state = null)
		{
			lock (recordingLock)
			{
				// If recording is not in progress then return false.
				if (!Recording)
				{
					recordingSnapshot = null;
					return false;
				}
				// If recording snapshot was not created yet then...
				else if (recordingSnapshot == null)
				{
					// Make snapshot out of the first state during recording.
					recordingSnapshot = state;
					return false;
				}
				// Must stop recording if null state passed i.e. probably ESC key was pressed.
				var stop = state == null;
				var actions = state == null
					  ? Array.Empty<string>()
					  // Get actions by comparing initial snapshot with current state.
					  : CompareTo(recordingSnapshot, state);
				string action = null;
				// if recording and at least one action was recorded then...
				if (!stop && actions.Length > 0)
				{
					// If this is DPad ComboBox then...
					if (_Map.PropertyName == SettingName.DPad)
					{
						// Get first action suitable for DPad
						var dPadAction = actions.FirstOrDefault(x => dPadRx.IsMatch(x));
						if (dPadAction != null)
						{
							action = dPadRx.Match(dPadAction).Groups[0].Value;
							stop = true;
						}
					}
					else
					{
						// Get first recorded action.
						action = actions[0];
						stop = true;
					}
				}
				// If recording must stop then...
				if (stop)
				{
					var box = ((ComboBox)_Map.Control);
					Recording = false;
					// If stop was initiated before action was recorded then...                    
					if (string.IsNullOrEmpty(action))
					{
						box.Items.Clear();
					}
					else
					{
						// If suitable action was recorded then...
						SettingsManager.Current.SetComboBoxValue(box, action);
						// Save setting and notify if value changed.
						SettingsManager.Current.RaiseSettingsChanged(box);
					}
					box.ForeColor = SystemColors.WindowText;
				}
				return stop;
			}
		}

		/// <summary>
		/// Compare to another state.
		/// </summary>
		public string[] CompareTo(CustomDiState oldState, CustomDiState newState)
		{
			if (oldState == null)
				throw new ArgumentNullException(nameof(oldState));
			if (newState == null)
				throw new ArgumentNullException(nameof(newState));
			var list = new List<string>();
			list.AddRange(CompareAxisAndSliders(oldState.Axis, newState.Axis, "Axis"));
			list.AddRange(CompareAxisAndSliders(oldState.Sliders, newState.Sliders, "Slider"));
			// Compare Buttons
			if (oldState.Buttons.Length == newState.Buttons.Length)
			{
				for (int i = 0; i < oldState.Buttons.Length; i++)
				{
					if (oldState.Buttons[i] != newState.Buttons[i])
					{
						list.Add(string.Format("Button {0}", i + 1));
					}
				}
			};
			// Compare POVs.
			if (oldState.Povs.Length == newState.Povs.Length)
			{
				for (int i = 0; i < oldState.Povs.Length; i++)
				{
					if (oldState.Povs[i] != newState.Povs[i])
					{
						//list.Add(string.Format("DPad {0}", i + 1));
						var v = newState.Povs[0];
						if ((DPadEnum)v == DPadEnum.Up)
							list.Add(string.Format("POV {0} {1}", i + 1, DPadEnum.Up.ToString()));
						if ((DPadEnum)v == DPadEnum.Right)
							list.Add(string.Format("POV {0} {1}", i + 1, DPadEnum.Right.ToString()));
						if ((DPadEnum)v == DPadEnum.Down)
							list.Add(string.Format("POV {0} {1}", i + 1, DPadEnum.Down.ToString()));
						if ((DPadEnum)v == DPadEnum.Left)
							list.Add(string.Format("POV {0} {1}", i + 1, DPadEnum.Left.ToString()));
					}
				}
			};
			return list.ToArray();
		}

		string[] CompareAxisAndSliders(int[] oldValues, int[] newValues, string name)
		{
			// Threshold mark at which action on axis/slider is detected.
			// [------|------------|------]
			//   full      half      full
			// Full/Half depends on where original value was started:
			//     [    0 16383] - Full (16384 steps)
			//     [16384 32767] - Half (16384 steps)
			//     [32768 49151] - Half (16384 steps)
			//     [49152 65535] - Full (16384 steps)
			// Inverted is added if new value is smaller (change is negative).
			var list = new List<string>();
			if (oldValues.Length != newValues.Length)
				return list.ToArray();
			for (int i = 0; i < oldValues.Length; i++)
			{
				// Get difference between states (this object represents old value).
				var oldValue = oldValues[i];
				var diff = newValues[i] - oldValue;
				var prefix = "";
				// If moved more than 10%.
				if (Math.Abs(diff) > (ushort.MaxValue / 10))
				{
					// If value is negative then add "I" prefix.
					if (diff < 0)
						prefix += "I";
					// if starting point is located in the middle then...
					if ((oldValue > (ushort.MaxValue / 4)) && oldValue < (ushort.MaxValue * 3 / 4))
					{
						// Note: Mapping wheel, which is centered in the middle, to the humb will use full axis.
						var pn = _Map.PropertyName;
						var thumb =
							pn == nameof(SettingName.LeftThumbAxisX) ||
							pn == nameof(SettingName.LeftThumbAxisY) ||
							pn == nameof(SettingName.RightThumbAxisX) ||
							pn == nameof(SettingName.RightThumbAxisY);
						// If target property is not thumb then...
						if (!thumb)
						{
							// Allow to add add half prefix.
							prefix += "H";
						}
					}
					list.Add(string.Format("{0}{1} {2}", prefix, name, i + 1));
				}
			}
			return list.ToArray();
		}

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (markR != null)
				{
					markR.Dispose();
				}
			}
		}

		#endregion

	}
}
