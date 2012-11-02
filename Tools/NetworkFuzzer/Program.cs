using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using Lidgren.Network;

namespace NetworkFuzzer
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = ServiceClient.Connect("NetworkFuzzer");
            NetworkInfo n = new NetworkInfo(client);

            List<string> strings = new List<string>(){
                "Network",
                "Self",
                "GetServices",
                "GetServiceInstances",
                "RegisterService",
                "GetServerDisplayName",
                "GetServerIdentifier",
                "GetConnectedServers",
            };

            var servs = n.GetConnectedServers();
            foreach (var i in servs)
            {
                strings.Add(i);
                var services = n.GetServerServices(i);
                foreach (var j in services)
                    strings.Add(j);
            }

            List<byte> bytes = new List<byte>()
            {
                (byte)ControlType.Kill,
                (byte)ControlType.Reserved,
                MessageClient.Version,
                0,
                1,
                2,
                3,
                4,
                5,
            };

            Random random = new Random();

            long count = 0;

            while (true)
            {
                NetOutgoingMessage m = client.Client.CreateMessage(32);
                
                //Semi-valid message header
                if (random.NextDouble() < 0.95)
                {
                    m.Write(bytes.OrderBy((x) => random.Next()).First());
                    m.Write(strings.OrderBy((x) => random.Next()).First());
                    m.Write(strings.OrderBy((x) => random.Next()).First());
                    m.Write(random.Next());
                }

                //Random data
                for (int i = 0; i < random.Next(8); i++)
                {
                    int type = random.Next(4);
                    switch (type)
                    {
                        case 0:
                            m.Write(bytes.OrderBy((x) => random.Next()).First());
                            break;
                        case 1:
                            m.Write(strings.OrderBy((x) => random.Next()).First());
                            break;
                        case 2:
                        case 3:
                            m.Write((byte)random.Next(256));
                            break;
                    }
                }
                client.Client.SendMessage(m, NetDeliveryMethod.ReliableUnordered);

                count++;
                if ((count % 1000) == 0)
                    Console.WriteLine(count);
            }
        }
    }
}
