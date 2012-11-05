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

            StartRunningMaps();

            // Messages and start running
            _erasClient.MessageHandlers.Add(MessageType.Service, HandleMessages);
            IsRunning = true;

            // Save the network info
            NetworkInfo = new NetworkInfo(_erasClient);

            while (_erasClient.IsConnected && IsRunning)
                System.Threading.Thread.Sleep(1000);

            Console.WriteLine("Service terminated.");
        
        }

        /// <summary>
        /// 
        /// </summary>
        private static void StartRunningMaps()
        {
            // TODO get all the already running instances
            // compare with all the database instances
            // start running all that is not yet running
            // request some maps from other services

            var col = Data.Map.GetCollection();
            foreach (var map in col.FindAll())
            {
                Data.MapInstance.StartInstance(map);
                Console.WriteLine("Started instance of {0}", map.Id);
            }
        }
    }
}
