using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;
using ERA.Protocols.ServiceProtocol;
using Lidgren.Network;

namespace EraS.Connections
{
    public class ServiceConnection : MessageClient
    {
        public String Name { get; protected set; }

        public ServiceConnection(NetConnection connection, String identifier, String remoteIdentifier, String name) : base(connection, identifier, remoteIdentifier)
        {
            Name = name;
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
