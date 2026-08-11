using System;
using System.Threading;
using System.Threading.Tasks;

namespace x360ce.App.Diagnostics
{
	public static class StartupStageRunner
	{
		/// <summary>
		/// Runs startup work away from the dispatcher and returns false at the
		/// deadline. A native call that ignores cancellation may finish later, but
		/// it cannot keep the UI startup path waiting.
		/// </summary>
		public static async Task<bool> RunAsync(
			Action<CancellationToken> action,
			TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (timeout <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout));

			using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				var work = Task.Run(() => action(deadline.Token), CancellationToken.None);
				var delay = Task.Delay(timeout, cancellationToken);
				var completed = await Task.WhenAny(work, delay);
				if (completed == work)
				{
					await work;
					return true;
				}

				cancellationToken.ThrowIfCancellationRequested();
				deadline.Cancel();
				// Observe a late failure without synchronously waiting for uncooperative
				// native or filesystem work.
				_ = work.ContinueWith(
					task => { var ignored = task.Exception; },
					CancellationToken.None,
					TaskContinuationOptions.OnlyOnFaulted,
					TaskScheduler.Default);
				return false;
			}
		}
	}
}
