using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
    public interface IDateTime
    {
        DateTime DateCreated { get; set; }
        DateTime DateUpdated { get; set; }

    }
}
