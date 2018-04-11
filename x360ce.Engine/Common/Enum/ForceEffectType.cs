using System;
using System.ComponentModel;

namespace x360ce.Engine
{

	[Flags]
	public enum ForceEffectType
	{
		///<summary>Good for vibrating motors. Good for vibrating motors on game pads.</summary>
		Constant = 0,
		///<summary>Periodic 'Sine Wave'. Good for car/plane engine vibration. Good for torque motors on wheels.</summary>
		PeriodicSine = 1,
		///<summary>Periodic 'Sawtooth Down Wave'. Good for gun recoil. Good for torque motors on wheels.</summary>
		PeriodicSawtooth = 2,
		///<summary>Alternative implementation - two motors / actuators per effect.</summary>
		_Type2 = 0x10000,
		Constant2 = Constant | _Type2,
		PeriodicSine2 = PeriodicSine | _Type2,
		PeriodicSawtooth2 = PeriodicSawtooth | _Type2,
	}
}
