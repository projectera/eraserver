using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Net;
using System.Threading;

namespace ServiceProtocol
{
    class ServiceClient
    {
        public const UInt16 ServicePort = 45246;
        public const byte Version = 0;

        public NetClient Client { get; protected set; }
        public String ServiceName { get; protected set; }
        public String Identifier { get; protected set; }
        public Boolean IsRunning { get; set; }

        public Action<Message> OnMessageArrived { get; set; }
        public Action OnConnectionClosed { get; set; }

        protected Thread Thread { get; set; }

        public ServiceClient(string serviceName)
        {
            ServiceName = serviceName;

            var c = new NetPeerConfiguration("EraService");
            Client = new NetClient(c);
            
            // Send version and service name
            var hail = Client.CreateMessage();
            hail.Write(Version);
            hail.Write(ServiceName);

            Client.Connect(new IPEndPoint(IPAddress.Loopback, ServicePort), hail);

            // Wait for greeting and read identifier
            Client.MessageReceivedEvent.WaitOne(2000);
            var greet = Client.ReadMessage();
            if (greet == null)
                return;
            Identifier = greet.ReadString();

            IsRunning = true;
            Thread = new Thread(() =>
            {
                while (IsRunning)
                {
                    Client.MessageReceivedEvent.WaitOne(100);
                    var m = Client.ReadMessage();
                    if (m == null)
                        continue;

                    switch (m.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine(m.ReadString());
                            break;
                        case NetIncomingMessageType.Data:
                            var msg = new Message(m);
                            OnMessageArrived(msg);
                            break;
                    }
                }
                // Notify closed
                OnConnectionClosed();
            });
            Thread.Start();
        }

        public Message CreateMessage(MessageType type, String destination)
        {
            if(!IsRunning)
                throw new ObjectDisposedException("ServiceClient");

            return new Message(Client.CreateMessage(), type, destination);
        }
    }
}
