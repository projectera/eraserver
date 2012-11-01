using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;
using ServiceProtocol;

namespace EraS.MessageHandlers.ErasComponents
{
    class ServiceExchangeComponent : DefaultComponent
    {
        public Network Network { get; protected set; }

        public ServiceExchangeComponent(Network network) : base("ServiceExchange")
        {
            Network = network;
            Functions.Add("RegisterService", RegisterService);
            Functions.Add("RemoveService", RemoveService);
        }

        public void RegisterService(MessageClient con, Message msg)
        {
            var server = msg.Origin;
            var serviceid = msg.Packet.ReadString();
            var servicename = msg.Packet.ReadString();
            Console.WriteLine("Received register " + servicename + " from " + server);
            lock (Network)
            {
                if (!Network.Servers.ContainsKey(server))
                    return;

                Network.AddService(new Service(Network.Servers[server], serviceid) { Name = servicename, });
            }
        }

        public void RemoveService(MessageClient con, Message msg)
        {
            var serviceid = msg.Packet.ReadString();
            lock (Network)
            {
                if (!Network.ServiceInstances.ContainsKey(serviceid))
                    return;

                Network.RemoveService(Network.ServiceInstances[serviceid]);
            }
        }
    }
}
