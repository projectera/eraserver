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
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            _erasClient = ServiceClient.Connect("resource");
            Console.WriteLine("my id is:" + _erasClient.ServiceName);

            // Example question
            var q = _erasClient.CreateQuestion(MessageType.Service, "resource");
            q.Packet.Write("hi there!");
            var ans = _erasClient.AskQuestion(q);
            Console.WriteLine(ans.Packet.ReadString());

            // TODO: get mongo url from EraS
            Server = MongoServer.Create("mongodb://pegu.maxmaton.nl");
            Database = Server.GetDatabase("era");

            System.Threading.Thread.Sleep(1000 * 10);
        }
    }
}
