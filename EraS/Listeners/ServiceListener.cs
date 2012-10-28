using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using Lidgren.Network;
using System.Threading.Tasks;
using EraS.Connections;

namespace EraS.Listeners
{
    class ServiceListener
    {
        public NetServer Server { get; protected set; }
        public Boolean IsConnected { get; set; }
        public Task LastTask { get; protected set; }
        public String Identifier { get; protected set; }
        protected Int32 _serviceCounter { get; set; }
        public Dictionary<String, ServiceConnection> Connections { get; protected set; }

        public event Action OnShutdown;

        public ServiceListener(String identifier)
        {
            Identifier = identifier;
            Connections = new Dictionary<string, ServiceConnection>();

            var conf = new NetPeerConfiguration("EraService")
            {
                Port = ServiceClient.ServicePort,
            };
            conf.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            Server = new NetServer(conf);
            Server.Start();

            IsConnected = true;

            LastTask = Task.Factory.StartNew(ReadMessages, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Tries to read a message from the internal client and adds itself to the task again
        /// </summary>
        protected void ReadMessages()
        {
            if (!IsConnected)
            {
                if (OnShutdown != null)
                    OnShutdown();
                Server.Shutdown("");
                return;
            }

            LastTask = LastTask.
                ContinueWith((_) => { Server.MessageReceivedEvent.WaitOne(100); }).
                ContinueWith((_) => ReadMessages());

            var m = Server.ReadMessage();
            if (m == null)
                return;

            switch (m.MessageType)
            {
                case NetIncomingMessageType.ConnectionApproval:
                    byte clientversion = m.ReadByte();
                    if (clientversion > MessageClient.Version)
                        m.SenderConnection.Deny("Max. version: " + MessageClient.Version.ToString());

                    string servicename = m.ReadString();
                    var outmsg = Server.CreateMessage(32);

                    string remoteid = Identifier + "-" + (_serviceCounter++).ToString();
                    outmsg.Write(remoteid);
                    var handler = new ServiceConnection(m.SenderConnection, Identifier, remoteid)
                    {
                        
                    };
                    m.SenderConnection.Tag = handler;
                    Connections.Add(remoteid, handler);
                    m.SenderConnection.Approve(outmsg);

                    Console.WriteLine("hello " + servicename);
                    break;
                case NetIncomingMessageType.Data:
                    ((ServiceConnection)m.SenderConnection.Tag).HandleData(m);
                    break;
            }
        }
    }
}
