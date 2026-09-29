using JocysCom.ClassLibrary.IO;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		/// <summary>Device results which mean a state change rather than a fault.</summary>
		/// <remarks>
		/// Raw HRESULTs, because SharpDX does not name the Win32 device errors. Measured from
		/// support reports: these five accounted for most of the mail sent by this handler.
		/// </remarks>
		static readonly int[] BenignDeviceResults = new int[]
		{
			unchecked((int)0x800700AA), // DIERR_ACQUIRED, device already acquired
			unchecked((int)0x8007048F), // ERROR_DEVICE_NOT_CONNECTED
			unchecked((int)0x80070016), // ERROR_BAD_UNIT, device does not recognise the command
			unchecked((int)0x80040203), // DIERR_NOTDOWNLOADED, effect not on the device
			unchecked((int)0x80070005), // E_ACCESSDENIED, another application holds the device
			unchecked((int)0x80040205), // DIERR_NOTEXCLUSIVEACQUIRED, the exclusive hold was lost to another program; it is taken again on the next poll
			unchecked((int)0x80004001), // E_NOTIMPL, the device does not implement the effect it was asked for
			unchecked((int)0x80070057), // E_INVALIDARG, the device refuses the effect's settings; ForceFeedbackState does not ask again
			unchecked((int)0x80004005), // E_FAIL, the driver's answer while a device is going away or resetting; it is acquired again on the next poll
		};

		/// <summary>Whether a device call's failure is a device condition, handled by the next poll, rather than a fault to report.</summary>
		public static bool IsBenignDeviceResult(SharpDX.Result result)
		{
			return result == SharpDX.DirectInput.ResultCode.InputLost
				|| result == SharpDX.DirectInput.ResultCode.NotAcquired
				|| result == SharpDX.DirectInput.ResultCode.Unplugged
				|| BenignDeviceResults.Contains(result.Code);
		}

		/// <summary>How long a device that failed to read twice in a row rests before it is tried again, in milliseconds.</summary>
		/// <remarks>
		/// A device mid-reset is back on the next poll, so one failure is tried again at once. One that keeps
		/// failing (held by another program, or a driver that answers E_FAIL to every acquire) would otherwise
		/// be asked on every poll, up to a thousand times a second, each time through an exception and three
		/// native calls. Resting it keeps that to twice a second.
		/// </remarks>
		public const int DeviceReadRetryMs = 500;

		/// <summary>Whether a device is resting at <paramref name="now"/> after failing to read twice in a row.</summary>
		/// <param name="ud">The device.</param>
		/// <param name="now">The <see cref="Environment.TickCount"/> of this poll.</param>
		public static bool IsDeviceReadResting(UserDevice ud, int now)
		{
			return ud.DiReadFailures > 1 && IsWaiting(ud.DiReadRetryAt, now, DeviceReadRetryMs);
		}

		/// <summary>Counts a failed read of a device and sets when it may be tried again.</summary>
		/// <param name="ud">The device.</param>
		/// <param name="now">The <see cref="Environment.TickCount"/> of this poll.</param>
		/// <param name="benign">Whether the failure is a device condition rather than a fault (<see cref="IsBenignDeviceResult"/>).</param>
		/// <returns>True for the first fault of a run that is not a device condition, the one worth reporting.</returns>
		public static bool CountDeviceReadFailure(UserDevice ud, int now, bool benign)
		{
			ud.DiReadRetryAt = unchecked(now + DeviceReadRetryMs);
			ud.DiReadFailures++;
			if (benign || ud.DiReadFaultReported)
				return false;
			ud.DiReadFaultReported = true;
			return true;
		}

		/// <summary>The time the centering spring's Auto run is measured by: counts up from 0, and never wraps or starts again.</summary>
		/// <remarks>
		/// The run marks a time not yet taken with -1, so it needs a time that is never negative, and one
		/// that never goes back. <see cref="Environment.TickCount"/> is negative from 24.9 to 49.7 days
		/// after Windows starts, and <see cref="watch"/> starts again with the update thread, which a
		/// change of Windows settings restarts in the middle of a run.
		/// </remarks>
		static readonly System.Diagnostics.Stopwatch SpringCalibrationClock = System.Diagnostics.Stopwatch.StartNew();

		/// <param name="routing">Which rows each controller reads and where each device's force comes from, for this pass.</param>
		void UpdateDiStates(DirectInput manager, UserGame game, DeviceDetector detector, DeviceRouting routing)
		{
			// The devices on the game's mapped rows, found when the routing was built. Found here, the settings and
			// devices lists would be copied on every pass, under the lock the interface holds while it draws a change.
			var userDevices = routing.MappedDevices;
			// Acquire copy of feedbacks for processing.
			var feedbacks = CopyAndClearFeedbacks();
			// Kept per pad, so a device whose force comes from several tabs is driven by what each asked for last.
			var arrived = KeepForces(feedbacks, _lastLargeMotor, _lastSmallMotor) | _forcesDropped;
			_forcesDropped = 0;
			// A new routing can take a tab from a device's force sources, and nothing that tab's game says
			// reaches the device after that. So every pad counts as new once, and every device is told.
			if (routing != _forceRouting)
			{
				_forceRouting = routing;
				arrived |= 0xF;
			}
			// On to a real controller as well, where a tab asks for it. An Xbox controller offers its motors
			// through XInput and nowhere else, so the force feedback driven below cannot reach one.
			PassForcesThrough(routing, arrived);

			// Allow if not testing or testing with option enabled. Read once for the pass.
			var o = SettingsManager.Options;
			var allow = !o.TestEnabled || o.TestGetDInputStates;
			for (int i = 0; i < userDevices.Length; i++)
			{
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				var ud = userDevices[i];
				// Only connected devices are read; one that is not keeps what it showed last.
				// Note: manager.IsDeviceAttached() use a lot of CPU resources.
				if (!ud.IsOnline)
					continue;
				JoystickState state = null;
				JoystickUpdate[] update = null;
				if (allow)
				{
					var device = ud.Device;
					// A device resting after failing twice in a row is not asked this poll. It reads as a
					// failed read does: no state.
					if (device != null && !IsDeviceReadResting(ud, Environment.TickCount))
					{
						// The call being made when a device call fails, sent with the report. Set from
						// literals, so a poll that fails nothing builds no text for a report it never sends.
						var step = "";
						try
						{
							if (o.UseDeviceBufferedData && device.Properties.BufferSize == 0)
							{
								// Set BufferSize in order to use buffered data.
								device.Properties.BufferSize = 128;
							}
							// Flags are tested by bit rather than with HasFlag, which boxes both values on every call.
							var isVirtual = (game.EmulationType & (int)EmulationType.Virtual) != 0;
							// Read when the device was opened. Asking the device again is a native call that
							// builds a new answer on every poll.
							var hasForceFeedback = (ud.CapFlags & (int)DeviceFlags.ForceFeedback) != 0;
							// Where this device's force comes from, in the current game. Looked up only for a
							// device that can produce force at all, in the routing handed to this pass: a
							// dictionary read that takes no lock and copies nothing.
							DeviceForce route = null;
							PadSetting ps = null;
							var mapped = false;
							// Force feedback this program drives itself. Windows will not let an effect
							// be built or driven on a device that is not held exclusively, and says so by
							// throwing, so a device being forced from here has to be held that way.
							// Forces already running are stopped before the hold is given up, which is
							// why a device that still has force state keeps it.
							var forcingFromHere = false;
							// Held again this poll. A device let go of, after a failed read, a rest or being opened
							// again, has missed whatever it was told meanwhile, a new routing among it, so it is told
							// its force once more.
							var held = false;
							if (hasForceFeedback)
							{
								mapped = routing.TryGetForce(ud.InstanceGuid, out route);
								ps = mapped ? route.PadSetting : null;
								// Forced when a tab's Force feedback enabled switch is on, as the routing was built.
								forcingFromHere = mapped && route.ForcePads.Length > 0;
							}
							// Exclusive mode required only if force feedback is available and device is virtual there are no info about effects.
							var exclusiveRequired = hasForceFeedback
								&& (isVirtual || forcingFromHere || ud.FFState != null || ud.DeviceEffects == null);
							// If exclusive mode is required and mode is unknown or not exclusive then...
							if (exclusiveRequired && (!ud.IsExclusiveMode.HasValue || !ud.IsExclusiveMode.Value))
							{
								var flags = CooperativeLevel.Background | CooperativeLevel.Exclusive;
								// Reacquire device in exclusive mode.
								step = "Unacquire (Exclusive)";
								device.Unacquire();
								step = "SetCooperativeLevel (Exclusive)";
								device.SetCooperativeLevel(detector.DetectorForm.Handle, flags);
								// Holding a wheel this way turns its own centering off. It is kept on unless this
								// program's spring is to hold the centre, so the wheel's own centering, or the wheel
								// maker's software, stays in charge whenever ours is off. Set here because it can
								// only be set while the device is let go of.
								step = "AutoCenter";
								SetAutoCenter(device, ps == null || ps.ForceSpringEnable != "1");
								step = "Acquire (Exclusive)";
								device.Acquire();
								ud.IsExclusiveMode = true;
								held = true;
							}
							// If current mode must be non exclusive and mode is unknown or exclusive then...
							else if (!exclusiveRequired && (!ud.IsExclusiveMode.HasValue || ud.IsExclusiveMode.Value))
							{
								var flags = CooperativeLevel.Background | CooperativeLevel.NonExclusive;
								// Reacquire device in non exclusive mode so that xinput.dll can control force feedback.
								step = "Unacquire (NonExclusive)";
								device.Unacquire();
								step = "SetCooperativeLevel (NonExclusive)";
								device.SetCooperativeLevel(detector.DetectorForm.Handle, flags);
								step = "Acquire (NonExclusive)";
								device.Acquire();
								ud.IsExclusiveMode = false;
							}
							step = "device.GetCurrentState()";
							// Polling - Retrieves data from polled objects on a DirectInput device.
							// Some devices require pooling (For example original "Xbox Controller S" with XBCD drivers).
							// If the device does not require polling, calling this method has no effect.
							// If a device that requires polling is not polled periodically, no new data is received from the device.
							// Calling this method causes DirectInput to update the device state, generate input
							// events (if buffered data is enabled), and set notification events (if notification is enabled).
							device.Poll();
							if (o.UseDeviceBufferedData && device.Properties.BufferSize > 0)
							{
								// Get buffered data.
								update = device.GetBufferedData();
							}
							// Get device state.
							// Into the device's reserved state, so a poll makes no new one. Named the poll's state only
							// once read, so a read that fails leaves no state, and one from two polls ago is not shown as new.
							var reading = NextJoState(ud);
							device.GetCurrentState(ref reading);
							state = reading;
							// Read, so a run of failures is over.
							ud.DiReadFailures = 0;
							// Fill device objects.
							if (ud.DeviceObjects == null)
							{
								step = "AppHelper.GetDeviceObjects(device)";
								var dos = AppHelper.GetDeviceObjects(device);
								ud.DeviceObjects = dos;
								// Update masks.
								int axisMask = 0;
								int actuatorMask = 0;
								int actuatorCount = 0;
								if (ud.CapType == (int)SharpDX.DirectInput.DeviceType.Mouse)
								{
									CustomDiState.GetMouseAxisMask(dos, device, out axisMask);
								}
								else
								{
									CustomDiState.GetJoystickAxisMask(dos, device, out axisMask, out actuatorMask, out actuatorCount);
								}
								ud.DiAxeMask = axisMask;
								// Contains information about which axis have force feedback actuator attached.
								ud.DiActuatorMask = actuatorMask;
								ud.DiActuatorCount = actuatorCount;
								// Which of the eight slider slots the device answers to. The mapping list and
								// the input panel offer a slider only when its bit is set here.
								ud.DiSliderMask = CustomDiState.GetJoystickSlidersMask(dos, device);
							}
							// Reading the effects lets go of the XInput library for a moment, which happens only while no
							// display read holds it. Otherwise they are read on a later poll.
							if (ud.DeviceEffects == null && System.Threading.Monitor.TryEnter(Controller.XInputLock))
							{
								try
								{
									step = "AppHelper.GetDeviceEffects(device)";
									ud.DeviceEffects = AppHelper.GetDeviceEffects(device);
									// The Direct Input tab draws the objects and effects once, when told the device
									// changed. Told nothing, it kept what it drew before they were read: a device
									// list read on a worker finishes after the tab has first drawn, so the lists
									// stayed empty.
									ud.DeviceChanged = true;
								}
								finally
								{
									System.Threading.Monitor.Exit(Controller.XInputLock);
								}
							}
							// If device support force feedback then...
							if (hasForceFeedback)
							{
								// If device is mapped to controller then...
								if (mapped)
								{
									// If force is enabled then... On one tab or several, whose settings are then
									// always stored, so ps is set.
									if (route.ForcePads.Length > 0)
									{
										if (ud.FFState == null)
											ud.FFState = new Engine.ForceFeedbackState();
										// The force of every tab the device's force comes from: the strongest of each
										// motor, a tab whose game said nothing new this pass counting with what it asked
										// for last. The effects are always made with the same settings, ps.
										byte large, small;
										var forceArrived = MergeForces(route.ForcePads, arrived, _lastLargeMotor, _lastSmallMotor, out large, out small);
										if (forceArrived || held || ud.FFState.Changed(ps))
										{
											var v = new Vibration();
											v.LeftMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, large);
											v.RightMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, small);
											// For the future: Investigate device states if force feedback is not working.
											// var st = ud.Device.GetForceFeedbackState();
											//st == SharpDX.DirectInput.ForceFeedbackState
											// ud.Device.SendForceFeedbackCommand(ForceFeedbackCommand.SetActuatorsOn);
											step = "ud.FFState.SetDeviceForces(device)";
											ud.FFState.SetDeviceForces(ud, device, ps, v);
										}
									}
									// Force feedback off on every switched-on tab, or no stored settings for any of them.
									// If force state was created then...
									else if (ud.FFState != null)
									{
										// Stop device forces.
										step = "ud.FFState.StopDeviceForces(device)";
										ud.FFState.StopDeviceForces(device);
										ud.FFState = null;
									}
								}
								// On no routed row of this game, every row of it switched off, or every tab it is on
								// switched off: what was playing is stopped, or the effect, which never ends by
								// itself, goes on at its last strength.
								else if (ud.FFState != null)
								{
									step = "ud.FFState.StopDeviceForces(device)";
									ud.FFState.StopDeviceForces(device);
									ud.FFState = null;
								}
								// The centering spring follows the wheel every poll, and the Auto button's run
								// drives the wheel through the same effect. A device with no force state, or
								// no actuator on an axis, pays nothing here. The clock is read only while a run is
								// under way, and the run is read once, so one started between two reads is never
								// handed time 0.
								if (ud.FFState != null && ud.DiState != null && ud.FFState.SpringAxisIndex >= 0)
								{
									var run = ud.SpringCalibration;
									ud.FFState.UpdateSpring(device, ud.DiState.Axis[ud.FFState.SpringAxisIndex], run, run != null ? SpringCalibrationClock.ElapsedMilliseconds : 0);
								}
							}
							// A poll that failed nothing ends the run's report, so the next fault is reported.
							ud.DiReadFaultReported = false;
						}
						catch (Exception ex)
						{
							var dex = ex as SharpDXException;
							// Device conditions which are not defects: the device was unplugged, is already
							// acquired, refuses the command, or has no force feedback effect downloaded. These
							// occur routinely while switching cooperative level, and every one that is treated
							// as a fault is emailed to support, so the noise buries real reports.
							var benign = dex != null && IsBenignDeviceResult(dex.ResultCode);
							// Every failure counts towards resting the device. A fault is reported once a run,
							// not once a poll.
							if (CountDeviceReadFailure(ud, Environment.TickCount, benign))
							{
								var cx = new DInputException("UpdateDiStates Exception", ex);
								cx.Data.Add("FFInfo", step + " // ud.IsExclusiveMode = " + ud.IsExclusiveMode);
								JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(cx);
							}
							ud.IsExclusiveMode = null;
						}
					}
					// If this is test device then...
					else if (TestDeviceHelper.ProductGuid.Equals(ud.ProductGuid))
					{
						// Fill device objects.
						if (ud.DeviceObjects == null)
						{
							var dos = TestDeviceHelper.GetDeviceObjects();
							ud.DeviceObjects = dos;
							// Update masks.
							ud.DiAxeMask = 0x1 | 0x2 | 0x4 | 0x8;
							ud.DiSliderMask = 0;
						}
						if (ud.DeviceEffects == null)
							ud.DeviceEffects = new DeviceEffectItem[0];
						state = NextJoState(ud);
						TestDeviceHelper.GetCurrentState(ud, state);
					}
				}
				ud.JoState = state;
				ud.JoUpdate = update;
				if (state != null)
				{
					// Filled into the state shown before the current one, rather than a new one each poll. A state is not
					// written until one whole poll after it stops being shown.
					var newState = ud.OldDiState ?? new CustomDiState();
					// Mouse needs special update.
					// Read when the device was opened. The device's own answer is a native call that builds new text on every poll.
					var isMouse = ud.Device != null && ud.IsMouse;
					// A mouse is read into its own reserved state, and the one shown is worked out from it below.
					var read = isMouse ? ud.DiStateRead ?? (ud.DiStateRead = new CustomDiState()) : newState;
					read.Load(ud.JoState);
					var newUpdates = update?.Select(x=> new CustomDiUpdate(x)).ToArray();
					// If updates from buffer supplied and old state is available then...
					if (newUpdates != null && newUpdates.Count(x=>x.Type == MapType.Button) > 1 && ud.DiState != null)
					{
						// Analyse if state must be modified.
						for (int b = 0; b < read.Buttons.Length; b++)
						{
							var oldPresseed = ud.DiState.Buttons[b];
							var newPresseed = read.Buttons[b];
							// If button state was not changed.
							if (oldPresseed == newPresseed)
							{
								// But buffer contains press then...
								var wasPressed = newUpdates.Count(x => x.Type == MapType.Button && x.Index == b) > 1;
								if (wasPressed)
								{
									// Invert state and give chance for the game to recognize the press.
									read.Buttons[b] = !read.Buttons[b];
								}
							}
						}
					}
					var newTime = watch.ElapsedTicks;
					if (isMouse)
						ToMouseState(ud, read, newState, newTime);
					// Remember old state.
					ud.OldDiState = ud.DiState;
					ud.OldDiUpdates = ud.DiUpdates;
					ud.OldDiStateTime = ud.DiStateTime;
					// Update state.
					ud.DiState = newState;
					ud.DiUpdates = newUpdates;
					ud.DiStateTime = newTime;
				}

			}
		}

		/// <summary>Sets DirectInput's autocenter, which can only be set while the device is not held. A device with no centre of its own is left as it is.</summary>
		static void SetAutoCenter(Joystick device, bool on)
		{
			try
			{
				device.Properties.AutoCenter = on;
			}
			catch (SharpDXException)
			{
				// Not every device has a centre of its own to switch.
			}
		}

		/// <summary>The device's reserved state the next poll reads into: its two, in turn, so a state is not written until one whole poll after it stops being shown.</summary>
		static JoystickState NextJoState(UserDevice ud)
		{
			var states = ud.JoStates;
			if (states == null)
				ud.JoStates = states = new JoystickState[2];
			var turn = ud.JoStateTurn;
			ud.JoStateTurn = turn ^ 1;
			return states[turn] ?? (states[turn] = new JoystickState());
		}

		/// <summary>Works out the state a mouse shows from its reading: how far it has moved from where it was first read, as a wheel's axis would be.</summary>
		/// <param name="ud">The mouse. Its first reading is kept, centred, as <see cref="UserDevice.OrgDiState"/>.</param>
		/// <param name="read">This poll's reading.</param>
		/// <param name="into">The state to show, filled here.</param>
		/// <param name="time">When the reading was taken, in the engine's ticks.</param>
		public static void ToMouseState(UserDevice ud, CustomDiState read, CustomDiState into, long time)
		{
			// If original state is missing then...
			if (ud.OrgDiState == null)
			{
				// Make sure new states have zero values.
				for (int a = 0; a < read.Axis.Length; a++)
					read.Axis[a] = -short.MinValue;
				for (int s = 0; s < read.Sliders.Length; s++)
					read.Sliders[s] = -short.MinValue;
				// Store current values: a copy, because the reading is filled again on the next poll.
				ud.OrgDiState = read.Clone();
				ud.OrgDiStateTime = time;
			}
			// The state shown is reused, so its four hats are set again on every poll: a mouse has none, and they read 0.
			Array.Clear(into.Povs, 0, into.Povs.Length);
			// Clone button values.
			Array.Copy(read.Buttons, into.Buttons, into.Buttons.Length);

			//	//--------------------------------------------------------
			//	// Map mouse acceleration to axis position. Good for FPS control.
			//	//--------------------------------------------------------

			//	// This parts needs to be worked on.
			//	//var ticks = (int)(newTime - ud.DiStateTime);
			//	// Update axis with delta.
			//	//for (int a = 0; a < newState.Axis.Length; a++)
			//	//	mouseState.Axis[a] = ticks * (newState.Axis[a] - ud.OldDiState.Axis[a]) - short.MinValue;
			//	// Update sliders with delta.
			//	//for (int s = 0; s < newState.Sliders.Length; s++)
			//	//	mouseState.Sliders[s] = ticks * (newState.Sliders[s] - ud.OldDiState.Sliders[s]) - short.MinValue;

			//--------------------------------------------------------
			// Map mouse position to axis position. Good for car wheel controls.
			//--------------------------------------------------------
			Calc(ud.OrgDiState.Axis, read.Axis, into.Axis);
			Calc(ud.OrgDiState.Sliders, read.Sliders, into.Sliders);
		}

		static void Calc(int[] orgRange, int[] newState, int[] mouseState)
		{
			var sensitivity = 16;
			for (int a = 0; a < newState.Length; a++)
			{
				// Get delta from original state.
				var value = (newState[a] - orgRange[a]) * sensitivity;
				if (value < ushort.MinValue)
				{
					value = ushort.MinValue;
					orgRange[a] = newState[a];
				}
				if (value > ushort.MaxValue)
				{
					value = ushort.MaxValue;
					orgRange[a] = newState[a] - (ushort.MaxValue / sensitivity);
				}
				mouseState[a] = value;
			}
		}


	}

}

