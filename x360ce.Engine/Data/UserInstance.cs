using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine.Data
{
    public partial class UserInstance : IChecksum, IDateTime
    {

        public UserInstance()
        {
            DateCreated = DateTime.Now;
            DateUpdated = DateCreated;
        }

    }
}
