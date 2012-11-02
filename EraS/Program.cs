using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EraS
{
    class Program
    {
        public static Router Router { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("EraS starting.");

            Router = new Router();
            Console.WriteLine("Testing");

            while (true)
                Thread.Sleep(1000);
        }
    }
}
