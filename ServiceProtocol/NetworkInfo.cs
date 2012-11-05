using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    /// <summary>
    /// Gets info from the connected server
    /// </summary>
    public class NetworkInfo : AbstractInfo
    {
        /// <summary>
        /// Creates a new NetworkInfo object
        /// </summary>
        /// <param name="client">The client to use</param>
        public NetworkInfo(MessageClient client)
            : base("Network", client)
        {
            Client = client;
        }

        /// <summary>
        /// Asks for the identifier of the connected server
        /// </summary>
        /// <returns>The identifier of the connected server</returns>
        public String GetServerIdentifier()
        {
            return GetString("GetServerIdentifier");
        }

        /// <summary>
        /// Get all the services
        /// </summary>
        /// <returns></returns>
        public List<String> GetServices()
        {
            return GetStringList("GetServices");
        }

        /// <summary>
        /// Get the service instances
        /// </summary>
        /// <param name="name">Service</param>
        /// <returns></returns>
        public List<String> GetServiceInstances(String name)
        {
            return GetStringList("GetServiceInstances", name);
        }

        /// <summary>
        /// Get the server where this service belongs to
        /// </summary>
        /// <param name="identifier">service id</param>
        /// <returns></returns>
        public String GetServiceServer(String identifier)
        {
            return GetString("GetServiceServer", identifier);
        }

        /// <summary>
        /// Queries the identifier of the servers connected to this server
        /// </summary>
        /// <returns>The list of servers</returns>
        public List<String> GetConnectedServers()
        {
            return GetStringList("GetConnectedServers");
        }

        /// <summary>
        /// Queries what services are connected to a certain server
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public List<String> GetServerServices(String identifier)
        {
            try
            {
                return GetStringList("GetServerServices", identifier);
            }
            catch (TimeoutException)
            {
                // Dropped
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public String GetServerDisplayName(String identifier)
        {
            return GetString("GetServerDisplayName", identifier);
        }

        /// <summary>
        /// Gets the name of a service instance or null on error
        /// </summary>
        /// <param name="identifier">The instance</param>
        /// <returns>The name of the service</returns>
        public String GetServiceName(String identifier)
        {
            try
            {
                return GetString("GetServiceName", identifier);
            }
            catch (TimeoutException)
            {
                return null;
            }
        }
    }
}
