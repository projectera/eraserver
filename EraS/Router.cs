using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Listeners;
using EraS.Services;
using EraS.Topography;
using EraS.Connections;
using System.Threading.Tasks;
using ServiceProtocol;

namespace EraS
{
    class Router
    {
        public ServerListener Servers { get; protected set; }
        public ServiceListener Services { get; protected set; }
        public Network Network { get; protected set; }
        public String Identifier { get; protected set; }

        protected Random _random;

        public Router()
        {
            _random = new Random();

            if (!HeartBeatService.Defibrillate())
                throw new ApplicationException("Heartbeat failed");
            Identifier = HeartBeatService.Identifier.ToString();

            Servers = new ServerListener(Identifier);
            Network = new Network(Identifier);

            Servers.OnActivate += OnActivate;
            Servers.OnConnect += OnServerConnect;
            Servers.OnDisconnect += OnServerDisconnect;

            Servers.RouteMessage = RouteMessage;
        }

        protected void RouteMessage(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.Service:
                    RouteServiceMessage(msg);
                    break;
                case MessageType.Internal:
                    RouteInternalMessage(msg);
                    break;
                default:
                    // Drop
                    break;
            }
        }

        protected void RouteInternalMessage(Message msg)
        {
            MessageClient dst = null;
            lock (Network)
            {
                if (!Network.ServiceInstances.ContainsKey(msg.Destination))
                    return;

                if (Network.Me.Services.ContainsKey(msg.Destination))
                {
                    // Deliver locally
                    dst = Services.Connections[msg.Destination];
                }
                else
                {
                    // Deliver to other eras
                    dst = Network.ServiceInstances[msg.Destination].Server.Connection;
                }
            }
            dst.SendMessage(dst.CloneMessage(msg));
        }

        protected void RouteServiceMessage(Message msg)
        {
            MessageClient dst = null;
            lock (Network)
            {
                if (!Network.ServiceGroupInstances.ContainsKey(msg.Destination))
                    return;

                // Try finding this service locally
                foreach (var i in Network.ServiceGroupInstances[msg.Destination])
                {
                    if(i.Server == Network.Me)
                    {
                        dst = Services.Connections[i.Identifier];
                        break;
                    }
                }

                // No local service, take random one
                if (dst == null)
                    dst = Network.ServiceGroupInstances[msg.Destination].OrderBy((x) => _random.Next()).First().Server.Connection;
            }
            // And send
            dst.SendMessage(dst.CloneMessage(msg));
        }

        protected void OnServerConnect(ServerConnection c)
        {
            var s = new Server(c.RemoteIdentifier);
            lock (Network)
                Network.AddServer(s);

            Task.Factory.StartNew(() =>
            {
                // Get service info
                NetworkInfo n = new NetworkInfo(c);
                var services = n.GetServices();
                foreach (var service in services)
                {
                    var name = n.GetServiceName(service);
                    if (name == null)
                        continue;

                    lock (Network)
                        Network.AddService(new Service(s, service) { Name = name });
                }
            });
        }

        protected void OnServerDisconnect(ServerConnection c)
        {
            lock (Network)
            {
                if (!Network.Servers.ContainsKey(c.RemoteIdentifier))
                    return;
                Network.RemoveServer(Network.Servers[c.RemoteIdentifier]);
            }
        }

        protected void OnServiceConnect(ServiceConnection con, String name)
        {
            // Builds the network
            var s = new Service(Network.Me, con.RemoteIdentifier)
            {
                Name = name,
            };
            lock (Network)
                Network.AddService(s);

            Console.WriteLine("Service [" + name + "] approved.");
        }

        protected void OnServiceDisconnect(ServiceConnection con)
        {
            lock (Network)
            {
                var s = Network.ServiceInstances[con.RemoteIdentifier];
                Network.RemoveService(s);

                Console.WriteLine("Service [" + s.Name + "] disconnected.");
            }
        }

        protected void OnActivate()
        {
            Services = new ServiceListener(Identifier);
            Services.OnConnect += OnServiceConnect;
            Services.OnDisconnect += OnServiceDisconnect;
        }
    }
}
