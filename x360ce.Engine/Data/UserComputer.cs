using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine.Data
{
    public partial class UserComputer: IChecksum, IDateTime
    {
        public UserComputer()
        {
            DateCreated = DateTime.Now;
            DateUpdated = DateCreated;
        }

    }
}
