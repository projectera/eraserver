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
            _erasClient = ServiceClient.Connect("Resource");
            Console.WriteLine("my id is:" + _erasClient.ServiceName);

            // TODO: get mongo url from EraS
            Server = MongoServer.Create("mongodb://localhost");
            Database = Server.GetDatabase("era");

            System.Threading.Thread.Sleep(1000 * 10);
        }
    }
}
