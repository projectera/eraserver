using System;
using System.Collections.Generic;
using System.Threading;
using ERA.Protocols.ServiceProtocol;
using ERA.Protocols.SubscriptionProtocol;
using MongoDB.Bson;

namespace ERA.Services.MapService
{
    partial class Program
    {
        /// <summary>
        /// Reference to the service client
        /// </summary>
        internal static ServiceClient EraSClient;

        /// <summary>
        /// Network Information
        /// </summary>
        public static NetworkInfo NetworkInfo { get; protected set; }

        /// <summary>
        /// Service is Running
        /// </summary>
        public static Boolean IsRunning
        {
            set
            {
                if (value) StopRunningSemaphore.Reset();
                else StopRunningSemaphore.Set();
            }
        }

        /// <summary>
        /// Signals the thread if it can stop running
        /// </summary>
        private static ManualResetEvent StopRunningSemaphore { get; set; }

        /// <summary>
        /// MapId { InstanceId => Instance}
        /// </summary>
        public static ThreadsafeDictOfDict<ObjectId, ObjectId, Data.MapInstance> MapInstances { get; protected set; }
        
        /// <summary>
        /// Subscriptions
        /// </summary>
        public static Subscriptions MapSubscriptions { get; protected set; }

    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Service starting.");

            // Functions
            RegisterFunctions();

            // Connect to the cloud
            EraSClient = ServiceClient.Connect("Map", true);
            StopRunningSemaphore = new ManualResetEvent(true);

            // Save the network info
            MapSubscriptions = new Subscriptions(EraSClient);
            NetworkInfo = new NetworkInfo(EraSClient);
            MapInstances = new ThreadsafeDictOfDict<ObjectId, ObjectId, Data.MapInstance>();

            // Start this
            StartRunningMaps();

            // Start handling messages
            EraSClient.MessageHandlers.Add(MessageType.Service, HandleMessages);
            Console.WriteLine("Connected with Id: {0}", EraSClient.ServiceName);
            IsRunning = true;

            StopRunningSemaphore.WaitOne();
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
                    var question = EraSClient.CreateQuestion(MessageType.Internal, mapservice);
                    question.Packet.Write("GetRunning");
                    var answer = EraSClient.AskQuestion(question);
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
                
                // TODO broadcast I am running this

                Console.WriteLine("Started instance of {0}", map.Id);
            }
        }
    }
}
