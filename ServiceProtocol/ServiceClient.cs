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

        public NetClient Client { get; protected set; }
        protected Thread Thread { get; set; }

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
            client.MessageReceivedEvent.WaitOne(10000);
            var greet = client.ReadMessage();
            if (greet == null)
                throw new TimeoutException("Server did not respond with needed info");
            var serviceClient = new ServiceClient(client, serviceName, Encoding.UTF8.GetString(client.ServerConnection.RemoteHailMessage.Data));

            return serviceClient;
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

        public void Stop()
        {
            Thread.Abort();
            Client.Disconnect("");
            base.RaiseOnConnectionClosed();
        }

        protected virtual void Run()
        {
            while (true)
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
