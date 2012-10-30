using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using Lidgren.Network;
using System.Threading.Tasks;
using EraS.Connections;
using System.Threading;

namespace EraS.Listeners
{
    class ServiceListener
    {
        public NetServer Server { get; protected set; }
        protected Thread Thread { get; set; }
        public String Identifier { get; protected set; }
        protected Int32 _serviceCounter { get; set; }
        public Dictionary<String, ServiceConnection> Connections { get; protected set; }
        public Dictionary<MessageType, Action<ServiceConnection, Message>> MessageHandlers { get; protected set; }

        public Action<ServiceConnection, String> OnConnect { get; set; }
        public Action<ServiceConnection> OnDisconnect { get; set; }

        public ServiceListener(String identifier)
        {
            Identifier = identifier;
            Connections = new Dictionary<string, ServiceConnection>();
            MessageHandlers = new Dictionary<MessageType, Action<ServiceConnection, Message>>();
            OnConnect = (_, name) => { };
            OnDisconnect = (_) => { };

            var conf = new NetPeerConfiguration("EraService")
            {
                Port = ServiceClient.ServicePort,
            };
            conf.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            Server = new NetServer(conf);
            Server.Start();

            Thread = new Thread(Run);
            Thread.Start();
        }

        protected void Run()
        {
            while (true)
            {
                var m = Server.ReadMessage();
                if (m == null)
                    continue;

                switch (m.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        ApproveConnection(m);
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        if (m.SenderConnection.Status == NetConnectionStatus.Disconnected)
                            ((ServiceConnection)m.SenderConnection.Tag).Stop();
                        break;
                    case NetIncomingMessageType.Data:
                        ((ServiceConnection)m.SenderConnection.Tag).HandleMessage(new Message(m));
                        break;
                }
            }
        }

        protected void ApproveConnection(NetIncomingMessage m)
        {
            byte clientversion = m.ReadByte();
            if (clientversion > MessageClient.Version)
                m.SenderConnection.Deny("Max. version: " + MessageClient.Version.ToString());

            string servicename = m.ReadString();
            var outmsg = Server.CreateMessage(32);

            string remoteid = Identifier + "-" + (_serviceCounter++).ToString();
            outmsg.Write(remoteid);
            var handler = new ServiceConnection(m.SenderConnection, Identifier, remoteid);
            handler.OnConnectionClosed += () => OnDisconnect(handler);
            OnConnect(handler, servicename);

            foreach(var type in MessageHandlers.Keys)
            {
                var func = MessageHandlers[type];
                handler.MessageHandlers.Add(type, (msg) => func(handler, msg));
            }

            m.SenderConnection.Tag = handler;
            Connections.Add(remoteid, handler);
            m.SenderConnection.Approve(outmsg);
        }
    }
}
