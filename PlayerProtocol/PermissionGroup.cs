using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERA.Protocols.PlayerProtocol
{
    /// <summary>
    /// Permission Groups
    /// </summary>
    [Flags]
    public enum PermissionGroup : byte
    {
        None = 0,
        Registered = (1 << 0),
        AlphaTester = (1 << 1),
        BetaTester = (1 << 2),
        PowerUser = (1 << 3),
        GameObserver = (1 << 4),
        GameMaster = (1 << 5),
        God = (1 << 6),
        Founder = (1 << 7),
    }
}