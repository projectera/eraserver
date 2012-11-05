using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;

namespace KillServer
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
