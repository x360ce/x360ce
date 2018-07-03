using System;

namespace x360ce.Engine.Data
{
	/// <summary>
	/// This record is used to link different Instance IDs to the same physical controller (Controller ID).
	/// </summary>
	public partial class UserInstance : IUserRecord
	{

		public UserInstance()
		{
			DateCreated = DateTime.Now;
			DateUpdated = DateCreated;
		}

	}
}
