using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Topography;

namespace EraS.Connections
{
    class ServiceConnection
    {
        public Service Service { get; protected set; }

        public ServiceConnection(Service service)
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
