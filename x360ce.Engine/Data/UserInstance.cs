using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine.Data
{
    public partial class UserInstance : IUserRecord
    {

		Guid IUserRecord.ItemId { get { return InstanceGuid; } set { InstanceGuid = value; } }

		public UserInstance()
        {
            DateCreated = DateTime.Now;
            DateUpdated = DateCreated;
        }

    }
}
