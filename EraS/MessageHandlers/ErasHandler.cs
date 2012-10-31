using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;
using ServiceProtocol;
using EraS.Connections;
using System.Threading.Tasks;
using EraS.Services;

namespace EraS.MessageHandlers
{
    /// <summary>
    /// Handles EraS Messages
    /// </summary>
    class ErasHandler
    {
        public Network Network { get; set; }
        public Dictionary<String, Action<ServiceConnection, Message>> Functions { get; protected set; }

        public ErasHandler(Network network)
        {
            Network = network;
            Functions = new Dictionary<String, Action<ServiceConnection, Message>>();

            #region Register Functions
            Functions.Add("GetServerIdentifier", GetServerIdentifier);
            Functions.Add("GetConnectedServers", GetConnectedServers);
            Functions.Add("GetServerServices", GetServerServices);
            Functions.Add("GetServerDisplayName", GetServerDisplayName);
            Functions.Add("GetServiceName", GetServiceName);
            Functions.Add("GetServiceServer", GetServiceServer);
            Functions.Add("GetServiceInstances", GetServiceInstances);
            Functions.Add("GetServices", GetServices);

            foreach(var function in HeartBeatService.Functions)
                Functions.Add(function.Key, function.Value);

            foreach (var function in StatisticsService.Functions)
                Functions.Add(function.Key, function.Value);
            #endregion
        }

        /// <summary>
        /// Processes a message
        /// </summary>
        /// <param name="c">The connection the message came from</param>
        /// <param name="m">The message</param>
        public void HandleMessage(ServiceConnection c, Message m)
        {
            try
            {
                var function = m.Packet.ReadString();
                lock (Functions)
                {
                    if (!Functions.ContainsKey(function))
                        return;
                    Functions[function](c, m);
                }
            }
            catch (Exception)
            {
                // TODO exception handling
            }
        }

        /// <summary>
        /// Get services topography
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        public void GetServices(ServiceConnection c, Message m)
        {
            var ans = m.Answer(c);

            lock (Network)
            {
                ans.Packet.Write(Network.ServiceGroupInstances.Keys.Count);
                foreach (var service in Network.ServiceGroupInstances.Keys)
                    ans.Packet.Write(service);
            }
            c.SendMessage(ans);
        }

        /// <summary>
        /// Get service instances topograpjhy
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        public void GetServiceInstances(ServiceConnection c, Message m)
        {
            var argument = m.Packet.ReadString();
            Message ans;

            lock (Network)
            {
                if (!Network.ServiceGroupInstances.ContainsKey(argument))
                    return;

                ans = m.Answer(c);
                var l = Network.ServiceGroupInstances[argument];
                ans.Packet.Write(l.Count);
                foreach (var id in l)
                    ans.Packet.Write(id.Identifier);
            }

            c.SendMessage(ans);
        }

        /// <summary>
        /// Get server of service
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        public void GetServiceServer(ServiceConnection c, Message m)
        {
            var argument = m.Packet.ReadString();
            Message ans;

            lock (Network)
            {
                if (!Network.ServiceInstances.ContainsKey(argument))
                    return;
                ans = m.Answer(c);

                ans.Packet.Write(Network.ServiceInstances[argument].Server.Identifier);
            }
            c.SendMessage(ans);
        }

        /// <summary>
        /// Returns the identifier of this host
        /// </summary>
        /// <param name="c">The connection this message came from</param>
        /// <param name="m">The message</param>
        protected void GetServerIdentifier(ServiceConnection c, Message m)
        {
            var ans = m.Answer(c);
            ans.Packet.Write(Network.Me.Identifier);
            c.SendMessage(ans);
        }

        /// <summary>
        /// Returns the connected server ids
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        protected void GetConnectedServers(ServiceConnection c, Message m)
        {
            var ans = m.Answer(c);
            lock (Network)
            {
                ans.Packet.Write(Network.Servers.Count);
                foreach (var s in Network.Servers.Values)
                    ans.Packet.Write(s.Identifier);
            }
            c.SendMessage(ans);
        }

        /// <summary>
        /// Returns the services connected to the specified server
        /// Has 1 argument, serverid
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        protected void GetServerServices(ServiceConnection c, Message m)
        {
            var id = m.Packet.ReadString();
            var ans = m.Answer(c);
            lock (Network)
            {
                if(!Network.Servers.ContainsKey(id))
                    return;

                Server s = Network.Servers[id];
                ans.Packet.Write(s.Services.Count);
                foreach (var service in s.Services.Keys)
                    ans.Packet.Write(service);
            }
            c.SendMessage(ans);
        }

        /// <summary>
        /// Returns the Displayname of a server
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        protected void GetServerDisplayName(ServiceConnection c, Message m)
        {
            var id = m.Packet.ReadString();
            var ans = m.Answer(c);
            lock (Network)
            {
                if (!Network.Servers.ContainsKey(id))
                    return;
                ans.Packet.Write(Network.Servers[id].DisplayName);
            }
            c.SendMessage(ans);
        }

        /// <summary>
        /// Returns the name of a service instance
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        protected void GetServiceName(ServiceConnection c, Message m)
        {
            var id = m.Packet.ReadString();
            var ans = m.Answer(c);
            lock (Network)
            {
                if (!Network.ServiceInstances.ContainsKey(id))
                    return;
                ans.Packet.Write(Network.ServiceInstances[id].Name);
            }
            c.SendMessage(ans);
        }
    }
}
