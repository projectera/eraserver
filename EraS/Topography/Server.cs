using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Connections;

namespace EraS.Topography
{
    /// <summary>
    /// A representation of a connected
    /// </summary>
    class Server
    {
        /// <summary>
        /// The list of services connected to this server
        /// </summary>
        public Dictionary<String, Service> Services { get; protected set; }

        /// <summary>
        /// Wether the server is fenced (no active services, won't relay messages)
        /// </summary>
        public Boolean IsFenced { get; set; }

        /// <summary>
        /// The unique identifier of the server
        /// This should be a hash
        /// </summary>
        public String Identifier { get; protected set; }

        /// <summary>
        /// A printable version of the name
        /// </summary>
        public String DisplayName { get; set; }

        /// <summary>
        /// The connection to that server
        /// </summary>
        public ServerConnection Connection { get; set; }

        /// <summary>
        /// Creates a new server representation instance
        /// </summary>
        /// <param name="identifier">The unique identifier (hash)</param>
        public Server(String identifier)
        {
            Services = new Dictionary<String, Service>();
            Identifier = identifier;
        }
    }
}
