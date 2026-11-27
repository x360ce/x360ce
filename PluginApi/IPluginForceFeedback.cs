namespace x360ce.Plugins
{
	/// <summary>A plugin device that takes the force feedback a game sends to the controller it is mapped to.</summary>
	/// <remarks>
	/// Optional: a class that implements <see cref="IPluginDevice"/> implements this too to feel the game, as a pad's
	/// motors or a wheel's do. The controller tab's Force feedback enabled switch decides whether the game's force reaches
	/// the device, as for any device; a device on several tabs takes the strongest of each motor.
	/// </remarks>
	public interface IPluginForceFeedback
	{
		/// <summary>Takes the force the game asks for now.</summary>
		/// <remarks>
		/// Called on x360ce's input thread, just before <see cref="IPluginDevice.Read"/>, whenever the force changes, and
		/// with 0 and 0 when it stops. Like Read, it must return at once.
		/// </remarks>
		/// <param name="largeMotor">The large, slow motor, 0 to 65535, as the game asks for it, before the tab's strengths.</param>
		/// <param name="smallMotor">The small, fast motor, 0 to 65535, as the game asks for it, before the tab's strengths.</param>
		void SetForce(int largeMotor, int smallMotor);
	}
}
