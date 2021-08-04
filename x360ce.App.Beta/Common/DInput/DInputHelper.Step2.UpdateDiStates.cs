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

		void UpdateDiStates(UserGame game, DeviceDetector detector)
		{
			// Get all mapped user devices.
			var userDevices = SettingsManager.GetMappedDevices(game?.FileName);
			// Acquire copy of feedbacks for processing.
			var feedbacks = CopyAndClearFeedbacks();

			for (int i = 0; i < userDevices.Count(); i++)
			{
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				var ud = userDevices[i];
				//JoystickState state = null;
				CustomDiState newState = null;
				CustomDiUpdate[] newUpdates = null;
				// Allow if not testing or testing with option enabled.
				var o = SettingsManager.Options;
				var allow = !o.TestEnabled || o.TestGetDInputStates;
				// Note: manager.IsDeviceAttached() use a lot of CPU resources.
				var isAttached = ud != null && ud.IsOnline; // && manager.IsDeviceAttached(ud.InstanceGuid);
				if (isAttached && allow)
				{
					var useData = false;
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
								useData = true;
							}
							var isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
							var hasForceFeedback = device.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);
							// Exclusive mode required only if force feedback is available and device is virtual there are no info about effects.
							var exclusiveRequired = hasForceFeedback && (isVirtual || ud.DeviceEffects == null);
							// If exclusive mode is required and mode is unknown or not exclusive then...
							if (exclusiveRequired && (!ud.IsExclusiveMode.HasValue || !ud.IsExclusiveMode.Value))
							{
								var flags = CooperativeLevel.Background | CooperativeLevel.Exclusive;
								// Reacquire device in exclusive mode.
								exceptionData.AppendLine("UnAcquire (Exclusive)...");
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
								exceptionData.AppendLine("UnAcquire (NonExclusive)...");
								device.Unacquire();
								exceptionData.AppendLine("SetCooperativeLevel (Exclusive)...");
								device.SetCooperativeLevel(detector.DetectorForm.Handle, flags);
								exceptionData.AppendLine("Acquire (Acquire)...");
								device.Acquire();
								ud.IsExclusiveMode = false;
							}
							exceptionData.AppendFormat("device.GetCurrentState() // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
							// Polling - Retrieves data from polled objects on a DirectInput device.
							// Some devices require pooling (For example original "XBOX Controller S" with XBCD drivers).
							// If the device does not require polling, calling this method has no effect.
							// If a device that requires polling is not polled periodically, no new data is received from the device.
							// Calling this method causes DirectInput to update the device state, generate input
							// events (if buffered data is enabled), and set notification events (if notification is enabled).
							device.Poll();
							// Get device states as buffered data.
							if (device is Mouse mDevice)
							{
								if (useData)
								{
									var data = mDevice.GetBufferedData();
									newUpdates = data?.Select(x => new CustomDiUpdate(x)).ToArray();

								}
								var state = mDevice.GetCurrentState();
								newState = new CustomDiState(state);
								ud.DeviceState = state;
							}
							else if (device is Keyboard kDevice)
							{
								if (useData)
								{
									var data = kDevice.GetBufferedData();
									newUpdates = data?.Select(x => new CustomDiUpdate(x)).ToArray();
								}
								var state = kDevice.GetCurrentState();
								newState = new CustomDiState(state);
								ud.DeviceState = state;
							}
							else if (device is Joystick jDevice)
							{
								if (useData)
								{
									var data = jDevice.GetBufferedData();
									newUpdates = data?.Select(x => new CustomDiUpdate(x)).ToArray();
								}
								var state = jDevice.GetCurrentState();
								newState = new CustomDiState(state);
								ud.DeviceState = state;
							}
							else
							{
								throw new Exception(string.Format("Unknown device: {0}", device));
							}
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
								if (device is Mouse mDevice2)
								{
									CustomDiState.GetMouseAxisMask(dos, mDevice2, out axisMask);
								}
								else if (device is Joystick jDevice)
								{
									CustomDiState.GetJoystickAxisMask(dos, jDevice, out axisMask, out actuatorMask, out actuatorCount);
									//CustomDiState.GetJoystickSlidersMask(dos, (Joystick) device, out slidersMask);
								}
								ud.DiAxeMask = axisMask;
								// Contains information about which axis have force feedback actuator attached.
								ud.DiActuatorMask = actuatorMask;
								ud.DiActuatorCount = actuatorCount;
							}
							if (ud.DeviceEffects == null)
							{
								exceptionData.AppendFormat("AppHelper.GetDeviceEffects(device) // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
								ud.DeviceEffects = AppHelper.GetDeviceEffects(device);
							}
							// If device support force feedback then...
							if (hasForceFeedback)
							{
								// Get setting related to user device.
								var setting = SettingsManager.UserSettings.ItemsToArraySyncronized()
									.FirstOrDefault(x => x.InstanceGuid == ud.InstanceGuid);
								// If device is mapped to controller then...
								if (setting != null && setting.MapTo > (int)MapTo.None)
								{
									// Get pad setting attached to device.
									var ps = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
									if (ps != null)
									{
										// If force is enabled then...
										if (ps.ForceEnable == "1")
										{
											if (ud.FFState == null)
												ud.FFState = new Engine.ForceFeedbackState();
											// If force update supplied then...
											var force = feedbacks[setting.MapTo - 1];
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
							if (dex != null && dex.ResultCode == SharpDX.DirectInput.ResultCode.InputLost)
							{
								// Ignore error.
							}
							else if (dex != null && dex.ResultCode == SharpDX.DirectInput.ResultCode.NotAcquired)
							{
								// Ignore error
							}
							else if (dex != null && dex.ResultCode == SharpDX.DirectInput.ResultCode.Unplugged)
							{
								// Ignore error
							}
							else
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
						var state = TestDeviceHelper.GetCurrentState(ud);
						newState = new CustomDiState(state);
						ud.DeviceState = state;
					}
				}
				if (newState != null)
				{
					// If updates from buffer supplied and old state is available then...
					if (newUpdates != null && newUpdates.Count(x => x.Type == MapType.Button) > 1 && ud.DiState != null)
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

