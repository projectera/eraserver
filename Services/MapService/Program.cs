using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using MongoDB.Driver;
using MongoDB.Bson;

namespace MapService
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
        /// Network Information
        /// </summary>
        public static NetworkInfo NetworkInfo { get; protected set; }

        /// <summary>
        /// Service is Running
        /// </summary>
        public static Boolean IsRunning { get; set; }

        /// <summary>
        /// MapId { InstanceId => Instance}
        /// </summary>
        public static Dictionary<ObjectId, Dictionary<ObjectId, Data.MapInstance>> MapInstances { get; protected set; }
        
        /// <summary>
        /// 
        /// </summary>
        public static Subscriptions MapSubscriptions { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Startup delay
            var delay = 500;
            if (args.Contains("-d"))
                delay = Int32.Parse(args.SkipWhile(a => a != "-d").Skip(1).First());
            Console.WriteLine("Service starting [{0} ms].", delay);
            System.Threading.Thread.Sleep(delay);

            // Functions
            RegisterFunctions();

            // Connect to the cloud
            _erasClient = ServiceClient.Connect("Map");
            Console.WriteLine("Connected with Id: {0}", _erasClient.ServiceName);
            MapSubscriptions = new Subscriptions(_erasClient);
            MapInstances = new Dictionary<ObjectId, Dictionary<ObjectId, Data.MapInstance>>();

            // Get mongo and connect
            var settings = new SettingsInfo(_erasClient);
            var mongo = settings.GetMongo();

            Server = MongoServer.Create("mongodb://" + mongo.ToString());
            Database = Server.GetDatabase("era");
            Console.WriteLine("Connected to database: {0}", mongo);

            // Save the network info
            NetworkInfo = new NetworkInfo(_erasClient);
            StartRunningMaps();

            // Messages and start running
            _erasClient.MessageHandlers.Add(MessageType.Service, HandleMessages);
            IsRunning = true;

            while (_erasClient.IsConnected && IsRunning)
                System.Threading.Thread.Sleep(1000);

            Console.WriteLine("Service terminated.");
        
        }

        /// <summary>
        /// Starts running maps
        /// </summary>
        private static void StartRunningMaps()
        {
            var mapservices = NetworkInfo.GetServiceInstances("Map");
            var maps = new List<ObjectId>();

            // TODO make a broadcast type
            foreach (var mapservice in mapservices)
            {
                if (mapservice == NetworkInfo.Client.Identifier)
                    continue;

                try
                {
                    var question = _erasClient.CreateQuestion(MessageType.Internal, mapservice);
                    question.Packet.Write("GetRunning");
                    var answer = _erasClient.AskQuestion(question);
                    var count = answer.Packet.ReadInt32();
                    for (Int32 i = 0; i < count; i++)
                        maps.Add(new ObjectId(answer.Packet.ReadBytes(12)));
                }
                catch (TimeoutException) { continue; }
            }
            
            var col = Data.Map.GetCollection();
            foreach (var map in col.FindAll())
            {
                if (maps.Contains(map.Id))
                    continue;

                var instance = Data.MapInstance.StartInstance(map);
                MapSubscriptions.AddSubscriptionList(map.Id.ToString());
                
                // TODO broadcast I am running this

                Console.WriteLine("Started instance of {0}", map.Id);
            }
        }
    }
}
