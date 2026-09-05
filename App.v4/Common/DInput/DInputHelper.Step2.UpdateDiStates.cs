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
		};

		void UpdateDiStates(DirectInput manager, UserGame game, DeviceDetector detector)
		{
			// Get all mapped user devices.
			var userDevices = SettingsManager.GetMappedDevices(game?.FileName);
			// Acquire copy of feedbacks for processing.
			var feedbacks = CopyAndClearFeedbacks();
			// On to a real controller as well, where a tab asks for it. An Xbox controller offers its motors
			// through XInput and nowhere else, so the force feedback driven below cannot reach one.
			PassForcesThrough(feedbacks);

			for (int i = 0; i < userDevices.Count(); i++)
			{
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				var ud = userDevices[i];
				JoystickState state = null;
				JoystickUpdate[] update = null;
				// Allow if not testing or testing with option enabled.
				var o = SettingsManager.Options;
				var allow = !o.TestEnabled || o.TestGetDInputStates;
				// Note: manager.IsDeviceAttached() use a lot of CPU resources.
				var isAttached = ud != null && ud.IsOnline; // && manager.IsDeviceAttached(ud.InstanceGuid);
				if (isAttached && allow)
				{
					var device = ud.Device;
					if (device != null)
					{
						var exceptionData = new System.Text.StringBuilder();
						try
						{
							if (o.UseDeviceBufferedData && device.Properties.BufferSize == 0)
							{
								// Set BufferSize in order to use buffered data.
								device.Properties.BufferSize = 128;
							}
							var isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
							var hasForceFeedback = device.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);
							// What this device is mapped to, and how. Looked up only for a device that
							// can produce force at all: this runs once per device per poll, at up to a
							// thousand polls a second, and the lookup copies a shared list under a lock.
							// Every device that cannot vibrate would pay for an answer nothing reads.
							Engine.Data.UserSetting setting = null;
							PadSetting ps = null;
							var mapped = false;
							// Force feedback this program drives itself. Windows will not let an effect
							// be built or driven on a device that is not held exclusively, and says so by
							// throwing, so a device being forced from here has to be held that way.
							// Forces already running are stopped before the hold is given up, which is
							// why a device that still has force state keeps it.
							var forcingFromHere = false;
							if (hasForceFeedback)
							{
								setting = SettingsManager.GetSettingByInstance(ud.InstanceGuid);
								mapped = setting != null && setting.MapTo > (int)MapTo.None;
								ps = mapped ? SettingsManager.GetPadSetting(setting.PadSettingChecksum) : null;
								forcingFromHere = ps != null && ps.ForceEnable == "1";
							}
							// Exclusive mode required only if force feedback is available and device is virtual there are no info about effects.
							var exclusiveRequired = hasForceFeedback
								&& (isVirtual || forcingFromHere || ud.FFState != null || ud.DeviceEffects == null);
							if (exclusiveRequired && (!ud.IsExclusiveMode.HasValue || !ud.IsExclusiveMode.Value))
							{
								var flags = CooperativeLevel.Background | CooperativeLevel.Exclusive;
								// Reacquire device in exclusive mode.
								exceptionData.AppendLine("Unacquire (Exclusive)...");
								device.Unacquire();
								exceptionData.AppendLine("SetCooperativeLevel (Exclusive)...");
								device.SetCooperativeLevel(detector.DetectorForm.Handle, flags);
								exceptionData.AppendLine("Acquire (Exclusive)...");
								device.Acquire();
								ud.IsExclusiveMode = true;
							}
							// If current mode must be non exclusive and mode is unknown or exclusive then...
							else if (!exclusiveRequired && (!ud.IsExclusiveMode.HasValue || ud.IsExclusiveMode.Value))
							{
								var flags = CooperativeLevel.Background | CooperativeLevel.NonExclusive;
								// Reacquire device in non exclusive mode so that xinput.dll can control force feedback.
								exceptionData.AppendLine("Unacquire (NonExclusive)...");
								device.Unacquire();
								exceptionData.AppendLine("SetCooperativeLevel (Exclusive)...");
								device.SetCooperativeLevel(detector.DetectorForm.Handle, flags);
								exceptionData.AppendLine("Acquire (Acquire)...");
								device.Acquire();
								ud.IsExclusiveMode = false;
							}
							exceptionData.AppendFormat("device.GetCurrentState() // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
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
							state = device.GetCurrentState();
							// Fill device objects.
							if (ud.DeviceObjects == null)
							{
								exceptionData.AppendFormat("AppHelper.GetDeviceObjects(device) // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
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
								CustomDiState.GetJoystickSlidersMask(dos, device);
							}
							if (ud.DeviceEffects == null)
							{
								exceptionData.AppendFormat("AppHelper.GetDeviceEffects(device) // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
								ud.DeviceEffects = AppHelper.GetDeviceEffects(device);
							}
							// If device support force feedback then...
							if (hasForceFeedback)
							{
								// If device is mapped to controller then...
								if (mapped)
								{
									if (ps != null)
									{
										// If force is enabled then...
										if (ps.ForceEnable == "1")
										{
											if (ud.FFState == null)
												ud.FFState = new Engine.ForceFeedbackState();
											// If force update supplied then...
											var force = feedbacks[(int)setting.MapTo - 1];
											if (force != null || ud.FFState.Changed(ps))
											{
												var v = new Vibration();
												if (force == null)
												{
													v.LeftMotorSpeed = short.MinValue;
													v.RightMotorSpeed = short.MinValue;
												}
												else
												{
													v.LeftMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, force.LargeMotor);
													v.RightMotorSpeed = (short)ConvertHelper.ConvertRange(byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue, force.SmallMotor);
												}
												// For the future: Investigate device states if force feedback is not working. 
												// var st = ud.Device.GetForceFeedbackState();
												//st == SharpDX.DirectInput.ForceFeedbackState
												// ud.Device.SendForceFeedbackCommand(ForceFeedbackCommand.SetActuatorsOn);
												exceptionData.AppendFormat("ud.FFState.SetDeviceForces(device) // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
												ud.FFState.SetDeviceForces(ud, device, ps, v);
											}
										}
										// If force state was created then...
										else if (ud.FFState != null)
										{
											// Stop device forces.
											exceptionData.AppendFormat("ud.FFState.StopDeviceForces(device) // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
											ud.FFState.StopDeviceForces(device);
											ud.FFState = null;
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							var dex = ex as SharpDXException;
							// Device conditions which are not defects: the device was unplugged, is already
							// acquired, refuses the command, or has no force feedback effect downloaded. These
							// occur routinely while switching cooperative level, and every one that is treated
							// as a fault is emailed to support, so the noise buries real reports.
							var benign = dex != null && (
								dex.ResultCode == SharpDX.DirectInput.ResultCode.InputLost ||
								dex.ResultCode == SharpDX.DirectInput.ResultCode.NotAcquired ||
								dex.ResultCode == SharpDX.DirectInput.ResultCode.Unplugged ||
								BenignDeviceResults.Contains(dex.ResultCode.Code));
							if (!benign)
							{
								var cx = new DInputException("UpdateDiStates Exception", ex);
								cx.Data.Add("FFInfo", exceptionData.ToString());
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
						state = TestDeviceHelper.GetCurrentState(ud);
					}
				}
				ud.JoState = state;
				ud.JoUpdate = update;
				if (state != null)
				{
					var newState = new CustomDiState(ud.JoState);
					var newUpdates = update?.Select(x=> new CustomDiUpdate(x)).ToArray();
					// If updates from buffer supplied and old state is available then...
					if (newUpdates != null && newUpdates.Count(x=>x.Type == MapType.Button) > 1 && ud.DiState != null)
					{
						// Analyse if state must be modified.
						for (int b = 0; b < newState.Buttons.Length; b++)
						{
							var oldPresseed = ud.DiState.Buttons[b];
							var newPresseed = newState.Buttons[b];
							// If button state was not changed.
							if (oldPresseed == newPresseed)
							{
								// But buffer contains press then...
								var wasPressed = newUpdates.Count(x => x.Type == MapType.Button && x.Index == b) > 1;
								if (wasPressed)
								{
									// Invert state and give chance for the game to recognize the press.
									newState.Buttons[b] = !newState.Buttons[b];
								}
							}
						}
					}
					var newTime = watch.ElapsedTicks;
					// Remember old state.
					ud.OldDiState = ud.DiState;
					ud.OldDiUpdates = ud.DiUpdates;
					ud.OldDiStateTime = ud.DiStateTime;
					// Update state.
					ud.DiState = newState;
					ud.DiUpdates = newUpdates;
					ud.DiStateTime = newTime;
					// Mouse needs special update.
					if (ud.Device != null && ud.Device.Information.Type == SharpDX.DirectInput.DeviceType.Mouse)
					{
						// If original state is missing then...
						if (ud.OrgDiState == null)
						{
							// Store current values.
							ud.OrgDiState = newState;
							ud.OrgDiStateTime = newTime;
							// Make sure new states have zero values.
							for (int a = 0; a < newState.Axis.Length; a++)
								newState.Axis[a] = -short.MinValue;
							for (int s = 0; s < newState.Sliders.Length; s++)
								newState.Sliders[s] = -short.MinValue;
						}
						var mouseState = new CustomDiState(new JoystickState());
						// Clone button values.
						Array.Copy(newState.Buttons, mouseState.Buttons, mouseState.Buttons.Length);

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
						Calc(ud.OrgDiState.Axis, newState.Axis, mouseState.Axis);
						Calc(ud.OrgDiState.Sliders, newState.Sliders, mouseState.Sliders);
						ud.DiState = mouseState;
					}
				}

			}
		}

		void Calc(int[] orgRange, int[] newState, int[] mouseState)
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

