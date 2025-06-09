using JocysCom.ClassLibrary.IO;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		#region Device Acquisition Methods

		private void DeviceExclusiveMode(UserDevice ud, DeviceDetector detector, Device device, StringBuilder exceptionData, CooperativeLevel cooperationLevel)
		{
			string Non = cooperationLevel == CooperativeLevel.Exclusive ? "" : "Non";
			exceptionData.AppendLine($"UnAcquire ({Non}Exclusive)...");
			device.Unacquire();
			exceptionData.AppendLine($"SetCooperativeLevel ({Non}Exclusive)...");
			device.SetCooperativeLevel(detector.DetectorForm.Handle, CooperativeLevel.Background | cooperationLevel);
			exceptionData.AppendLine("Acquire (Exclusive)...");
			device.Acquire();
			ud.IsExclusiveMode = cooperationLevel == CooperativeLevel.Exclusive;
		}

		#endregion

		#region Fields

		UserDevice[] mappedDevices = new UserDevice[0];
		UserGame currentGame = SettingsManager.CurrentGame;
		CustomDiState newState = null;
		CustomDiUpdate[] newUpdates = null;
		Options options = SettingsManager.Options;
		Boolean isVirtual = false;

		#endregion

		#region UpdateDiStates Method

		void UpdateDiStates(UserGame game, DeviceDetector detector)
		{
			// Get all mapped user devices for the specified game (if game or devices changed).
			if (Global.DHelper.SettingsChanged)
			{
				currentGame = game;
				options = SettingsManager.Options;
				mappedDevices = SettingsManager.GetMappedDevices(game?.FileName)
					.Where(x => x != null && x.IsOnline && x.Device != null)
					.ToArray();
				isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
			}

			//if (options.TestEnabled && !options.TestGetDInputStates) return;

			foreach (var ud in mappedDevices)
			{
				//if (/*ud == null || !ud.IsOnline || ud.Device == null || */(options.TestEnabled && !options.TestGetDInputStates)) continue;
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				//JoystickState state = null;

				// Note: && manager.IsDeviceAttached(ud.InstanceGuid) use a lot of CPU resources. 
				// Allow if not testing or testing with option enabled.
				var device = ud.Device;

				if (device != null)
				{
					var exceptionData = new StringBuilder();
					try
					{
						bool useData = false;
						if (options.UseDeviceBufferedData && device.Properties.BufferSize == 0)
						{
							// Set BufferSize in order to use buffered data.
							device.Properties.BufferSize = 128;
							useData = true;
						}
						var deviceType = device.Information.Type;
						// Original device will be hidden from the game when acquired in exclusive mode.
						// If device is not keyboard or mouse then apply AcquireMappedDevicesInExclusiveMode option.
						var acquireMappedDevicesInExclusiveMode =
							(deviceType != SharpDX.DirectInput.DeviceType.Keyboard && deviceType != SharpDX.DirectInput.DeviceType.Mouse)
							? options.AcquireMappedDevicesInExclusiveMode
							: false;
						// Exclusive mode required only if force feedback is available and device is virtual or there is no info about effects.
						var hasForceFeedback = device.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);
						var exclusiveRequired = acquireMappedDevicesInExclusiveMode ||
							(hasForceFeedback && (isVirtual || ud.DeviceEffects == null));
						// Check if the current mode is unknown or differs from the desired, then reacquire the device.
						if (!ud.IsExclusiveMode.HasValue || ud.IsExclusiveMode.Value != exclusiveRequired)
						{
							var cooperativeLevel = exclusiveRequired ? CooperativeLevel.Exclusive : CooperativeLevel.NonExclusive;
							// Reacquire device in exclusive or in non exclusive mode (xinput.dll can control force feedback).
							DeviceExclusiveMode(ud, detector, device, exceptionData, cooperativeLevel);
						}
						exceptionData.AppendFormat($"device.GetCurrentState() // ud.IsExclusiveMode = {ud.IsExclusiveMode}").AppendLine();
						// Polling - Retrieves data from polled objects on a DirectInput device.
						// Some devices require pooling (For example original "XBOX Controller S" with XBCD drivers).
						// If the device does not require polling, calling this method has no effect.
						// If a device that requires polling is not polled periodically, no new data is received from the device.
						// Calling this method causes DirectInput to update the device state, generate input
						// events (if buffered data is enabled), and set notification events (if notification is enabled).
						device.Poll();

						// Use switch based on pattern matching for supported device types.
						switch (device)
						{
							case Mouse mDevice:
								newUpdates = useData ? mDevice.GetBufferedData()?.Select(x => new CustomDiUpdate(x)).ToArray() : null;
								{
									var state = mDevice.GetCurrentState();
									newState = new CustomDiState(state);
									ud.DeviceState = state;
								}
								break;
							case Keyboard kDevice:
								newUpdates = useData ? kDevice.GetBufferedData()?.Select(x => new CustomDiUpdate(x)).ToArray() : null;
								{
									var state = kDevice.GetCurrentState();
									newState = new CustomDiState(state);
									ud.DeviceState = state;
								}
								break;
							case Joystick jDevice:
								newUpdates = useData ? jDevice.GetBufferedData()?.Select(x => new CustomDiUpdate(x)).ToArray() : null;
								{
									var state = jDevice.GetCurrentState();
									newState = new CustomDiState(state);
									ud.DeviceState = state;
								}
								break;
							default:
								throw new Exception($"Unknown device: {device}");
						}

						// Fill device objects force feedback actuator masks.
						if (ud.DeviceObjects == null)
						{
							exceptionData.AppendFormat($"AppHelper.GetDeviceObjectsByUsageAndInstanceNumber(device) // ud.IsExclusiveMode = {ud.IsExclusiveMode}").AppendLine();
							// var item = AppHelper.GetDeviceObjects(ud, device);
							// ud.DeviceObjects = item;
							//// Update masks.
							//int axisMask = 0;
							//int actuatorMask = 0;
							//int actuatorCount = 0;
							//if (device is Mouse mDevice2)
							//{
							//	CustomDiState.GetMouseAxisMask(item, mDevice2, out axisMask);
							//}
							//else if (device is Joystick jDevice)
							//{
							//	CustomDiState.GetJoystickAxisMask(item, jDevice, out axisMask, out actuatorMask, out actuatorCount);
							//}
							//ud.DiAxeMask = axisMask;
							//// Contains information about which axis have force feedback actuator attached.
							//ud.DiActuatorMask = actuatorMask;
							//ud.DiActuatorCount = actuatorCount;
						}
						if (ud.DeviceEffects == null)
						{
							exceptionData.AppendFormat($"AppHelper.GetDeviceEffects(device) // ud.IsExclusiveMode = {ud.IsExclusiveMode}").AppendLine();
							ud.DeviceEffects = AppHelper.GetDeviceEffects(device);
						}

						// Handle force feedback if supported.
						if (hasForceFeedback)
						{
							// Get setting related to user device.
							var setting = SettingsManager.UserSettings.ItemsToArraySynchronized()
								.FirstOrDefault(x => x.InstanceGuid == ud.InstanceGuid);
							if (setting != null && setting.MapTo > (int)MapTo.None)
							{
								// Get pad setting attached to device.
								var ps = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
								if (ps != null)
								{
									if (ps.ForceEnable == "1")
									{
										ud.FFState = ud.FFState ?? new Engine.ForceFeedbackState();
										// If force update supplied then...
										var force = CopyAndClearFeedbacks()[setting.MapTo - 1];
										if (force != null || ud.FFState.Changed(ps))
										{
											var vibration = new Vibration
											{
												LeftMotorSpeed = (force == null) ? short.MinValue : (short)ConvertHelper.ConvertRange(force.LargeMotor, byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue),
												RightMotorSpeed = (force == null) ? short.MinValue : (short)ConvertHelper.ConvertRange(force.SmallMotor, byte.MinValue, byte.MaxValue, short.MinValue, short.MaxValue)
											};
											// For the future: Investigate device states if force feedback is not working. 
											// var st = ud.Device.GetForceFeedbackState();
											// st == SharpDX.DirectInput.ForceFeedbackState
											// ud.Device.SendForceFeedbackCommand(ForceFeedbackCommand.SetActuatorsOn);
											exceptionData.AppendFormat("ud.FFState.SetDeviceForces(device) // ud.IsExclusiveMode = {0}", ud.IsExclusiveMode).AppendLine();
											ud.FFState.SetDeviceForces(ud, device, ps, vibration);
										}
									}
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
						if (dex != null &&
							(dex.ResultCode == SharpDX.DirectInput.ResultCode.InputLost ||
							 dex.ResultCode == SharpDX.DirectInput.ResultCode.NotAcquired ||
							 dex.ResultCode == SharpDX.DirectInput.ResultCode.Unplugged))
						{
							Debug.WriteLine($"InputLost {DateTime.Now:HH: mm: ss.fff}");
							Debug.WriteLine($"Device {dex.Descriptor.ApiCode}. DisplayName {ud.DisplayName}. ProductId {ud.DevProductId}. ProductName {ud.ProductName}. InstanceName {ud.InstanceName}.");
							DevicesNeedUpdating = true;
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
					// Fill device objects and update masks.
					if (ud.DeviceObjects == null)
					{
						ud.DeviceObjects = TestDeviceHelper.GetDeviceObjects();
						ud.DiAxeMask = 0x1 | 0x2 | 0x4 | 0x8;
						ud.DiSliderMask = 0;
					}
					ud.DeviceEffects = ud.DeviceEffects ?? new DeviceEffectItem[0];
					var state = TestDeviceHelper.GetCurrentState(ud);
					newState = new CustomDiState(state);
					ud.DeviceState = state;
				}

				if (newState != null)
				{
					// If updates from buffer supplied and old state is available then...
					if (newUpdates != null && newUpdates.Count(x => x.Type == MapType.Button) > 1 && ud.DiState != null)
					{
						// Analyse if state must be modified.
						for (int b = 0; b < newState.Buttons.Length; b++)
						{
							// If button state was not changed.
							if (ud.DiState.Buttons[b] == newState.Buttons[b])
							{
								// But buffer contains press then...
								if (newUpdates.Count(x => x.Type == MapType.Button && x.Index == b) > 1)
								{
									// Invert state and give chance for the game to recognize the press.
									newState.Buttons[b] = !newState.Buttons[b];
								}
							}
						}
					}
					var newTime = _Stopwatch.ElapsedTicks;
					// Remember old value, set new value.
					(ud.OldDiState, ud.DiState) = (ud.DiState, newState);
					(ud.OldDiUpdates, ud.DiUpdates) = (ud.DiUpdates, newUpdates);
					(ud.OldDiStateTime, ud.DiStateTime) = (ud.DiStateTime, newTime);
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

		#endregion

		#region Helper Methods

		void Calc(int[] orgRange, int[] newState, int[] mouseState)
		{
			// Sensitivity factor for mouse movement conversion.
			var sensitivity = 16;
			for (int a = 0; a < newState.Length; a++)
			{
				// Get delta from original state and apply sensitivity.
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

		#endregion
	}
}
