namespace x360ce.App
{
	public enum ForceFeedBackType
	{
		/// <summary>Old constant force (like in 3.1.4.1)</summary>
		Constant = 0,
		/// <summary>EJocys method (from rev 150)</summary>
		PeriodicSine = 1,
		/// <summary>New force</summary>
		PeriodicSawtooth = 2,
	}
}
