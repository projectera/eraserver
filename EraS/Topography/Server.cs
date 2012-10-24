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
        List<Service> Services { get; protected set; }

        /// <summary>
        /// Wether the server is fenced (no active services, won't relay messages)
        /// </summary>
        Boolean IsFenced { get; set; }

        /// <summary>
        /// The unique identifier of the server
        /// This should be a hash
        /// </summary>
        String Identifier { get; protected set; }

        /// <summary>
        /// A printable version of the name
        /// </summary>
        String DisplayName { get; set; }

        ServerConnection Connection { get; set; }

        /// <summary>
        /// Creates a new server representation
        /// </summary>
        public Server(String identifier)
        {
            Services = new List<Service>();
            Identifier = identifier;
        }
    }
}
