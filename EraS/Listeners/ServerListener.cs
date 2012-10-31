using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using EraS.Topography;
using System.Threading;
using EraS.Connections;
using EraS.Services;

namespace EraS.Listeners
{
    class ServerListener
    {
        public const UInt16 ServerPort = 38237;
        public NetPeer Peer { get; protected set; }
        public Network Network { get; protected set; }
        public Thread Thread { get; protected set; }
        public Boolean IsActive { get; protected set; }

        public List<String> UnconnectedServers { get; protected set; }

        public ServerListener(Network network)
        {
            Network = network;
            UnconnectedServers = new List<String>();

            var conf = new NetPeerConfiguration("EraServer")
            {
                Port = ServerPort,
                AcceptIncomingConnections = true,
            };
            conf.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            Peer.Start();

            var servers = HeartBeatService.GetServers();
            foreach (var server in servers.Keys)
            {
                var nets = new Server(server.ToString());
                var con = new ServerConnection(Peer.Connect(servers[server]["IP"].AsString, ServerPort), server.ToString());
                nets.Connection = con;
                con.Connection.Tag = con;
                
                UnconnectedServers.Add(server.ToString());
                Network.AddServer(nets);
            }

            Thread = new Thread(Run);
            Thread.Start();
        }

        protected virtual void Run()
        {
            while (true)
            {
                var msg = Peer.WaitMessage(100);
                if (msg == null)
                    continue;

                var con = msg.SenderConnection.Tag as ServerConnection;

                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        switch(msg.SenderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                OnConnect(con, msg);
                                break;
                            case NetConnectionStatus.Disconnected:
                                OnDisconnect(con, msg);
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        OnData(con, msg);
                        break;
                    case NetIncomingMessageType.ConnectionApproval:
                        OnConnectionApprove(con, msg);
                        break;
                }
            }
        }

        protected virtual void OnConnect(ServerConnection con, NetIncomingMessage msg)
        {
            if (IsActive)
                return;
            if (UnconnectedServers.Contains(con.Identifier))
                UnconnectedServers.Remove(con.Identifier);

            if (UnconnectedServers.Count == 0)
                Activate();
        }

        protected virtual void OnDisconnect(ServerConnection con, NetIncomingMessage msg)
        {

        }

        protected virtual void OnData(ServerConnection con, NetIncomingMessage msg)
        {

        }

        protected virtual void OnConnectionApprove(ServerConnection con, NetIncomingMessage msg)
        {
            throw new NotImplementedException();
        }

        protected void Activate()
        {
            IsActive = true;
        }
    }
}
