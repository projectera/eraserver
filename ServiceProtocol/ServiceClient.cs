using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ERA.Protocols.ServiceProtocol
{
    /// <summary>
    /// Handles the connection to the EraServer
    /// </summary>
    public class ServiceClient : MessageClient
    {
        /// <summary>
        /// EraS port
        /// </summary>
        public const UInt16 ServerPort = 45246;
        
        /// <summary>
        /// The name of this service
        /// </summary>
        public String ServiceName { get; protected set; }

        /// <summary>
        /// Net Client
        /// </summary>
        public NetClient Client { get; protected set; }

        /// <summary>
        /// Service Thread
        /// </summary>
        protected Thread Thread { get; set; }

        /// <summary>
        /// Server reference
        /// </summary>
        public static MongoServer Server { get; protected set; }

        /// <summary>
        /// Database reference
        /// </summary>
        public static MongoDatabase Database { get; protected set; }

        protected static Object _serverSingletonLock = new Object();

        /// <summary>
        /// Is currently connected to the server
        /// </summary>
        public Boolean IsConnected
        {
            get
            {
                return Client != null && Client.ConnectionStatus == Lidgren.Network.NetConnectionStatus.Connected;
            }
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <param name="serviceName">The servicename</param>
        /// <returns>A new ServiceClient</returns>
        public static ServiceClient Connect(String serviceName, Boolean withDatabase = false)
        {
            var conf = new NetPeerConfiguration("EraService");
            var client = new NetClient(conf);
            client.Start();

            // Send version and service name
            var hail = client.CreateMessage();
            hail.Write(Version);
            hail.Write(serviceName);

            client.Connect(new IPEndPoint(IPAddress.Loopback, ServerPort), hail);

            // Wait for greeting and read identifier
            while (true)
            {
                client.MessageReceivedEvent.WaitOne(10 * 1000);
                var greet = client.ReadMessage();
               
                if (greet == null)
                    throw new TimeoutException("Server did not respond with needed info.");

                if (greet.MessageType != NetIncomingMessageType.StatusChanged)
                    continue;

                break;
            }

            if (client.ServerConnection == null || client.ServerConnection.Status != NetConnectionStatus.Connected)
                throw new InvalidOperationException("Server did not connect to this client.");

            // Get the identifiers
            var remid = client.ServerConnection.RemoteHailMessage.ReadString();
            var myid = client.ServerConnection.RemoteHailMessage.ReadString();

            var result = new ServiceClient(client, myid, serviceName, remid);

            if (withDatabase)
                result.ConnectToDatabase();
            return result;
        }

        /// <summary>
        /// Creates a new ServiceClient, connects automatically to the server
        /// </summary>
        /// <param name="serviceName"></param>
        public ServiceClient(NetClient client, String identifier, String serviceName, String remoteIdentifier) : base(client.ServerConnection, identifier, remoteIdentifier)
        {
            Client = client;
            ServiceName = serviceName;

            Thread = new Thread(Run);
            Thread.Start();
        }

        /// <summary>
        /// Connect to Mongo Database
        /// </summary>
        public void ConnectToDatabase()
        {
            lock (_serverSingletonLock)
            {
                if (Server != null)
                    return;

                // Get mongo and connect
                var settings = new SettingsInfo(this);
                var mongo = settings.GetMongo();

                Server = MongoServer.Create("mongodb://" + mongo.ToString());
                Database = Server.GetDatabase("era");

                Console.WriteLine("Connected to database: {0}", mongo);
            }
        }

        /// <summary>
        /// Stops the service
        /// </summary>
        public void Stop(String reason = null)
        {
            Thread.Abort();
            Client.Disconnect(reason ?? "Stopping the service.");
            base.RaiseOnConnectionClosed();
        }

        /// <summary>
        /// Runs the service loop
        /// </summary>
        protected virtual void Run()
        {
            while (IsConnected)
            {
                Client.MessageReceivedEvent.WaitOne(100);
                var msg = Client.ReadMessage();
                if (msg == null)
                    continue;

                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        if (Client.ConnectionStatus == NetConnectionStatus.Disconnected)
                            Stop();
                        break;
                    case NetIncomingMessageType.Data:
                        HandleMessage(new Message(msg));
                        break;
                }
            }
        }
    }
}
