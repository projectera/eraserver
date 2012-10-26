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
        /// Connect to the server
        /// </summary>
        /// <param name="serviceName">The servicename</param>
        /// <returns>A new ServiceClient</returns>
        public static ServiceClient Connect(string serviceName)
        {
            var conf = new NetPeerConfiguration("EraService");
            var client = new NetClient(conf);

            // Send version and service name
            var hail = client.CreateMessage();
            hail.Write(Version);
            hail.Write(serviceName);

            client.Connect(new IPEndPoint(IPAddress.Loopback, ServicePort), hail);

            // Wait for greeting and read identifier
            client.MessageReceivedEvent.WaitOne(2000);
            var greet = client.ReadMessage();
            if (greet == null)
                throw new TimeoutException("Server did not respond with needed info");
            var serviceClient = new ServiceClient(client, serviceName);
            serviceClient.Identifier = greet.ReadString();

            return serviceClient;
        }

        /// <summary>
        /// Creates a new ServiceClient, connects automatically to the server
        /// </summary>
        /// <param name="serviceName"></param>
        public ServiceClient(NetClient client, string serviceName) : base(client, client.ServerConnection)
        {
            ServiceName = serviceName;
        }
    }
}
