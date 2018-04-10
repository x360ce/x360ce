using System;

namespace x360ce.Engine
{

	/// <summary>
	/// Forces for vibrating motors (Game pads):
	///     0 - Constant. Good for vibrating motors.
	/// Forces for torque motors (Wheels):
	///     1 - Periodic 'Sine Wave'. Good for car/plane engine vibration.
	///     2 - Periodic 'Sawtooth Down Wave'. Good for gun recoil.
	/// </summary>
	[Flags]
	public enum ForceEffectType
	{
		// Default Implementation (one motor/actuator per effect). 
		Constant = 0,
		PeriodicSine = 1,
		PeriodicSawtooth = 2,
		// Alternative implementations (two motors/actuators per effect).
		// Used by SpeedLink.
		_Type2 = 0x10000,
		Constant2 = Constant | _Type2,
		PeriodicSine2 = PeriodicSine | _Type2,
		PeriodicSawtooth2 = PeriodicSawtooth | _Type2,
	}
}
