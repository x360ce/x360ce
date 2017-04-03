using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	public class CloudCommand
	{

		public CloudAction Action { get; set; }

		public List<UserDevice> UserControllers { get; set; }
		public List<UserGame> UserGames { get; set; }

	}
}
