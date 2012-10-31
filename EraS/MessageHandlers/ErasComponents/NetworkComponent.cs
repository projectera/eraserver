using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Connections;
using ServiceProtocol;
using EraS.Topography;

namespace EraS.MessageHandlers.ErasComponents
{
    class NetworkComponent : DefaultComponent
    {
        public Network Network { get; protected set; }

        public NetworkComponent(Network network) : base("Network")
        {
            Network = network;

            Functions.Add("GetServerIdentifier", GetServerIdentifier);
            Functions.Add("GetConnectedServers", GetConnectedServers);
            Functions.Add("GetServerServices", GetServerServices);
            Functions.Add("GetServerDisplayName", GetServerDisplayName);
            Functions.Add("GetServiceName", GetServiceName);
            Functions.Add("GetServiceServer", GetServiceServer);
            Functions.Add("GetServiceInstances", GetServiceInstances);
            Functions.Add("GetServices", GetServices);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        public void GetServices(MessageClient c, Message m)
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
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        public void GetServiceInstances(MessageClient c, Message m)
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
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        public void GetServiceServer(MessageClient c, Message m)
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
        protected void GetServerIdentifier(MessageClient c, Message m)
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
        protected void GetConnectedServers(MessageClient c, Message m)
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
        protected void GetServerServices(MessageClient c, Message m)
        {
            var id = m.Packet.ReadString();
            var ans = m.Answer(c);
            lock (Network)
            {
                if (!Network.Servers.ContainsKey(id))
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
        protected void GetServerDisplayName(MessageClient c, Message m)
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
        protected void GetServiceName(MessageClient c, Message m)
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
