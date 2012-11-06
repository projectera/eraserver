using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using MongoDB.Driver;
using System.Threading;

namespace ResourceService
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
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Functions
            RegisterFunctions();

            // Connect to the cloud
            EraSClient = ServiceClient.Connect("Resource", true);
            StopRunningSemaphore = new ManualResetEvent(true);

            EraSClient.MessageHandlers.Add(MessageType.Service, HandleMessages);
            Console.WriteLine("Connected with Id: {0}", EraSClient.ServiceName);
            IsRunning = true;

            // Save the network info
            NetworkInfo = new ServiceProtocol.NetworkInfo(EraSClient);

            StopRunningSemaphore.WaitOne();
            Console.WriteLine("Service terminated.");
        }
    }
}
