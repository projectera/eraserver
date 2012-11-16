using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ERA.Protocols.ServiceProtocol;
using ERA.Protocols.SubscriptionProtocol;
using ERA.Services.InteractableService.Data;
using ERA.Utils;
using MongoDB.Bson;

namespace ERA.Services.InteractableService
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
        /// Subscriptions
        /// </summary>
        public static Subscriptions InteractableSubscriptions { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public static ThreadsafeDictOfDict<ObjectId, ObjectId, MapInteractablesInstance> MapInteractablesInstances;

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<ObjectId, String> InteractableMapper { get; protected set; }


    
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
            EraSClient = ServiceClient.Connect("Interactable", true);
            StopRunningSemaphore = new ManualResetEvent(true);

            // Save the network info
            InteractableSubscriptions = new Subscriptions(EraSClient);
            InteractableMapper = new Dictionary<ObjectId, string>();
            MapInteractablesInstances = new ThreadsafeDictOfDict<ObjectId, ObjectId, MapInteractablesInstance>();
            NetworkInfo = new NetworkInfo(EraSClient);

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
            var interactableservices = NetworkInfo.GetServiceInstances("Interactable");
            var interactables = new List<ObjectId>();
            var runningmapinstances = new Dictionary<Object, List<ObjectId>>();

            // TODO make a broadcast type
            foreach (var iservice in interactableservices)
            {
                if (iservice == NetworkInfo.Client.Identifier)
                    continue;

                // Gets all the running interactables
                try
                {
                    var question = EraSClient.CreateQuestion(MessageType.Internal, iservice);
                    question.Packet.Write("GetRunning");
                    var answer = EraSClient.AskQuestion(question);
                    var count = answer.Packet.ReadInt32();
                    for (Int32 i = 0; i < count; i++) {
                        var id = new ObjectId(answer.Packet.ReadBytes(12));
                        InteractableMapper.Add(id, iservice);
                        interactables.Add(id);
                    }
                }
                catch (TimeoutException) { continue; }

                // Gets all the running map instances
                try
                {
                    var question = EraSClient.CreateQuestion(MessageType.Internal, iservice);
                    question.Packet.Write("GetRunningMapInstances");
                    var answer = EraSClient.AskQuestion(question);
                    var count = answer.Packet.ReadInt32();
                    for (Int32 i = 0; i < count; i++)
                    {
                        var id = new ObjectId(answer.Packet.ReadBytes(12));
                        runningmapinstances.Add(id, new List<ObjectId>());
                        var mcount = answer.Packet.ReadInt32();
                        for (; mcount > 0; mcount--)
                            runningmapinstances[id].Add(new ObjectId(answer.Packet.ReadBytes(12)));
                    }
                }
                catch (TimeoutException) { continue; }
            }

            // Gets all the running map instances
            var mapservices = NetworkInfo.GetServiceInstances("Map");
            var maps = new Dictionary<ObjectId, List<ObjectId>>();

            foreach (var mservice in mapservices)
            {
                try
                {
                    var question = EraSClient.CreateQuestion(MessageType.Internal, mservice);
                    question.Packet.Write("GetRunning");
                    var answer = EraSClient.AskQuestion(question);
                    var count = answer.Packet.ReadInt32();
                    for (Int32 i = 0; i < count; i++)
                    {
                        var id = new ObjectId(answer.Packet.ReadBytes(12));

                        try
                        {
                            var innerquestion = EraSClient.CreateQuestion(MessageType.Internal, mservice);
                            innerquestion.Packet.Write("GetRunningInstances");
                            var inneranswer = EraSClient.AskQuestion(question);
                            var innercount = answer.Packet.ReadInt32();

                            maps.Add(id, new List<ObjectId>());
                            for (; innercount > 0; innercount--)
                            {
                                maps[id].Add(new ObjectId(inneranswer.Packet.ReadBytes(12)));
                            }
                        }
                        catch (TimeoutException) { continue; }
                    }
                }
                catch (TimeoutException) { continue; }
            }

            // For each mapinstance start a mapinteractablesinstance. 
            // But don't start the instances already existant
            foreach (var map in maps)
            {
                var mapid = map.Key;
                foreach (var instance in map.Value)
                {
                    if (runningmapinstances.ContainsKey(mapid) && runningmapinstances[mapid].Contains(instance))
                        continue;

                    var question = EraSClient.CreateQuestion(MessageType.Service, "Map");
                    question.Packet.Write("Get");
                    question.Packet.Write(mapid.ToByteArray());
                    var answer = EraSClient.AskQuestion(question);
                    var mapdata = Protocols.MapProtocol.Map.Read(answer.Packet);
                    Protocols.MapProtocol.MapInstance mapinstancedata = Protocols.MapProtocol.MapInstance.Generate(mapid, instance);

                    Data.MapInteractablesInstance.StartInstance(mapdata, mapinstancedata);
                }
            }
        }
    }
}
