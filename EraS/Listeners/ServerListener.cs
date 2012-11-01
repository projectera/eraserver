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
using System.Threading.Tasks;
using EraS.MessageHandlers;

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
        public MessageHandler ControlHandler { get; protected set; }

        public event Action<ServerConnection> OnConnect;
        public event Action<ServerConnection> OnDisconnect;
        public event Action OnActivate;

        public Action<Message> RouteMessage;

        public ServerListener(String identifier)
        {
            UnconnectedServers = new List<String>();
            MessageHandlers = new Dictionary<MessageType, Action<MessageClient, Message>>();
            Identifier = identifier;
        }

        public void Start()
        {
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
                if (server.ToString() == Identifier)
                    continue;

                NetOutgoingMessage m = Peer.CreateMessage(32);
                m.Write(Version);
                m.Write(Identifier);

                var con = new ServerConnection(Peer.Connect(servers[server]["IP"].AsString, ServerPort, m), Identifier, server.ToString());
                Console.WriteLine("Connecting to: " + server.ToString());
                con.Connection.Tag = con;

                UnconnectedServers.Add(server.ToString());
            }

            if (UnconnectedServers.Count == 0)
                Activate();

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
                if (msg.SenderConnection != null)
                    con = msg.SenderConnection.Tag as ServerConnection;

                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.WarningMessage:
                        Console.WriteLine(msg.ReadString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        switch (msg.SenderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                HandleConnect(con, msg);
                                break;
                            case NetConnectionStatus.Disconnected:
                                if (!IsActive && con != null)
                                {
                                    if (UnconnectedServers.Contains(con.RemoteIdentifier))
                                    {
                                        Console.WriteLine("Unable to become active, connection refused.");
                                        HeartBeatService.Cleanup();
                                        throw new ApplicationException();
                                    }
                                }

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
            Message m = new Message(msg);

            if (m.Destination.ToLower() != "self" && m.Destination != Identifier)
                RouteMessage(m);
            else
                ((ServerConnection)msg.SenderConnection.Tag).HandleMessage(m);
        }

        protected void HandleConnect(ServerConnection con, NetIncomingMessage msg)
        {
            if (OnConnect != null)
                OnConnect(con);

            if (!IsActive)
            {
                if (UnconnectedServers.Contains(con.RemoteIdentifier))
                    UnconnectedServers.Remove(con.RemoteIdentifier);

                if (UnconnectedServers.Count == 0)
                    Activate();
            }
        }

        protected virtual void OnConnectionApprove(ServerConnection con, NetIncomingMessage msg)
        {
            byte version = msg.ReadByte();
            if(version != Version)
            {
                msg.SenderConnection.Deny("Max version: " + Version.ToString());
                return;
            }
            var remid = msg.ReadString();

            NetOutgoingMessage m = Peer.CreateMessage(32);
            m.Write(Identifier);
            msg.SenderConnection.Approve(m);

            ServerConnection c = new ServerConnection(msg.SenderConnection, Identifier, remid);
            msg.SenderConnection.Tag = c;

            foreach (var key in MessageHandlers.Keys)
                c.MessageHandlers.Add(key, (x) => MessageHandlers[key](c, x));
        }

        protected void Activate()
        {
            IsActive = true;
            Console.WriteLine("Became active!");
            if (OnActivate != null)
                OnActivate();
        }
    }
}
