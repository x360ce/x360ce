using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using JocysCom.ClassLibrary.Mail;
using x360ce.Engine.Data;

namespace x360ce.Engine
{

	/// <summary>
	///  Message which will be used to communicate with web service.
	/// </summary>
	public class CloudMessage
	{

		public CloudMessage()
		{
			Values = new KeyValueList();
		}

		public CloudMessage(CloudAction action)
		{
			Action = action;
			Values = new KeyValueList();
		}

		[DefaultValue(0)]
		public int ErrorCode { get; set; }

		[DefaultValue(null)]
		public string ErrorMessage { get; set; }

		[DefaultValue(CloudAction.None)]
		public CloudAction Action { get; set; }

		[DefaultValue(null)]
		public KeyValueList Values { get; set; }

		#region Data

		/// <summary>
		/// Supply checksums of existing data to skip during synchronization in order to reduce traffic.
		/// </summary>
		[XmlArray] public Guid[] Checksums { get; set; }

		[XmlArray] public UserDevice[] UserDevices { get; set; }

		[XmlArray] public UserInstance[] UserInstances { get; set; }

		[XmlArray] public UserComputer[] UserComputers { get; set; }

		[XmlArray] public UserSetting[] UserSettings { get; set; }

		[XmlArray] public MailMessageSerializable[] MailMessages { get; set; }

		/// <summary>
		/// During request it will be used to specify search filters. If null then do not retrieve.
		/// During response it will contain used data.
		/// </summary>
		[XmlArray] public UserGame[] UserGames { get; set; }

		#endregion

		#region Help Methods

		public bool FixComputerId(out string error)
		{
			var computerId = CloudHelper.GetGuidId(CloudKey.ComputerId, this, out error);
			if (!computerId.HasValue)
				return false;
			var profileId = CloudHelper.GetGuidId(CloudKey.ProfileId, this, out error);
			if (!profileId.HasValue)
				return false;
			Fix(computerId.Value, profileId.Value, UserGames);
			Fix(computerId.Value, profileId.Value, UserDevices);
			Fix(computerId.Value, profileId.Value, UserInstances);
			Fix(computerId.Value, profileId.Value, UserSettings);
			return true;
		}

		void Fix(Guid computerId, Guid profileid, IUserRecord[] list)
		{
			if (list == null)
				return;
			foreach (var item in list)
			{
				item.ComputerId = computerId;
				item.ProfileId = profileid;
			}
		}

		#endregion

	}
}
