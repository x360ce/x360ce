using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public class Recorder : IDisposable
	{

		public Recorder(float horizontalResolution = 0, float verticalResolution = 0)
		{
			if (horizontalResolution > 0 && verticalResolution > 0)
			{
				var a = GetType().Assembly;
				markR = JocysCom.ClassLibrary.Helper.FindResource<Bitmap>("Images.bullet_ball_glass_red_16x16.png", a);
				markR.SetResolution(horizontalResolution, verticalResolution);
			}
		}

		Bitmap markR;
		public bool Recording;
		public bool DrawRecordingImage
		{
			get
			{
				// Make image flash: 250 ms - ON, 250 ms - OFF.
				var milliseconds = (int)DateTime.Now.Subtract(RecordingStarted).TotalMilliseconds;
				var show = (milliseconds / 250) % 2 == 0;
				return Recording && show;
			}
		}
		object recordingLock = new object();
		public SettingsMapItem CurrentMap;

		DateTime RecordingStarted = new DateTime();

		public void StartRecording(SettingsMapItem map = null)
		{
			lock (recordingLock)
			{
				// If recording is already in progress then return.
				if (Recording)
					return;
				CurrentMap = map;
				// Set time to now to make sure that DrawRecordingImage always shows image immediately.
				RecordingStarted = DateTime.Now;
				Recording = true;
				recordingSnapshot = null;
				Global._MainWindow.MainPanel.StatusTimerLabel.Content = (CurrentMap?.Code == MapCode.DPad)
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
				// Must stop recording if null state passed i.e. probably ESC key was pressed.
				var stop = state == null;
				string action = null;
				var map = CurrentMap;
				var code = map.Code;
				var box = (ComboBox)map.Control;
				if (state != null)
				{
					// If recording snapshot was not created yet then...
					if (recordingSnapshot == null)
					{
						// Make snapshot out of the first state during recording.
						recordingSnapshot = state;
						return false;
					}
					var actions = state == null
						  ? Array.Empty<string>()
						  // Get actions by comparing initial snapshot with current state.
						  : Recorder.CompareTo(recordingSnapshot, state, map.Code);
					// if recording and at least one action was recorded then...
					if (!stop && actions.Length > 0)
					{
						MapType type;
						int index;
						SettingsConverter.TryParseTextValue(actions[0], out type, out index);
						// If this is Thumb Up, Left, Right, Down and axis was mapped.
						if (SettingsConverter.ThumbDirections.Contains(code) && SettingsConverter.IsAxis(type))
						{
							// Make full axis.
							type = SettingsConverter.ToFull(type);
							var isUp =
								code == MapCode.LeftThumbUp ||
								code == MapCode.RightThumbUp;
							var isLeft =
								code == MapCode.LeftThumbLeft ||
								code == MapCode.RightThumbLeft;
							var isRight =
								code == MapCode.LeftThumbRight ||
								code == MapCode.RightThumbRight;
							var isDown =
								code == MapCode.LeftThumbDown ||
								code == MapCode.RightThumbDown;
							// Invert.
							if (isLeft || isDown)
								type = SettingsConverter.Invert(type);
							var newCode = code;
							var isLeftThumb = SettingsConverter.LeftThumbCodes.Contains(code);
							if (isRight || isLeft)
								newCode = isLeftThumb
									? MapCode.LeftThumbAxisX
									: MapCode.RightThumbAxisX;
							if (isUp || isDown)
								newCode = isLeftThumb
									? MapCode.LeftThumbAxisY
									: MapCode.RightThumbAxisY;
							// Change destination control.
							var rMap = SettingsManager.Current.SettingsMap.First(x => x.MapTo == map.MapTo && x.Code == newCode);
							box = (ComboBox)rMap.Control;
							action = SettingsConverter.ToTextValue(type, index);
							stop = true;
						}
						// If this is DPad ComboBox then...
						else if (code == MapCode.DPad)
						{
							// Get first action suitable for DPad
							Regex dPadRx = new Regex("(POV [0-9]+)");
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
				}
				// If recording must stop then...
				if (stop)
				{
					Recording = false;
					CurrentMap = null;
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
					//box.ForeColor = SystemColors.WindowText;
				}
				return stop;
			}
		}

		/// <summary>
		/// Compare to another state.
		/// </summary>
		public static string[] CompareTo(CustomDiState oldState, CustomDiState newState, MapCode mappingTo)
		{
			if (oldState == null)
				throw new ArgumentNullException(nameof(oldState));
			if (newState == null)
				throw new ArgumentNullException(nameof(newState));
			var list = new List<string>();
			list.AddRange(CompareAxisAndSliders(oldState.Axis, newState.Axis, "Axis", mappingTo));
			list.AddRange(CompareAxisAndSliders(oldState.Sliders, newState.Sliders, "Slider", mappingTo));
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
			if (oldState.POVs.Length == newState.POVs.Length)
			{
				for (int i = 0; i < oldState.POVs.Length; i++)
				{
					if (oldState.POVs[i] != newState.POVs[i])
					{
						//list.Add(string.Format("DPad {0}", i + 1));
						var v = newState.POVs[0];
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

		static string[] CompareAxisAndSliders(int[] oldValues, int[] newValues, string name, MapCode mappingTo)
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
				// If differ by more than 10%.
				if (Math.Abs(diff) > (ushort.MaxValue / 10))
				{
					// If value is negative then add "I" prefix.
					if (diff < 0)
						prefix += "I";
					// if starting point is located in the middle then...
					if ((oldValue > (ushort.MaxValue / 4)) && oldValue < (ushort.MaxValue * 3 / 4))
					{
						// Note: Mapping wheel, which is centered in the middle, to the humb will use full axis.
						var thumb =
							mappingTo == MapCode.LeftThumbAxisX ||
							mappingTo == MapCode.LeftThumbAxisY ||
							mappingTo == MapCode.RightThumbAxisX ||
							mappingTo == MapCode.RightThumbAxisY;
						// If target property is not thumb then...
						if (!thumb)
						{
							// Allow to add half prefix.
							prefix += "H";
						}
					}
					list.Add(string.Format("{0}{1} {2}", prefix, name, i + 1));
				}
			}
			return list.ToArray();
		}

		#region ■ IDisposable

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
