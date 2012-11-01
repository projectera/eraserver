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
        /// <summary>
        /// Lidgren NetServer for the listener
        /// </summary>
        public NetServer Server { get; protected set; }

        /// <summary>
        /// Internal listening thread
        /// </summary>
        protected Thread Thread { get; set; }

        /// <summary>
        /// Gets the status of the service listener
        /// </summary>
        public Boolean IsRunning { get { return Thread.IsAlive; } }

        /// <summary>
        /// Server (instance) Identifier
        /// </summary>
        public String Identifier { get; protected set; }
        protected Int32 _serviceCounter { get; set; }

        /// <summary>
        /// All the service connections by service name
        /// </summary>
        public Dictionary<String, ServiceConnection> Connections { get; protected set; }

        /// <summary>
        /// Message handlers handle all the messages not handled in ServiceConnection.
        /// They are assigned when the a new service connection is approved.
        /// </summary>
        public Dictionary<MessageType, Action<ServiceConnection, Message>> MessageHandlers { get; protected set; }

        /// <summary>
        /// Runs when a connection is made
        /// </summary>
        public event Action<ServiceConnection, String> OnConnect;

        /// <summary>
        /// Runs when a connection is broken
        /// </summary>
        public event Action<ServiceConnection> OnDisconnect;

        public Action<Message> RouteMessage;

        /// <summary>
        /// Creates the servicelistener
        /// </summary>
        /// <param name="identifier">The instance identifier</param>
        public ServiceListener(String identifier)
        {
            Identifier = identifier;
            Connections = new Dictionary<string, ServiceConnection>();
            MessageHandlers = new Dictionary<MessageType, Action<ServiceConnection, Message>>();
            OnConnect = delegate { };
            OnDisconnect = delegate { };
        }

        public void Start()
        {
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

        /// <summary>
        /// Runs the thread
        /// </summary>
        protected void Run()
        {
            while (IsRunning)
            {
                var m = Server.ReadMessage();
                if (m == null)
                {
                    System.Threading.Thread.Sleep(10);
                    continue;
                }

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
                        OnData(m);
                        break;
                }
            }
        }

        protected void OnData(NetIncomingMessage msg)
        {
            try
            {
                var m = new Message(msg);
                if (m.Destination.ToLower() != "self" && m.Destination != Identifier)
                    RouteMessage(m);
                else
                    ((ServiceConnection)msg.SenderConnection.Tag).HandleMessage(m);
            }
            catch (NetException) {
                Console.WriteLine("Malformed package received from: " + ((ServiceConnection)msg.SenderConnection.Tag).RemoteIdentifier);
            }
        }

        /// <summary>
        /// Approves a new connection
        /// </summary>
        /// <param name="m">Incomming hail</param>
        protected void ApproveConnection(NetIncomingMessage m)
        {
            // Deny wrong versions
            Byte clientversion = m.ReadByte();
            if (clientversion > MessageClient.Version)
                m.SenderConnection.Deny("Max. version: " + MessageClient.Version.ToString());

            String servicename = m.ReadString();

            // Remote hail message with our and their internal ids
            var outmsg = Server.CreateMessage(32);
            outmsg.Write(Identifier);
            String remoteid = Identifier + "-" + (_serviceCounter++).ToString();
            outmsg.Write(remoteid);
            
            // Create the service handler
            var handler = new ServiceConnection(m.SenderConnection, Identifier, remoteid, servicename);
            handler.OnConnectionClosed += () => OnDisconnect(handler);
            OnConnect.Invoke(handler, servicename);

            // Assign message handlers
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
