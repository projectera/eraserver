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
using EraS.MessageHandlers;
using EraS.MessageHandlers.ErasComponents;
using Lidgren.Network;

namespace EraS
{
    class Router
    {
        public ServerListener Servers { get; protected set; }
        public ServiceListener Services { get; protected set; }
        public Network Network { get; protected set; }
        public String Identifier { get; protected set; }
        public MessageHandler ErasHandler { get; protected set; }

        protected Random _random;

        public Router()
        {
            _random = new Random();

            if (!HeartBeatService.Defibrillate())
                throw new ApplicationException("Heartbeat failed");
            Identifier = HeartBeatService.Identifier.ToString();
            Console.WriteLine("Router identifier: " + Identifier);

            Servers = new ServerListener(Identifier);
            Network = new Network(Identifier);

            ErasHandler = new MessageHandler();
            ErasHandler.Add(new NetworkComponent(Network));
            ErasHandler.Add(new StatisticsComponent());
            ErasHandler.Add(new SettingsComponent());
            ErasHandler.Add(new ServiceExchangeComponent(Network));

            Servers.OnActivate += OnActivate;
            Servers.OnConnect += OnServerConnect;
            Servers.OnDisconnect += OnServerDisconnect;

            Servers.RouteMessage = RouteMessage;
            Servers.MessageHandlers.Add(MessageType.EraS, ErasHandler.HandleMessage);

            Servers.Start();
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
            var s = new Server(c.RemoteIdentifier)
            {
                Connection = c,
            };
            lock (Network)
                Network.AddServer(s);

            Console.WriteLine("Connected to " + c.RemoteIdentifier);
        }

        protected void OnServerDisconnect(ServerConnection c)
        {
            Console.WriteLine("Disconnected from: " + c.RemoteIdentifier);

            lock (Network)
            {
                if (!Network.Servers.ContainsKey(c.RemoteIdentifier))
                    return;
                Network.RemoveServer(Network.Servers[c.RemoteIdentifier]);
            }
        }

        protected void BroadcastServers(Message msg)
        {
            List<Server> servers = null;
            lock (Network)
                servers = Network.Servers.Values.ToList();
            
            foreach (var s in servers)
                if(s.Identifier != Identifier)
                    s.Connection.SendMessage(msg);
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

            Message register = new Message(Servers.Peer.CreateMessage(32), MessageType.EraS, Identifier, "Self", 0);
            register.Packet.Write("ServiceExchange");
            register.Packet.Write("RegisterService");
            register.Packet.Write(con.RemoteIdentifier);
            register.Packet.Write(name);
            BroadcastServers(register);

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

            Message remove = new Message(Servers.Peer.CreateMessage(32), MessageType.EraS, Identifier, "Self", 0);
            remove.Packet.Write("ServiceExchange");
            remove.Packet.Write("RemoveService");
            remove.Packet.Write(con.RemoteIdentifier);
            BroadcastServers(remove);
        }

        protected void OnActivate()
        {
            Task.Factory.StartNew(() =>
            {
                // Find connected services on other servers
                List<String> servers;
                lock (Network)
                    servers = Network.Servers.Keys.ToList();

                foreach (var server in servers)
                {
                    if (server == Identifier)
                        continue;
                    try
                    {
                        NetworkInfo n = null;
                        lock (Network)
                            if (Network.Servers.ContainsKey(server))
                                n = new NetworkInfo(Network.Servers[server].Connection);

                        if (n == null)
                            continue;

                        var services = n.GetServerServices(server);
                        foreach (var service in services)
                        {
                            var name = n.GetServiceName(service);
                            if (name == null)
                                continue;

                            lock (Network)
                            {
                                if (!Network.Servers.ContainsKey(server))
                                    continue;
                                Server serv = Network.Servers[server];
                                Service s = new Service(serv, service) { Name = name, };

                                Network.AddService(s);
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        // If server didn't disconnect
                        lock(Network)
                            if(Network.Servers.ContainsKey(server))
                                Console.WriteLine("Server didn't respond: " + server);
                    }
                }

            });
            Task.Factory.StartNew(() => {

                // Start listening for services
                Services = new ServiceListener(Identifier);
                Services.OnConnect += OnServiceConnect;
                Services.OnDisconnect += OnServiceDisconnect;
                Services.MessageHandlers.Add(MessageType.EraS, ErasHandler.HandleMessage);
                Services.RouteMessage = RouteMessage;

                Services.Start();
            });
        }
    }
}
