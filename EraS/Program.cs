using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using EraS.Listeners;
using EraS.Topography;
using EraS.MessageHandlers;
using EraS.MessageHandlers.ErasComponents;
using EraS.Services;

namespace EraS
{
    class Program
    {
        public static ServiceListener Services { get; protected set; }
        public static ServerListener Servers { get; protected set; }
        public static Network Network { get; protected set; }
        public static Boolean IsRunning { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("EraS starting.");
            if (HeartBeatService.Defibrillate())
                IsRunning = true;


            Network = new Network(HeartBeatService.Identifier.ToString());

            Servers = new ServerListener(HeartBeatService.Identifier.ToString());
            Servers.OnConnect += (con) =>
            {
                var s = new Server(con.RemoteIdentifier);
                lock(Network)
                    Network.AddServer(s);
                Console.WriteLine("Connection established: " + con.RemoteIdentifier);
            };
            Servers.OnDisconnect += (c) =>
            {
                Console.WriteLine("Disconnected: " + c.RemoteIdentifier);
                lock (Network)
                {
                    if (!Network.Servers.ContainsKey(c.RemoteIdentifier))
                        return;
                    Server s = Network.Servers[c.RemoteIdentifier];
                    Network.RemoveServer(s);
                }
            };

            Services = new ServiceListener(HeartBeatService.Identifier.ToString());
            Services.OnConnect += (con, name) =>
            {
                try
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
                catch (Exception)
                {
                    // TODO exception reports
                }
            };

            Services.OnDisconnect += (con) =>
            {
                try
                {
                    lock (Network)
                    {
                        var s = Network.ServiceInstances[con.RemoteIdentifier];
                        Network.RemoveService(s);

                        Console.WriteLine("Service [" + s.Name + "] disconnected.");
                    }
                }
                catch (Exception)
                {
                    // TODO exception reports
                }
            };


            StatisticsService.Start();
            Console.WriteLine("Service listener started.");

            MessageHandler h = new MessageHandler();
            h.Add(new NetworkComponent(Network));
            h.Add(new StatisticsComponent());
            h.Add(new SettingsComponent());

            Services.MessageHandlers.Add(MessageType.EraS, h.HandleMessage);

            while (!HeartBeatService.HasFlatlined && IsRunning)
                System.Threading.Thread.Sleep(1000);

            Console.WriteLine("Service listener stopped.");
            Console.WriteLine("EraS stopped.");
        }
    }
}
