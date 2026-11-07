using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using x360ce.Engine;
using System.Linq;
using x360ce.Engine.Data;
using System.Runtime.CompilerServices;

namespace x360ce.App
{
	/// <summary>
	/// Contains Item which needs to be updated on the cloud (Cloud Message will be created from it).
	/// </summary>
	public class CloudItem : INotifyPropertyChanged
	{
		public CloudAction Action { get { return Message == null ? CloudAction.None : Message.Action; } }

		public CloudState State { get { return _State; } set { _State = value; OnPropertyChanged(); } }
		CloudState _State;

        public int Try { get { return _Try; } set { _Try = value; OnPropertyChanged(); } }
        int _Try;

        public int Retries { get { return _Retries; } set { _Retries = value; OnPropertyChanged(); } }
		int _Retries = 4;

		public DateTime Date { get; set; }

		[XmlIgnore]
		public Exception Error { get; set; }

		/// <summary>When the item may be sent again: not before the wait after its last failed try is over.</summary>
		public DateTime NextTry { get; set; }

		/// <summary>How long an item waits after each failed try, the first wait first; the last one repeats.</summary>
		public static readonly TimeSpan[] RetryWaits = { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2) };

		/// <summary>
		/// The wait after a number of failed tries: longer after each, plus up to a fifth more, so that programs which
		/// failed at the same moment do not all try again at the same moment. Never shorter than the first wait.
		/// </summary>
		/// <param name="failedTries">Failed tries so far, at least 1.</param>
		/// <param name="jitter">A number from 0 to 1 that picks how much of the fifth is added.</param>
		public static TimeSpan RetryWait(int failedTries, double jitter)
		{
			var wait = RetryWaits[Math.Min(Math.Max(failedTries, 1), RetryWaits.Length) - 1];
			return TimeSpan.FromTicks(wait.Ticks + (long)(wait.Ticks * 0.2 * jitter));
		}

		/// <summary>
		/// Records a failed try. After a failure that can pass, such as no connection, the item waits and is sent again
		/// until it has had <see cref="Retries"/> tries; any other failure, and the last try, stops it.
		/// </summary>
		/// <param name="error">Why the try failed.</param>
		/// <param name="transient">True when the same message may succeed later.</param>
		/// <param name="now">The time the try ended.</param>
		/// <param name="jitter">A number from 0 to 1, see <see cref="RetryWait"/>.</param>
		public void Fail(Exception error, bool transient, DateTime now, double jitter)
		{
			Error = error;
			if (transient && Try < Retries)
			{
				NextTry = now + RetryWait(Try, jitter);
				State = CloudState.Error;
			}
			else
			{
				State = CloudState.Stopped;
			}
		}

		/// <summary>Makes the item due now, with all its tries again: what the person asks for with Retry.</summary>
		public void Retry()
		{
			Try = 0;
			Error = null;
			NextTry = default(DateTime);
			State = CloudState.None;
		}

		/// <summary>
		/// Message command to send.
		/// </summary>
		public CloudMessage Message
		{
			get { return _Message; }
			set
			{
				_Message = value;
				OnPropertyChanged(nameof(Action));
				OnPropertyChanged(nameof(Description));
			}
		}
		CloudMessage _Message;

		public string Description
		{
			get
			{
				var list = new List<string>();
				if (Message.UserDevices != null)
				{
					list.AddRange(Message.UserDevices.Select(x => string.Format("{0}: {1}", typeof(UserDevice).Name, x.DisplayName)));
					if (Message.UserDevices.Length == 0)
						list.Add(string.Format("{0}s", typeof(UserDevice).Name));
				}
				if (Message.UserGames != null)
				{
					list.AddRange(Message.UserGames.Select(x => string.Format("{0}: {1}", typeof(UserGame).Name, x.DisplayName)));
					if (Message.UserGames.Length == 0)
						list.Add(string.Format("{0}s", typeof(UserGame).Name));
				}
				if (Message.UserSettings != null)
				{
					list.AddRange(Message.UserSettings.Select(x => string.Format("{0}: {1}", typeof(UserSetting).Name, x.SettingId)));
					if (Message.UserSettings.Length == 0)
						list.Add(string.Format("{0}s", typeof(UserSetting).Name));
				}
				return string.Join(", ", list);
			}
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}

}
