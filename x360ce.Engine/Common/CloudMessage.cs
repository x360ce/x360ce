using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	public class CloudMessage
	{

		public CloudMessage()
		{
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

		[DefaultValue(null)]
		public List<UserDevice> UserControllers { get; set; }

		[DefaultValue(null)]
		public List<UserGame> UserGames { get; set; }

	}
}
