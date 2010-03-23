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
			for (int i = 0; i < 4; i++)
			{
				this.controllers.Add(new Controller(i));
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

