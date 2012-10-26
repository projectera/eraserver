using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;
using ServiceProtocol;

namespace EraS.Connections
{
    class ServiceConnection : ServiceClient
    {
        public Service Service { get; protected set; }

        public ServiceConnection(Service service) : base("")
        {
            Service = service;
        }

        public void SendMessage()
        {
            throw new NotImplementedException();
        }

        public void Kill()
        {
            throw new NotImplementedException();
        }
    }
}
