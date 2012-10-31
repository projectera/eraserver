using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    /// <summary>
    /// Gets info from the connected server
    /// </summary>
    public class NetworkInfo
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
        /// Asks for the identifier of the connected server
        /// </summary>
        /// <returns>The identifier of the connected server</returns>
        public String GetServerIdentifier()
        {
            return GetString("GetServerIdentifier");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<String> GetServices()
        {
            return GetStringList("GetServices");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<String> GetServiceInstances(String name)
        {
            return GetStringList("GetServiceInstances", name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public String GetServiceServer(String identifier)
        {
            return GetString("GetServiceServer", identifier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        protected String GetString(String function)
        {
            var m = Client.CreateQuestion(MessageType.EraS, "Self");
            m.Packet.Write("Network");
            m.Packet.Write(function);
            var res = Client.AskQuestion(m);
            return res.Packet.ReadString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="function"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        protected String GetString(String function, String argument)
        {
            var m = Client.CreateQuestion(MessageType.EraS, "Self");
            m.Packet.Write("Network");
            m.Packet.Write(function);
            m.Packet.Write(argument);
            var res = Client.AskQuestion(m);
            return res.Packet.ReadString();
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
        /// 
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        protected List<String> GetStringList(String function)
        {
            var m = Client.CreateQuestion(MessageType.EraS, "Self");
            m.Packet.Write("Network");
            m.Packet.Write(function);
            var res = Client.AskQuestion(m);

            Int32 count = res.Packet.ReadInt32();
            var l = new List<String>(count);
            for (int i = 0; i < count; i++)
                l.Add(res.Packet.ReadString());

            return l;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="function"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        protected List<String> GetStringList(String function, String argument)
        {
            var m = Client.CreateQuestion(MessageType.EraS, "Self");
            m.Packet.Write("Network");
            m.Packet.Write(function);
            m.Packet.Write(argument);
            var res = Client.AskQuestion(m);

            Int32 count = res.Packet.ReadInt32();
            var l = new List<String>(count);
            for (int i = 0; i < count; i++)
                l.Add(res.Packet.ReadString());

            return l;
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
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public String GetServiceName(String identifier)
        {
            return GetString("GetServiceName", identifier);
        }
    }
}
