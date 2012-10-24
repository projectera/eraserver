using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EraS.Topography
{
    class Service
    {
        /// <summary>
        /// The server the program is connected to (EraS instance)
        /// </summary>
        public Server Server { get; protected set; }
        /// <summary>
        /// The service name
        /// </summary>
        public String Name { get; protected set; }
        /// <summary>
        /// The unique identifier for the service
        /// </summary>
        public String Identifier { get; protected set; }
    }
}
