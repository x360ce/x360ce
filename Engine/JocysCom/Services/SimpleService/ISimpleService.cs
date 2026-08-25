namespace JocysCom.ClassLibrary.Services.SimpleService
{
	public interface ISimpleService
	{
		bool IsStopping { get; set; }
		bool IsPaused { get; set; }
		void InitStart();
		/// <summary>
		/// Method which will be called continuously.
		/// </summary>
		/// <param name="args">Windows web service start arguments.</param>
		/// <param name="skipSleep">Set it to true if sleep must be skipped and DoAction must be restarted immediately.</param>
		void DoAction(string[] args, ref bool skipSleep);
		void InitEnd();
	}
}
