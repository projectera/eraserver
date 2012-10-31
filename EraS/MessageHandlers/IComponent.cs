using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Connections;
using ServiceProtocol;

namespace EraS.MessageHandlers
{
    public interface IComponent
    {
        String Name { get; }
        void HandleMessage(MessageClient con, Message msg);
    }
}
