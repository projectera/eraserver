using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using Lidgren.Network;

namespace PlayerService
{
    class ClientConnection : MessageClient
    {
        public String Name { get; protected set; }

        public ClientConnection(NetConnection connection, String identifier, String remoteIdentifier, String name)
            : base(connection, identifier, remoteIdentifier)
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
