using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using MongoDB.Driver;

namespace ResourceService
{
    class Program
    {
        static ServiceClient _erasClient;

        /// <summary>
        /// Server reference
        /// </summary>
        public static MongoServer Server { get; protected set; }

        /// <summary>
        /// Database reference
        /// </summary>
        public static MongoDatabase Database { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public static NetworkInfo NetworkInfo { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            System.Threading.Thread.Sleep(4000);

            _erasClient = ServiceClient.Connect("Resource");
            Console.WriteLine("my id is:" + _erasClient.ServiceName);

            var q = _erasClient.CreateQuestion(MessageType.EraS, "Self");
            q.Packet.Write("GetMongo");
            var a = _erasClient.AskReliableQuestion(q); 
            var host = a.Packet.ReadString();
            var port = a.Packet.ReadInt32();
            Server = MongoServer.Create("mongodb://" + new MongoServerAddress(host, port).ToString());
            Database = Server.GetDatabase("era");

            NetworkInfo = new ServiceProtocol.NetworkInfo(_erasClient);
            System.Threading.Thread.Sleep(1000 * 10);
        }
    }
}
