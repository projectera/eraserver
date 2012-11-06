using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Threading;
using ServiceProtocol;

namespace PlayerService
{
    class ClientListener
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
        protected Int32 _clientCounter { get; set; }

        /// <summary>
        /// All the service connections by service name
        /// </summary>
        public Dictionary<String, ClientConnection> Connections { get; protected set; }

        /// <summary>
        /// Message handlers handle all the messages not handled in ServiceConnection.
        /// They are assigned when the a new service connection is approved.
        /// </summary>
        public Action<ClientConnection, Message> MessageHandler { get; protected set; }

        /// <summary>
        /// Runs when a connection is made
        /// </summary>
        public event Action<ClientConnection, String> OnConnect;

        /// <summary>
        /// Runs when a connection is broken
        /// </summary>
        public event Action<ClientConnection> OnDisconnect;

        /// <summary>
        /// Creates the clientListener
        /// </summary>
        /// <param name="identifier">The instance identifier</param>
        public ClientListener()
        {
            Connections = new Dictionary<String, ClientConnection>();
            MessageHandler = delegate { };
            OnConnect = delegate { };
            OnDisconnect = delegate { };
        }

        /// <summary>
        /// Starts listening
        /// </summary>
        public void Start()
        {
            var conf = new NetPeerConfiguration("EraClient")
            {
                Port = 45247, //Client.ServerPort,
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
                Server.MessageReceivedEvent.WaitOne();

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
                            ((ClientConnection)m.SenderConnection.Tag).Stop();
                        break;

                    case NetIncomingMessageType.Data:
                        OnData(m);
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        protected void OnData(NetIncomingMessage msg)
        {
            try
            {
                ((ClientConnection)msg.SenderConnection.Tag).HandleMessage(new Message(msg));
            }
            catch (NetException) {
                Console.WriteLine("Malformed package received from: " + ((ClientConnection)msg.SenderConnection.Tag).RemoteIdentifier);
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

            String username = m.ReadString();

            // Remote hail message with our and their internal ids
            var outmsg = Server.CreateMessage(32);

            // TODO get userId;
            //String remoteid = userid;
            //outmsg.Write(remoteid);
            
            // Create the service handler
            var handler = new ClientConnection(m.SenderConnection, null, null, username);
            handler.OnConnectionClosed += () => OnDisconnect(handler);
            OnConnect.Invoke(handler, username);

            // Assign message handler
            handler.MessageHandlers.Add(MessageType.Client, (msg) => MessageHandler(handler, msg));
            
            m.SenderConnection.Tag = handler;
            Connections.Add(handler.Identifier, handler);
            m.SenderConnection.Approve(outmsg);
        }
    }
}
