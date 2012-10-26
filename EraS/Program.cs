using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;

namespace EraS
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            HeartBeatService.Defibrillate();

            

            System.Threading.Thread.Sleep(1000 * 65);
        }
    }
}
