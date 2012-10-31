using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProtocol
{
    /// <summary>
    /// Handles the connection to the EraServer
    /// </summary>
    public class ServiceClient : MessageClient
    {
        public const UInt16 ServicePort = 45246;
        
        /// <summary>
        /// The name of this service
        /// </summary>
        public String ServiceName { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public NetClient Client { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        protected Thread Thread { get; set; }

        /// <summary>
        /// 
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
        public static ServiceClient Connect(string serviceName)
        {
            var conf = new NetPeerConfiguration("EraService");
            var client = new NetClient(conf);
            client.Start();

            // Send version and service name
            var hail = client.CreateMessage();
            hail.Write(Version);
            hail.Write(serviceName);

            client.Connect(new IPEndPoint(IPAddress.Loopback, ServicePort), hail);

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

            return new ServiceClient(client, serviceName, client.ServerConnection.RemoteHailMessage.ReadString());
        }

        /// <summary>
        /// Creates a new ServiceClient, connects automatically to the server
        /// </summary>
        /// <param name="serviceName"></param>
        public ServiceClient(NetClient client, String identifier, String serviceName) : base(client.ServerConnection, identifier)
        {
            Client = client;
            ServiceName = serviceName;

            Thread = new Thread(Run);
            Thread.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop(String reason = null)
        {
            Thread.Abort();
            Client.Disconnect(reason ?? "Stopping the service.");
            base.RaiseOnConnectionClosed();
        }

        /// <summary>
        /// 
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
