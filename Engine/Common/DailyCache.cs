using System;
using System.Threading;

namespace x360ce.Engine
{
	/// <summary>
	/// A server's answer that is costly to make and the same for everybody, kept in memory: built at most once a day, and
	/// at the first request after the server starts.
	/// </summary>
	/// <remarks>
	/// One build at a time. Only a request that comes while there is nothing to serve waits for the build; the others get
	/// the last good answer at once. A build that fails keeps that answer, and the next build is tried only after
	/// <see cref="RetryAfter"/>, so a database that is down or slow is not asked again by every request.
	/// Used for x360ce's controller database (<see cref="X360ceDatabaseServer"/>) and the web service's Most Popular list.
	/// </remarks>
	public sealed class DailyCache<T> where T : class
	{
		/// <summary>How long an answer is served before it is built again.</summary>
		public static readonly TimeSpan MaxAge = TimeSpan.FromDays(1);

		/// <summary>How long after a failed build the next is tried.</summary>
		public static readonly TimeSpan RetryAfter = TimeSpan.FromHours(1);

		/// <param name="build">Makes the answer from the one served now, null at first; it may return that one to keep it.</param>
		/// <param name="utcNow">The clock; the real one when null.</param>
		public DailyCache(Func<T, T> build, Func<DateTime> utcNow = null)
		{
			Build = build;
			UtcNow = utcNow ?? (() => DateTime.UtcNow);
		}

		readonly Func<T, T> Build;
		readonly Func<DateTime> UtcNow;
		readonly object BuildLock = new object();
		volatile T _value;
		DateTime _built = DateTime.MinValue;
		DateTime _lastAttempt = DateTime.MinValue;

		/// <summary>How many builds succeeded.</summary>
		public int Builds { get; private set; }

		/// <summary>The answer, built first when there is none or it is a day old; null when there is none and it cannot be built.</summary>
		public T Get()
		{
			var value = _value;
			var taken = false;
			try
			{
				if (value == null)
					Monitor.Enter(BuildLock, ref taken);
				else
					Monitor.TryEnter(BuildLock, ref taken);
				// Another request is building: the last good answer meanwhile.
				if (!taken)
					return value;
				var now = UtcNow();
				if ((_value == null || now - _built >= MaxAge) && now - _lastAttempt >= RetryAfter)
				{
					_lastAttempt = now;
					try
					{
						_value = Build(_value);
						Builds++;
						_built = now;
					}
					catch (Exception)
					{
						// The database could not be read: the last good answer is served, and a build is tried again later.
					}
				}
				return _value;
			}
			finally
			{
				if (taken)
					Monitor.Exit(BuildLock);
			}
		}
	}
}
