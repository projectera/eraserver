﻿using System;
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
        /// The list of running services by service name
        /// </summary>
        public Dictionary<String, List<Service>> ServiceGroupInstances { get; protected set; }

        /// <summary>
        /// The list of servers
        /// </summary>
        public List<Server> Servers { get; protected set; }

        /// <summary>
        /// Creates a new network representation
        /// </summary>
        public Network(String identifier)
        {
            ServiceInstances = new Dictionary<String, Service>();
            Servers = new List<Server>();

            Me = new Server(identifier);
            AddServer(Me);
        }

        public void AddServer(Server s)
        {
            Servers.Add(s);
            foreach (var service in s.Services)
            {
                ServiceInstances.Add(service.Identifier, service);
                ServiceGroupInstances[service.Name].Remove(service);
            }
        }

        public void RemoveServer(Server s)
        {
            Servers.Remove(s);
            foreach (var service in s.Services)
            {
                ServiceInstances.Remove(service.Identifier);
                if (!ServiceGroupInstances.ContainsKey(service.Name))
                    ServiceGroupInstances.Add(service.Name, new List<Service>());
                ServiceGroupInstances[service.Name].Add(service);
            }
        }

        public void AddService(Service s)
        {
            s.Server.Services.Add(s);
            ServiceInstances.Add(s.Identifier, s);

            if (!ServiceGroupInstances.ContainsKey(s.Name))
                ServiceGroupInstances.Add(s.Name, new List<Service>());
            ServiceGroupInstances[s.Name].Add(s);
        }
    }
}
