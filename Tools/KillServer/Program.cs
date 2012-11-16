using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERA.Protocols.ServiceProtocol;

namespace ERA.Tools.KillServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = ServiceClient.Connect("KillServer");
            var msg = client.CreateMessage(MessageType.Control, "self");
            msg.Packet.Write((byte)ControlType.Kill);

            client.SendMessage(msg);
        }
    }
}
