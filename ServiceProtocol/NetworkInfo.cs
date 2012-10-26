using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    /// <summary>
    /// Gets info from the connected server
    /// </summary>
    class NetworkInfo
    {
        /// <summary>
        /// The socket used to connect to the server
        /// </summary>
        public ServiceClient Client { get; protected set; }

        /// <summary>
        /// Creates a new NetworkInfo object
        /// </summary>
        /// <param name="client">The client to use</param>
        public NetworkInfo(ServiceClient client)
        {
            Client = client;
        }

        /// <summary>
        /// Asks the identifier of the connected server
        /// </summary>
        /// <returns>The identifier of the connected server</returns>
        public string GetServerIdentifier()
        {
            var m = Client.CreateQuestion(MessageType.EraS, "Self");
            m.Packet.Write("GetIdentifier");
            var res = Client.AskReliableQuestion(m);
            return res.Packet.ReadString();
        }
    }
}
