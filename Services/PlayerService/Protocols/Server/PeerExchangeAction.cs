using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERAServer.Protocols.Server
{
    /// <summary>
    /// TODO: This will be replaced with NetworkManagement??
    /// </summary>
    public enum PeerExchangeAction : byte
    {
        None = 0,
        Introduce = 1,
    }
}
