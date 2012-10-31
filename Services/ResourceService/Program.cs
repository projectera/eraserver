using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using MongoDB.Driver;

namespace ResourceService
{
    partial class Program
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
        public static Boolean IsRunning { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Startup delay
            var delay = 4000;
            if (args.Contains("-d"))
                delay = Int32.Parse(args.SkipWhile(a => a != "-d").Skip(1).First());
            Console.WriteLine("Service starting [{0} ms].", delay);
            System.Threading.Thread.Sleep(delay);

            // Functions
            RegisterFunctions();

            // Connect to the cloud
            _erasClient = ServiceClient.Connect("Resource");
            _erasClient.MessageHandlers.Add(MessageType.Service, HandleMessages);
            Console.WriteLine("Connected with Id: {0}", _erasClient.ServiceName);

            // Get mongo and connect
            var q = _erasClient.CreateQuestion(MessageType.EraS, "Self");
            q.Packet.Write("GetMongo");
            
            var mongo = _erasClient.AskReliableQuestion(q);
            var host = mongo.Packet.ReadString();
            var port = mongo.Packet.ReadInt32();

            Server = MongoServer.Create("mongodb://" + new MongoServerAddress(host, port).ToString());
            Database = Server.GetDatabase("era");
            Console.WriteLine("Connected to database: {0}:{1}", host, port);
            IsRunning = true;

            // Save the network info
            NetworkInfo = new ServiceProtocol.NetworkInfo(_erasClient);

            while (_erasClient.IsConnected && IsRunning)
                System.Threading.Thread.Sleep(1000);

            Console.WriteLine("Service terminated.");
        }
    }
}
