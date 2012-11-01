using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using EraS.Listeners;
using EraS.Topography;
using EraS.MessageHandlers;
using EraS.MessageHandlers.ErasComponents;
using EraS.Services;

namespace EraS
{
    class Program
    {
        public static ServiceListener Services { get; protected set; }
        public static ServerListener Servers { get; protected set; }
        public static Network Network { get; protected set; }
        public static Boolean IsRunning { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("EraS starting.");

            Router router = new Router();

            Console.ReadKey(true);
            HeartBeatService.Cleanup();
        }
    }
}
