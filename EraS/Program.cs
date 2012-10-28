using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using EraS.Listeners;
using EraS.Topography;
using EraS.MessageHandlers;

namespace EraS
{
    class Program
    {
        public static ServiceListener Services { get; protected set; }
        public static Network Network { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            HeartBeatService.Defibrillate();
            Network = new Network(HeartBeatService.Identifier.ToString());

            Services = new ServiceListener(HeartBeatService.Identifier.ToString());
            ErasHandler h = new ErasHandler(Network);
            Services.MessageHandlers.Add(MessageType.EraS, h.HandleMessage);

            while(Services.IsConnected)
                System.Threading.Thread.Sleep(1000);
        }
    }
}
