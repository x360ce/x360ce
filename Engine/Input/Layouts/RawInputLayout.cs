using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.Engine
{
	/// <summary>Where each control of a Raw Input device goes in its <see cref="SourceState"/>, by data index.</summary>
	/// <remarks>
	/// A control goes into the slot DirectInput fills for the same control, so a mapping made on either reads the
	/// same. Made once per device; the reader looks a control up on every report.
	/// </remarks>
	public sealed class RawInputLayout
	{
		/// <summary>The kind of slot each control goes to: <see cref="MapType.Axis"/>, <see cref="MapType.Slider"/>, <see cref="MapType.POV"/>, <see cref="MapType.Button"/>, or <see cref="MapType.None"/> for none.</summary>
		public readonly MapType[] Types;

		/// <summary>The index of each control's slot in <see cref="SourceState.Axis"/>, <see cref="SourceState.Sliders"/>, <see cref="SourceState.Povs"/> or <see cref="SourceState.Buttons"/>.</summary>
		public readonly int[] Indexes;

		/// <summary>The twin's controls the layout was made from, which name and describe the slots it fills; null for a layout made from the usages.</summary>
		public readonly DeviceObjectItem[] TwinObjects;

		/// <summary>The controls the layout was made for, by data index.</summary>
		readonly RawInputControl[] controls;

		/// <summary>The objects <see cref="GetDeviceObjects"/> made, once.</summary>
		DeviceObjectItem[] madeObjects;

		RawInputLayout(RawInputControl[] controls, DeviceObjectItem[] twinObjects)
		{
			this.controls = controls;
			TwinObjects = twinObjects;
			Types = new MapType[controls.Length];
			Indexes = new int[controls.Length];
		}

		/// <summary>Whether the layout fits a device with <paramref name="deviceControls"/>: as many controls, each a button or a value as before, with the same usage page and usage.</summary>
		/// <remarks>A device that arrives again fits the layout it had, unless its firmware or mode now describes other controls.</remarks>
		public bool Fits(RawInputControl[] deviceControls)
		{
			if (deviceControls == null || deviceControls.Length != controls.Length)
				return false;
			for (var i = 0; i < controls.Length; i++)
			{
				var a = controls[i];
				var b = deviceControls[i];
				if (a == null || b == null)
				{
					if (a != b)
						return false;
					continue;
				}
				if (a.IsButton != b.IsButton || a.UsagePage != b.UsagePage || a.Usage != b.Usage)
					return false;
			}
			return true;
		}

		/// <summary>Whether <paramref name="other"/> puts every control in the same slot as this layout.</summary>
		public bool SameSlots(RawInputLayout other)
		{
			if (other == null || other.Types.Length != Types.Length)
				return false;
			for (var i = 0; i < Types.Length; i++)
				if (other.Types[i] != Types[i] || other.Indexes[i] != Indexes[i])
					return false;
			return true;
		}

		#region From the DirectInput twin

		/// <summary>The slots DirectInput uses for the same device, read from its twin's controls.</summary>
		/// <remarks>
		/// <para>
		/// DirectInput does not place a control by its usage: on a Logitech G27 the combined pedals (1:31) are
		/// Sliders[0], the accelerator (1:32) is Y and the clutch (1:36) is Sliders[1]. So the slot is taken from the
		/// twin. A control is matched to the twin's control with the same usage page, usage and kind; when a usage
		/// appears more than once, the first of one is matched to the first of the other, and so on.
		/// </para>
		/// <para>
		/// <paramref name="objects"/> must be the twin's controls as <see cref="Data.UserDevice.DeviceObjects"/> holds
		/// them once the engine has read the device, which is what sets each slot: an axis's
		/// <see cref="DeviceObjectItem.DiIndex"/> is its place in <see cref="SourceState.Axis"/>, as
		/// <see cref="SourceState.GetJoystickAxisMask"/> learns from DirectInput itself, a slider's its place in
		/// <see cref="SourceState.Sliders"/>, from <see cref="SourceState.GetJoystickSlidersMask"/>, and a button's
		/// its place in <see cref="SourceState.Buttons"/>. A hat's DiIndex is not a slot, so hats go into
		/// <see cref="SourceState.Povs"/> in the order the twin lists them, which is the order DirectInput fills them.
		/// </para>
		/// <para>
		/// A control the twin does not list, such as one on a vendor-defined page, goes nowhere, as it does in DirectInput.
		/// </para>
		/// </remarks>
		public static RawInputLayout FromTwin(RawInputControl[] controls, DeviceObjectItem[] objects)
		{
			var layout = new RawInputLayout(controls, objects);
			var types = new MapType[objects.Length];
			var indexes = new int[objects.Length];
			var povs = 0;
			for (var i = 0; i < objects.Length; i++)
			{
				var o = objects[i];
				if (o.Type == ObjectGuid.XAxis || o.Type == ObjectGuid.YAxis || o.Type == ObjectGuid.ZAxis ||
					o.Type == ObjectGuid.RxAxis || o.Type == ObjectGuid.RyAxis || o.Type == ObjectGuid.RzAxis)
					SetSlot(types, indexes, i, MapType.Axis, o.DiIndex, DirectInputLayout.AxisOffsets.Count);
				else if (o.Type == ObjectGuid.Slider)
					SetSlot(types, indexes, i, MapType.Slider, o.DiIndex, DirectInputLayout.SliderOffsets.Count);
				else if (o.Type == ObjectGuid.PovController)
					SetSlot(types, indexes, i, MapType.POV, povs++, DirectInputLayout.PovOffsets.Count);
				else if (o.Type == ObjectGuid.Button || o.Type == ObjectGuid.Key)
					SetSlot(types, indexes, i, MapType.Button, o.DiIndex, DirectInputLayout.ButtonOffsets.Count);
			}
			var matched = new bool[objects.Length];
			foreach (var c in controls)
			{
				if (c == null)
					continue;
				for (var i = 0; i < objects.Length; i++)
				{
					var o = objects[i];
					if (matched[i] || types[i] == MapType.None || o.UsagePage != c.UsagePage || o.Usage != c.Usage)
						continue;
					if ((types[i] == MapType.Button) != c.IsButton)
						continue;
					matched[i] = true;
					layout.Types[c.DataIndex] = types[i];
					layout.Indexes[c.DataIndex] = indexes[i];
					break;
				}
			}
			return layout;
		}

		static void SetSlot(MapType[] types, int[] indexes, int i, MapType type, int index, int count)
		{
			if (index < 0 || index >= count)
				return;
			types[i] = type;
			indexes[i] = index;
		}

		#endregion

		#region Standard

		const int GenericDesktopPage = 0x01;
		const int SimulationPage = 0x02;
		const int ButtonPage = 0x09;

		const int XUsage = 0x30;
		const int RzUsage = 0x35;
		const int SliderUsage = 0x36;
		const int DialUsage = 0x37;
		const int WheelUsage = 0x38;
		const int HatSwitchUsage = 0x39;

		const int RudderUsage = 0xBA;
		const int ThrottleUsage = 0xBB;
		const int AcceleratorUsage = 0xC4;
		const int BrakeUsage = 0xC5;
		const int ClutchUsage = 0xC6;
		const int SteeringUsage = 0xC8;

		/// <summary>The slots a device with no DirectInput twin uses, from the HID usage of each control.</summary>
		/// <remarks>
		/// <para>
		/// Generic Desktop X, Y, Z, Rx, Ry and Rz are Axis[0] to Axis[5]; a slider, dial or wheel takes the next free
		/// slider, and a hat switch the next free hat. Button N is Buttons[N - 1]. Of the Simulation page, steering is
		/// Axis[0], the accelerator Axis[1], the brake Axis[5], a rudder Axis[5], and a throttle or clutch takes the
		/// next free slider.
		/// </para>
		/// <para>
		/// Controls are placed in data index order, and a control whose slot is already taken, such as a second X axis
		/// or a rudder beside a brake, goes nowhere. So does every other usage, those on vendor-defined pages included.
		/// </para>
		/// </remarks>
		public static RawInputLayout Standard(RawInputControl[] controls)
		{
			var layout = new RawInputLayout(controls, null);
			var axes = new bool[DirectInputLayout.AxisOffsets.Count];
			var sliders = new bool[DirectInputLayout.SliderOffsets.Count];
			var povs = new bool[DirectInputLayout.PovOffsets.Count];
			var buttons = new bool[DirectInputLayout.ButtonOffsets.Count];
			foreach (var c in controls)
			{
				if (c == null)
					continue;
				if (c.IsButton)
				{
					if (c.UsagePage == ButtonPage)
						Place(layout, c, MapType.Button, c.Usage - 1, buttons);
					continue;
				}
				if (c.UsagePage == GenericDesktopPage)
				{
					if (c.Usage >= XUsage && c.Usage <= RzUsage)
						Place(layout, c, MapType.Axis, c.Usage - XUsage, axes);
					else if (c.Usage == SliderUsage || c.Usage == DialUsage || c.Usage == WheelUsage)
						Place(layout, c, MapType.Slider, Array.IndexOf(sliders, false), sliders);
					else if (c.Usage == HatSwitchUsage)
						Place(layout, c, MapType.POV, Array.IndexOf(povs, false), povs);
				}
				else if (c.UsagePage == SimulationPage)
				{
					if (c.Usage == SteeringUsage)
						Place(layout, c, MapType.Axis, 0, axes);
					else if (c.Usage == AcceleratorUsage)
						Place(layout, c, MapType.Axis, 1, axes);
					else if (c.Usage == BrakeUsage || c.Usage == RudderUsage)
						Place(layout, c, MapType.Axis, 5, axes);
					else if (c.Usage == ThrottleUsage || c.Usage == ClutchUsage)
						Place(layout, c, MapType.Slider, Array.IndexOf(sliders, false), sliders);
				}
			}
			return layout;
		}

		static void Place(RawInputLayout layout, RawInputControl c, MapType type, int index, bool[] taken)
		{
			if (index < 0 || index >= taken.Length || taken[index])
				return;
			taken[index] = true;
			layout.Types[c.DataIndex] = type;
			layout.Indexes[c.DataIndex] = index;
		}

		#endregion

		#region Device objects

		/// <summary>The objects that describe the slots the layout fills, as <see cref="Data.UserDevice.DeviceObjects"/> holds them for the pages and the automatic preset.</summary>
		/// <remarks>
		/// <para>
		/// A layout made from the twin returns copies of the twin's objects, whose slots are the layout's by construction,
		/// so the names, kinds and <see cref="DeviceObjectItem.DiIndex"/> DirectInput gave them apply. The copies carry no
		/// force feedback flags: Raw Input sends nothing to a device, so its pages must not count motors it cannot drive.
		/// </para>
		/// <para>
		/// Otherwise there is one object for each control placed, in data index order: an axis in Axis[0] to Axis[5] is
		/// the X to Z Rotation axis, then sliders, hats and buttons by their kind. Its DiIndex and instance are its slot,
		/// its offset the slot's in DirectInput's joystick state, and its name the HID usage's.
		/// </para>
		/// <para>
		/// Made on the first call and kept, so every call returns the same array. One thread at a time: the device list's.
		/// </para>
		/// </remarks>
		public DeviceObjectItem[] GetDeviceObjects()
		{
			if (madeObjects != null)
				return madeObjects;
			if (TwinObjects != null)
			{
				madeObjects = TwinObjects.Select(WithoutForceFeedback).ToArray();
				return madeObjects;
			}
			var items = new List<DeviceObjectItem>();
			for (var i = 0; i < Types.Length; i++)
			{
				var c = controls[i];
				var slot = Indexes[i];
				if (c == null)
					continue;
				switch (Types[i])
				{
					case MapType.Axis:
						items.Add(Item(c, slot, slot < AxisTypes.Length ? AxisTypes[slot] : ObjectGuid.Unknown,
							(int)DirectInputLayout.AxisOffsets[slot], ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis));
						break;
					case MapType.Slider:
						items.Add(Item(c, slot, ObjectGuid.Slider,
							(int)DirectInputLayout.SliderOffsets[slot], ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis));
						break;
					case MapType.POV:
						items.Add(Item(c, slot, ObjectGuid.PovController,
							(int)DirectInputLayout.PovOffsets[slot], 0, DeviceObjectTypeFlags.PointOfViewController));
						break;
					case MapType.Button:
						items.Add(Item(c, slot, ObjectGuid.Button,
							(int)DirectInputLayout.ButtonOffsets[slot], 0, DeviceObjectTypeFlags.PushButton));
						break;
				}
			}
			madeObjects = items.ToArray();
			return madeObjects;
		}

		/// <summary>A copy of a twin's object with its force feedback flags taken off.</summary>
		static DeviceObjectItem WithoutForceFeedback(DeviceObjectItem o)
		{
			return new DeviceObjectItem
			{
				Name = o.Name,
				Offset = o.Offset,
				ObjectId = o.ObjectId,
				Instance = o.Instance,
				DiIndex = o.DiIndex,
				Aspect = o.Aspect,
				Flags = o.Flags & ~(DeviceObjectTypeFlags.ForceFeedbackActuator | DeviceObjectTypeFlags.ForceFeedbackEffectTrigger),
				Type = o.Type,
				UsagePage = o.UsagePage,
				Usage = o.Usage,
				DeadZone = o.DeadZone,
				Granularity = o.Granularity,
				LogicalRangeMin = o.LogicalRangeMin,
				LogicalRangeMax = o.LogicalRangeMax,
				PhysicalRangeMin = o.PhysicalRangeMin,
				PhysicalRangeMax = o.PhysicalRangeMax,
				RangeMin = o.RangeMin,
				RangeMax = o.RangeMax,
				Saturation = o.Saturation,
			};
		}

		/// <summary>The kind of each of the first six axis slots, X to Z Rotation, as DirectInput names them.</summary>
		static readonly Guid[] AxisTypes = { ObjectGuid.XAxis, ObjectGuid.YAxis, ObjectGuid.ZAxis, ObjectGuid.RxAxis, ObjectGuid.RyAxis, ObjectGuid.RzAxis };

		static DeviceObjectItem Item(RawInputControl c, int slot, Guid type, int offset, ObjectAspect aspect, DeviceObjectTypeFlags flags)
		{
			return new DeviceObjectItem(offset, type, aspect, flags, slot, UsageName(c, slot))
			{
				DiIndex = slot,
				UsagePage = c.UsagePage,
				Usage = c.Usage,
			};
		}

		/// <summary>The name DirectInput gives a control with the same usage: "X Axis", "Hat Switch", "Button 0" and so on.</summary>
		static string UsageName(RawInputControl c, int slot)
		{
			if (c.IsButton)
				return "Button " + (c.UsagePage == ButtonPage ? c.Usage - 1 : slot);
			if (c.UsagePage == GenericDesktopPage)
			{
				switch (c.Usage)
				{
					case XUsage: return "X Axis";
					case XUsage + 1: return "Y Axis";
					case XUsage + 2: return "Z Axis";
					case XUsage + 3: return "X Rotation";
					case XUsage + 4: return "Y Rotation";
					case RzUsage: return "Z Rotation";
					case SliderUsage: return "Slider";
					case DialUsage: return "Dial";
					case WheelUsage: return "Wheel";
					case HatSwitchUsage: return "Hat Switch";
				}
			}
			else if (c.UsagePage == SimulationPage)
			{
				switch (c.Usage)
				{
					case RudderUsage: return "Rudder";
					case ThrottleUsage: return "Throttle";
					case AcceleratorUsage: return "Accelerator";
					case BrakeUsage: return "Brake";
					case ClutchUsage: return "Clutch";
					case SteeringUsage: return "Steering";
				}
			}
			return string.Format("Usage {0:X2}:{1:X2}", c.UsagePage, c.Usage);
		}

		#endregion
	}
}
