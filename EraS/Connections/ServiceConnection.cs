using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;
using ServiceProtocol;
using Lidgren.Network;

namespace EraS.Connections
{
    public class ServiceConnection : MessageClient
    {
        public String RemoteIdentifier { get; protected set; }

        public ServiceConnection(NetConnection connection, String identifier, String remoteIdentifier) : base(connection, identifier)
        {
            RemoteIdentifier = remoteIdentifier;
        }

        public void Stop()
        {
            RaiseOnConnectionClosed();
        }

        public void Kill()
        {
            throw new NotImplementedException();
        }
    }
}
