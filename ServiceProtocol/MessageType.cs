using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    enum MessageType : byte
    {
        EraS = 0,
        Internal = 1,
        Service = 2,
    }
}
