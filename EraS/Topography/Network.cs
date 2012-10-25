using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EraS.Topography
{
    /// <summary>
    /// A representation of the current (known) network
    /// </summary>
    class Network
    {
        /// <summary>
        /// My representation
        /// </summary>
        public Server Me { get; protected set; }

        /// <summary>
        /// The list of running service instances
        /// </summary>
        public Dictionary<String, Service> ServiceInstances { get; protected set; }

        /// <summary>
        /// The list of servers
        /// </summary>
        public List<Server> Servers { get; protected set; }

        /// <summary>
        /// Creates a new network representation
        /// </summary>
        public Network()
        {
            ServiceInstances = new Dictionary<String, Service>();
            Servers = new List<Server>();
        }
    }
}
