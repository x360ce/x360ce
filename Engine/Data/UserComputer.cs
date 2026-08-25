using System;

namespace x360ce.Engine.Data
{
	public partial class UserComputer: IComputer, IChecksum, IDateTime
    {
        public UserComputer()
        {
            DateCreated = DateTime.Now;
            DateUpdated = DateCreated;
        }

    }
}
