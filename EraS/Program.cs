using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using EraS.Listeners;

namespace EraS
{
    class Program
    {
        public static ServiceListener Services { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            HeartBeatService.Defibrillate();

            Services = new ServiceListener(HeartBeatService.Identifier.ToString());

            while(Services.IsConnected)
                System.Threading.Thread.Sleep(1000);
        }
    }
}
