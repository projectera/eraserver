using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;

namespace ResourceService
{
    class Program
    {
        static ServiceClient _eras;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            _eras = ServiceClient.Connect("EraS");
        }
    }
}
