using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    enum ControlType : byte
    {
        Initialize = 0,
        Kill = 1,
        IdentifierNotFound = 2,

        Reserved = 255,
    }
}
