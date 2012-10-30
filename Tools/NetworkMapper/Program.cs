using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;

namespace NetworkMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = ServiceClient.Connect("NetworkMapper");
            NetworkInfo n = new NetworkInfo(client);

            Console.WriteLine("Servers:");
            var servers = n.GetConnectedServers();
            foreach (var server in servers)
            {
                try
                {
                    var services = n.GetServerServices(server);
                    Console.WriteLine(" - " + server + " [" + n.GetServerDisplayName(server) + "]");
                    foreach (var service in services)
                    {
                        try
                        {
                            Console.WriteLine("    - " + service + " [" + n.GetServiceName(service) + "]");
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("    - " + service + " {Unavailable}");
                        }
                    }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(" - " + server + " {Unavailable}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Services:");

            var servicenames = n.GetServices();
            foreach (var name in servicenames)
            {
                Console.WriteLine(" - " + name);
                var instances = n.GetServiceInstances(name);
                foreach (var instance in instances)
                {
                    var server = n.GetServiceServer(instance);
                    Console.WriteLine("    - " + instance + " [" + server + "@" + n.GetServerDisplayName(server) + "]");
                }
            }

            client.Stop();

            Console.ReadKey();
        }
    }
}
