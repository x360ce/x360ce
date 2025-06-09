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

		public string TryRetry
			=> Retries == int.MaxValue
				? string.Format("{0}", Try)
				: string.Format("{0}/{1}", Try, Retries);


		public int Try
		{
			get { return _Try; }
			set
			{
				_Try = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(TryRetry));
			}
		}
		int _Try;

		public int Retries
		{
			get { return _Retries; }
			set
			{
				_Retries = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(TryRetry));
			}
		}
		int _Retries = 4;

		public DateTime Date { get; set; }

		[XmlIgnore]
		public Exception Error { get; set; }

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

		#region ■ INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}

}
