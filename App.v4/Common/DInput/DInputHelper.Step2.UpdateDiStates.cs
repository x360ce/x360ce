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
			unchecked((int)0x80004001), // E_NOTIMPL, the device does not implement the effect it was asked for
			unchecked((int)0x80070057), // E_INVALIDARG, the device refuses the effect's settings; ForceFeedbackState does not ask again
			unchecked((int)0x80004005), // E_FAIL, the driver's answer while a device is going away or resetting; a read that gets it acquires the device again on the next poll
		};

		/// <summary>Whether a device call's failure is a device condition, handled by the next poll, rather than a fault to report.</summary>
		public static bool IsBenignDeviceResult(SharpDX.Result result)
		{
			return IsDeviceHoldLost(result) || BenignDeviceResults.Contains(result.Code);
		}

		/// <summary>Whether a device call failed because the device is no longer held, so it has to be held again.</summary>
		/// <remarks>
		/// DIERR_NOTEXCLUSIVEACQUIRED (0x80040205) is the exclusive hold lost to another program; it is taken again on the
		/// next poll. Each is a device condition as well (<see cref="IsBenignDeviceResult"/>). A force failure with any
		/// other error leaves the hold as it is; a failed read lets go of it whatever the error.
		/// </remarks>
		public static bool IsDeviceHoldLost(SharpDX.Result result)
		{
			return result == SharpDX.DirectInput.ResultCode.InputLost
				|| result == SharpDX.DirectInput.ResultCode.NotAcquired
				|| result == SharpDX.DirectInput.ResultCode.Unplugged
				|| result == SharpDX.DirectInput.ResultCode.NotExclusiveAcquired;
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

		/// <summary>How long a device's force feedback that failed twice in a row rests before it is sent again, in milliseconds.</summary>
		/// <remarks>
		/// A device that lost its hold for a moment is held again on the next poll and takes its force there, so one
		/// failure is sent again at once. A force that keeps failing with anything but a refusal, such as a device with
		/// no room for another effect, would otherwise be sent again on every poll, up to a thousand times a second, each
		/// time through an exception. Resting the force alone keeps that to twice a second, and the device is still read
		/// on every poll.
		/// </remarks>
		public const int ForceRetryMs = 500;

		/// <summary>Whether a device's force feedback is resting at <paramref name="now"/> after failing twice in a row.</summary>
		/// <param name="ud">The device.</param>
		/// <param name="now">The <see cref="Environment.TickCount"/> of this poll.</param>
		public static bool IsForceResting(UserDevice ud, int now)
		{
			return ud.ForceFailures > 1 && IsWaiting(ud.ForceRetryAt, now, ForceRetryMs);
		}

		/// <summary>Counts a failure of a device's force feedback and sets when it may be sent again.</summary>
		/// <remarks>Counted apart from the device's reads, which a force failure never rests.</remarks>
		/// <param name="ud">The device.</param>
		/// <param name="now">The <see cref="Environment.TickCount"/> of this poll.</param>
		/// <param name="error">The failure's error code, which the Issues tab names.</param>
		/// <param name="benign">Whether the failure is a device condition rather than a fault (<see cref="IsBenignDeviceResult"/>).</param>
		/// <returns>True for the first fault of a run that is not a device condition, the one worth reporting.</returns>
		public static bool CountForceFailure(UserDevice ud, int now, int error, bool benign)
		{
			ud.ForceRetryAt = unchecked(now + ForceRetryMs);
			ud.ForceFailures++;
			if (benign || ud.ForceFault != 0)
				return false;
			ud.ForceFault = error;
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
			// The devices on the game's ticked rows, found when the routing was built. Found here, the settings and
			// devices lists would be copied on every pass, under the lock the interface holds while it draws a change.
			var userDevices = routing.MappedDevices;
			// Acquire copy of feedbacks for processing.
			var feedbacks = CopyAndClearFeedbacks();
			// Kept per pad, so a device whose force comes from several tabs is driven by what each asked for last.
			var arrived = KeepForces(feedbacks, _lastLargeMotor, _lastSmallMotor) | _forcesDropped;
			_forcesDropped = 0;
			// A new routing can take a tab from a device's force sources, and nothing that tab's game says
			// reaches the device after that. So every pad counts as new once, and every device is told.
			// A device it no longer reads at all is let go of, once, as it is taken in.
			if (TakeRouting(routing))
				arrived |= 0xF;
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
							// Virtual emulation holds only a device on a switched-on tab: one whose tabs are all switched
							// off is read for its tabs' own pages, and left for other programs to use.
							var exclusiveRequired = hasForceFeedback
								&& ((isVirtual && mapped) || forcingFromHere || ud.FFState != null || ud.DeviceEffects == null);
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
								// Acquired: the running totals of axes that report movement may start anywhere.
								ud.DiRelativeRestart = true;
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
								ud.DiRelativeRestart = true;
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
								int relativeMask = 0;
								if (ud.CapType == (int)SharpDX.DirectInput.DeviceType.Mouse)
								{
									CustomDiState.GetMouseAxisMask(dos, device, out axisMask, out relativeMask);
								}
								else
								{
									CustomDiState.GetJoystickAxisMask(dos, device, out axisMask, out actuatorMask, out actuatorCount, out relativeMask);
								}
								ud.DiAxeMask = axisMask;
								// Axes that report how far they moved rather than where they are. The state shown works them out below.
								// A gamepad's sticks are read as they report, whatever its objects declare.
								ud.DiRelativeAxisMask = CustomDiState.TrustedRelativeMask(ud.CapType, relativeMask);
								// Contains information about which axis have force feedback actuator attached.
								ud.DiActuatorMask = actuatorMask;
								ud.DiActuatorCount = actuatorCount;
								// Which of the eight slider slots the device answers to. The mapping list and
								// the input panel offer a slider only when its bit is set here.
								int relativeSliderMask;
								ud.DiSliderMask = CustomDiState.GetJoystickSlidersMask(dos, device, out relativeSliderMask);
								ud.DiRelativeSliderMask = CustomDiState.TrustedRelativeMask(ud.CapType, relativeSliderMask);
							}
							// Reading the effects lets go of the XInput library for a moment, which happens only while no
							// display read holds it. Otherwise they are read on a later poll.
							if (ud.DeviceEffects == null && TryEnterXInputLockWhenFree())
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
									ExitXInputLock();
								}
							}
							// Read, and its objects and effects listed when due, so a run of failures is over. A device
							// whose objects or effects cannot be listed fails as a read does, and rests.
							ud.DiReadFailures = 0;
							// If device support force feedback then... Not while its force rests after failing twice in a
							// row: two field reads and a compare, and nothing asked of the device.
							if (hasForceFeedback && !IsForceResting(ud, Environment.TickCount))
							{
								// A try of its own, after the read: a failure here costs the device neither this poll's
								// state nor, unless the hold was lost, its hold, and is sent again when due rather than
								// on every poll.
								try
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
											// for last. The effects are always made with the same settings, ps. A force that
											// failed is sent again once it is due, as to a device held again.
											byte large, small;
											var forceArrived = MergeForces(route.ForcePads, arrived, _lastLargeMotor, _lastSmallMotor, out large, out small);
											if (forceArrived || held || ud.ForceFailures > 0 || ud.FFState.Changed(ps))
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
											EndSpringRun(ud);
										}
									}
									// Every tab it is on switched off: what was playing is stopped, or the effect, which
									// never ends by itself, goes on at its last strength. A device on no ticked row of
									// this game is not read at all, and ReleaseDroppedDevices stopped it.
									else if (ud.FFState != null)
									{
										step = "ud.FFState.StopDeviceForces(device)";
										ud.FFState.StopDeviceForces(device);
										ud.FFState = null;
										EndSpringRun(ud);
									}
									// The centering spring follows the wheel every poll, and the Auto button's run
									// drives the wheel through the same effect. A device with no force state, or
									// no actuator on an axis, pays nothing here. The clock is read only while a run is
									// under way, and the run is read once, so one started between two reads is never
									// handed time 0.
									if (ud.FFState != null && ud.DiState != null && ud.FFState.SpringAxisIndex >= 0)
									{
										var run = ud.SpringCalibration;
										step = "ud.FFState.UpdateSpring(device)";
										ud.FFState.UpdateSpring(device, ud.DiState.Axis[ud.FFState.SpringAxisIndex], run, run != null ? SpringCalibrationClock.ElapsedMilliseconds : 0);
									}
									// Nothing failed, so a run of failures is over and the next fault is news.
									ud.ForceFailures = 0;
									ud.ForceFault = 0;
								}
								catch (Exception ex)
								{
									var fault = ex as SharpDXException;
									// Reported once a run, as a failed read is; a device condition is not reported.
									if (CountForceFailure(ud, Environment.TickCount, ex.HResult, fault != null && IsBenignDeviceResult(fault.ResultCode)))
									{
										var cx = new DInputException("UpdateDiStates Exception", ex);
										cx.Data.Add("FFInfo", step + " // ud.IsExclusiveMode = " + ud.IsExclusiveMode);
										JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(cx);
									}
									// Held again only when the hold was lost. Any other failure leaves the device held, and read.
									if (fault != null && IsDeviceHoldLost(fault.ResultCode))
										ud.IsExclusiveMode = null;
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
					// Axes and sliders that report movement are worked out from where they were first read. Known since the
					// device's objects were read, so this is two field reads.
					var moving = ud.Device != null && (ud.DiRelativeAxisMask | ud.DiRelativeSliderMask) != 0;
					// Such a device is read into its own reserved state, and the one shown is worked out from it below.
					var read = moving ? ud.DiStateRead ?? (ud.DiStateRead = new CustomDiState()) : newState;
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
					if (moving)
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

		/// <summary>Takes in the routing a pass reads: when it is new, lets go of each device it no longer reads.</summary>
		/// <remarks>
		/// Called on every pass, with a game or without one. A pass with no game reads no device, so the devices the
		/// last game read are let go of on the first pass without it. The routing is taken before anything is let go
		/// of, so a release that fails is not tried again on the next pass. On a pass whose routing is the same, this
		/// is one compare.
		/// </remarks>
		/// <param name="routing">The routing of this pass.</param>
		/// <returns>True when the routing is new.</returns>
		bool TakeRouting(DeviceRouting routing)
		{
			if (routing == _forceRouting)
				return false;
			var was = _forceRouting;
			_forceRouting = routing;
			ReleaseDroppedDevices(was, routing);
			return true;
		}

		/// <summary>Lets go of every device the previous routing read and this one does not: unticked in a tab's list or on the Devices page, taken off its tabs, or of another game.</summary>
		/// <remarks>
		/// No pass visits such a device again, so nothing else would stop what it was playing or give up the
		/// exclusive hold that keeps every other program from it. Its effects are stopped first and its hold given
		/// up after, the order a pass keeps for a device that stops being a force source, and it is left as no
		/// program holds it: not acquired, with no force state or force failure, and its mode unknown, so a later
		/// routing that reads it again takes it from the start. An Auto run under way on it ends.
		///
		/// Every step is made whatever the one before it did, since nothing comes back to make it later: an effect
		/// that will not stop leaves the next effect and the hold still to be given up. The first fault that is not
		/// a device condition is reported, once for the device. Called once a routing change, on the engine thread
		/// that owns the devices. Nothing on a pass; on a change it lists each dropped device's effects once.
		/// </remarks>
		/// <param name="was">The routing the previous pass read, or null before the first pass.</param>
		/// <param name="routing">The routing of this pass.</param>
		void ReleaseDroppedDevices(DeviceRouting was, DeviceRouting routing)
		{
			if (was == null)
				return;
			var before = was.MappedDevices;
			var now = routing.MappedDevices;
			for (var i = 0; i < before.Length; i++)
			{
				var ud = before[i];
				var kept = false;
				for (var n = 0; n < now.Length && !kept; n++)
					kept = ReferenceEquals(now[n], ud);
				if (kept)
					continue;
				var device = ud.Device;
				// A device that is not connected plays nothing and nothing holds it; only what is noted about it goes.
				if (device != null && ud.IsOnline)
				{
					Exception fault = null;
					var faultStep = "";
					if (ud.FFState != null)
					{
						System.Collections.Generic.IList<Effect> effects = null;
						try
						{
							effects = device.CreatedEffects;
						}
						catch (Exception ex)
						{
							KeepReleaseFault(ex, "CreatedEffects (Release)", ref fault, ref faultStep);
						}
						for (var e = 0; effects != null && e < effects.Count; e++)
						{
							try
							{
								effects[e].Stop();
							}
							catch (Exception ex)
							{
								KeepReleaseFault(ex, "Stop effect (Release)", ref fault, ref faultStep);
							}
						}
					}
					try
					{
						device.Unacquire();
					}
					catch (Exception ex)
					{
						KeepReleaseFault(ex, "Unacquire (Release)", ref fault, ref faultStep);
					}
					if (fault != null)
					{
						var cx = new DInputException("ReleaseDroppedDevices Exception", fault);
						cx.Data.Add("FFInfo", faultStep + " // ud.IsExclusiveMode = " + ud.IsExclusiveMode);
						JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(cx);
					}
				}
				ud.FFState = null;
				ud.IsExclusiveMode = null;
				ud.ForceFailures = 0;
				ud.ForceFault = 0;
				EndSpringRun(ud);
			}
		}

		/// <summary>Keeps the first fault of a device's release that is not a device condition, and the step it came from.</summary>
		/// <remarks>A device condition (<see cref="IsBenignDeviceResult"/>) is not a fault, as for a failed read.</remarks>
		static void KeepReleaseFault(Exception ex, string step, ref Exception fault, ref string faultStep)
		{
			var dex = ex as SharpDXException;
			if (fault != null || (dex != null && IsBenignDeviceResult(dex.ResultCode)))
				return;
			fault = ex;
			faultStep = step;
		}

		/// <summary>Ends a centering spring Auto run under way on a device the engine stops driving: unticked, its tab or force feedback switched off, or gone.</summary>
		/// <remarks>
		/// A run moves on only when the engine polls it while it drives the wheel's spring, so with nothing driving it
		/// the page would wait on it for ever, its button showing Wait. The engine thread is the one that polls a run,
		/// so it ends the run here the way the run's own <see cref="SpringCalibration.Cancel"/> ends it on the next
		/// poll: no force, no strength, and the run's own word for a stop. Nothing is made.
		/// </remarks>
		/// <param name="ud">The device.</param>
		static void EndSpringRun(UserDevice ud)
		{
			var run = ud.SpringCalibration;
			if (run == null || run.IsFinished)
				return;
			run.Cancel();
			run.Update(SpringCalibration.Center, 0);
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

		/// <summary>How far one count of movement moves an axis shown: 2,048 counts from the middle reach the end.</summary>
		const int MovementScale = 16;

		/// <summary>How far back from its first reading a moving control starts, in counts: half a travel, so it shows the middle.</summary>
		const int HalfTravel = (ushort.MaxValue + 1) / 2 / MovementScale;

		/// <summary>Works out the state shown for a device whose axes or sliders report movement: each such control shows how far it has moved from where it was first read, starting in the middle, as a wheel's axis would. Every other control is shown as read.</summary>
		/// <param name="ud">The device. <see cref="UserDevice.DiRelativeAxisMask"/> and <see cref="UserDevice.DiRelativeSliderMask"/> say which controls move. Its first reading, and its first after each acquire (<see cref="UserDevice.DiRelativeRestart"/>), is kept half a travel back as <see cref="UserDevice.OrgDiState"/>.</param>
		/// <param name="read">This poll's reading.</param>
		/// <param name="into">The state to show, filled here.</param>
		/// <param name="time">When the reading was taken, in the engine's ticks.</param>
		public static void ToMouseState(UserDevice ud, CustomDiState read, CustomDiState into, long time)
		{
			var axes = ud.DiRelativeAxisMask;
			var sliders = ud.DiRelativeSliderMask;
			// The first reading, or the first since the device was acquired again, when the running totals may start anywhere.
			if (ud.OrgDiState == null || ud.DiRelativeRestart)
			{
				// Made once and filled in place after that. Each control starts half a travel back, so one that has not
				// moved shows the middle, as a stick at rest does. Only the moving ones are read back.
				var origin = ud.OrgDiState ?? (ud.OrgDiState = new CustomDiState());
				for (int a = 0; a < origin.Axis.Length; a++)
					origin.Axis[a] = unchecked(read.Axis[a] - HalfTravel);
				for (int s = 0; s < origin.Sliders.Length; s++)
					origin.Sliders[s] = unchecked(read.Sliders[s] - HalfTravel);
				ud.OrgDiStateTime = time;
				ud.DiRelativeRestart = false;
			}
			// The state shown is reused, so everything in it is set again on every poll.
			Array.Copy(read.Buttons, into.Buttons, into.Buttons.Length);
			Array.Copy(read.Povs, into.Povs, into.Povs.Length);

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
			var origins = ud.OrgDiState;
			for (int a = 0; a < into.Axis.Length; a++)
				into.Axis[a] = (axes & (1 << a)) != 0 ? Travel(origins.Axis, read.Axis, a) : read.Axis[a];
			for (int s = 0; s < into.Sliders.Length; s++)
				into.Sliders[s] = (sliders & (1 << s)) != 0 ? Travel(origins.Sliders, read.Sliders, s) : read.Sliders[s];
		}

		/// <summary>How far a moving axis or slider has gone from its origin, 0 to 65535. Past either end the origin moves with it, so turning back answers at once.</summary>
		static int Travel(int[] origins, int[] reading, int a)
		{
			// Get delta from original state.
			var value = (reading[a] - origins[a]) * MovementScale;
			if (value < ushort.MinValue)
			{
				origins[a] = reading[a];
				return ushort.MinValue;
			}
			if (value > ushort.MaxValue)
			{
				origins[a] = reading[a] - (ushort.MaxValue / MovementScale);
				return ushort.MaxValue;
			}
			return value;
		}


	}

}

