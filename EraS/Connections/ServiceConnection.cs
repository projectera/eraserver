using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;
using ServiceProtocol;
using Lidgren.Network;

namespace EraS.Connections
{
    class ServiceConnection : MessageClient
    {
        public String RemoteIdentifier { get; protected set; }

        public ServiceConnection(NetConnection connection, String identifier, String remoteIdentifier) : base(connection.Peer, connection, identifier)
        {
            RemoteIdentifier = remoteIdentifier;
        }

        public void SendMessage()
        {
            throw new NotImplementedException();
        }

        public void Kill()
        {
            throw new NotImplementedException();
        }
    }
}
