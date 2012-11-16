using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERA.Protocols.ServiceProtocol;
using Lidgren.Network;

namespace EraS.Connections
{
    class ServerConnection : MessageClient
    {
        public ServerConnection(NetConnection connection, String identifier, String remoteIdentifier)
            : base(connection, identifier, remoteIdentifier)
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
