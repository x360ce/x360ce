using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{

	/// <summary>
	/// Class used to store current force feedback state of device.
	/// </summary>
	public class ForceFeedbackState
	{
		public ForceFeedbackState()
		{
			LeftRestart = true;
			RightRestart = true;
			LeftPeriodicForce = new PeriodicForce();
			RightPeriodicForce = new PeriodicForce();
			LeftConstantForce = new ConstantForce();
			RightConstantForce = new ConstantForce();
		}

		// Left

		public Effect LeftEffect;
		public bool LeftRestart;
		public PeriodicForce LeftPeriodicForce;
		public ConstantForce LeftConstantForce;

		// Right

		public Effect RightEffect;
		public bool RightRestart;
		public PeriodicForce RightPeriodicForce;
		public ConstantForce RightConstantForce;

	}
}
