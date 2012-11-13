using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERAServer.Protocols.Server.Misc
{
    public enum MiscAction : byte
    {
        Extend = 0,
        UserTransfer = 1,
        MapBroadcast = 2,
        Max = 255,
    }
}
