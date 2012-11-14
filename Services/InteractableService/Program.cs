using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using System.Threading;
using SubscriptionProtocol;
using MongoDB.Bson;

namespace InteractableService
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
            NetworkInfo = new ServiceProtocol.NetworkInfo(EraSClient);

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

            // TODO make a broadcast type
            foreach (var iservice in interactableservices)
            {
                if (iservice == NetworkInfo.Client.Identifier)
                    continue;

                try
                {
                    var question = EraSClient.CreateQuestion(MessageType.Internal, iservice);
                    question.Packet.Write("GetRunning");
                    var answer = EraSClient.AskQuestion(question);
                    var count = answer.Packet.ReadInt32();
                    for (Int32 i = 0; i < count; i++)
                        interactables.Add(new ObjectId(answer.Packet.ReadBytes(12)));
                }
                catch (TimeoutException) { continue; }
            }
        }
    }
}
