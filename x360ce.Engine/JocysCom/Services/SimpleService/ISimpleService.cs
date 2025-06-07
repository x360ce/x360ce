namespace JocysCom.ClassLibrary.Services.SimpleService
{
	/// <summary>Defines the contract for simple Windows service implementations hosted by <see cref="SimpleServiceBase{T}"/>.</summary>
	public interface ISimpleService
	{
		bool IsStopping { get; set; }
		bool IsPaused { get; set; }
		/// <summary>Invoked once before the service loop begins; use to perform startup initialization.</summary>
		void InitStart();
		/// <summary>Called by <see cref="SimpleServiceBase{T}"/> on each loop iteration while the service is active.</summary>
		/// <param name="args">Windows web service start arguments.</param>
		/// <param name="skipSleep">Set it to true if sleep must be skipped and DoAction must be restarted immediately.</param>
		void DoAction(string[] args, ref bool skipSleep);
		/// <summary>Invoked once after the service loop ends; use to perform cleanup before service stops.</summary>
		void InitEnd();
	}
}
