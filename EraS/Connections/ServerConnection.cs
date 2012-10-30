using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using Lidgren.Network;

namespace EraS.Connections
{
    class ServerConnection : MessageClient
    {
        public ServerConnection(NetConnection connection, String identifier)
            : base(connection, identifier)
        {

        }

        /// <summary>
        /// Fences the server
        /// This will result in a restart of EraS (while disconnecting all services)
        /// </summary>
        public void Fence()
        {
            throw new NotImplementedException();
        }
    }
}
