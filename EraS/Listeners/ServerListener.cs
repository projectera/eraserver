using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using EraS.Topography;
using System.Threading;
using EraS.Connections;
using EraS.Services;
using ServiceProtocol;

namespace EraS.Listeners
{
    class ServerListener
    {
        public const UInt16 ServerPort = 38237;
        public const Byte Version = 1;

        public NetPeer Peer { get; protected set; }
        public Thread Thread { get; protected set; }
        public Boolean IsActive { get; protected set; }
        public String Identifier { get; protected set; }

        public List<String> UnconnectedServers { get; protected set; }

        public Dictionary<MessageType, Action<MessageClient, Message>> MessageHandlers { get; protected set; }

        public event Action<ServerConnection> OnConnect;
        public event Action<ServerConnection> OnDisconnect;

        public ServerListener(String identifier)
        {
            UnconnectedServers = new List<String>();
            MessageHandlers = new Dictionary<MessageType, Action<MessageClient, Message>>();
            Identifier = identifier;

            var conf = new NetPeerConfiguration("EraServer")
            {
                Port = ServerPort,
                AcceptIncomingConnections = true,
            };
            conf.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            Peer = new NetPeer(conf);
            Peer.Start();

            var servers = HeartBeatService.GetServers();
            foreach (var server in servers.Keys)
            {
                var nets = new Server(server.ToString());

                NetOutgoingMessage m = Peer.CreateMessage(32);
                m.Write(Identifier);

                var con = new ServerConnection(Peer.Connect(servers[server]["IP"].AsString, ServerPort, m), identifier, server.ToString());
                Console.WriteLine("Connecting to: " + servers[server]["IP"].AsString);
                nets.Connection = con;
                con.Connection.Tag = con;
                
                UnconnectedServers.Add(server.ToString());
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


                ServerConnection con = null;
                if(msg.SenderConnection != null)
                     con = msg.SenderConnection.Tag as ServerConnection;

                Console.WriteLine(msg.MessageType.ToString());

                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                        Console.WriteLine(msg.ReadString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        switch (msg.SenderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                if (OnConnect != null)
                                    OnConnect(con);

                                if (IsActive)
                                    break;
                                if (UnconnectedServers.Contains(con.Identifier))
                                    UnconnectedServers.Remove(con.Identifier);

                                if (UnconnectedServers.Count == 0)
                                    Activate();
                                break;
                            case NetConnectionStatus.Disconnected:
                                if(OnDisconnect != null)
                                    OnDisconnect(con);
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

        protected virtual void OnData(ServerConnection con, NetIncomingMessage msg)
        {
            //TODO: Add routing

            Message m = new Message(msg);
            if (!MessageHandlers.ContainsKey(m.Type))
                return;

            MessageHandlers[m.Type](con, m);
        }

        protected virtual void OnConnectionApprove(ServerConnection con, NetIncomingMessage msg)
        {
            byte version = msg.ReadByte();
            if(version != Version)
            {
                con.Connection.Deny("Max version: " + Version.ToString());
                return;
            }
            var remid = msg.ReadString();

            NetOutgoingMessage m = Peer.CreateMessage(32);
            m.Write(Identifier);
            con.Connection.Approve(m);
        }

        protected void Activate()
        {
            IsActive = true;
        }
    }
}
