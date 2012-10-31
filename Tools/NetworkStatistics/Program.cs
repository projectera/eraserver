using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;

namespace NetworkStatistics
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = ServiceClient.Connect("NetworkStatistics");
            NetworkInfo n = new NetworkInfo(client);
        }
    }
}
