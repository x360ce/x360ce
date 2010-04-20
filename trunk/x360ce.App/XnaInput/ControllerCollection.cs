using System;
using System.Collections;
using System.Reflection;

namespace x360ce.App.XnaInput
{

	public class ControllerCollection
	{
		private ArrayList controllers = new ArrayList();

		public ControllerCollection()
		{
            foreach (PlayerIndex playerIndex in Enum.GetValues(typeof(PlayerIndex)))
            {
                this.controllers.Add(new Controller(playerIndex));
            }
		}

		public Controller this[int index]
		{
			get
			{
				return (Controller)this.controllers[index];
			}
		}
	}
}

