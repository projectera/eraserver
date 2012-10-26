using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProtocol
{
    /// <summary>
    /// Handles the connection to the EraServer
    /// </summary>
    class ServiceClient
    {
        public const UInt16 ServicePort = 45246;
        public const byte Version = 0;

        /// <summary>
        /// The socket used to connect to the server
        /// </summary>
        public NetClient Client { get; protected set; }
        /// <summary>
        /// The name of this service
        /// </summary>
        public String ServiceName { get; protected set; }
        /// <summary>
        /// The identifier received from the server
        /// </summary>
        public String Identifier { get; protected set; }
        /// <summary>
        /// Whether the client is still connected
        /// </summary>
        public Boolean IsConnected { get; set; }

        /// <summary>
        /// Gets called when an internal (sent by this service) message arrives
        /// </summary>
        public Action<Message> OnInternalMessageArrived { get; set; }
        /// <summary>
        /// Gets called when an external (sent by another server) message arrives
        /// </summary>
        public Action<Message> OnServiceMessageArrived { get; set; }
        /// <summary>
        /// Gets called when the connection closes, should clean up stuff
        /// </summary>
        public event Action OnConnectionClosed;

        /// <summary>
        /// The thread the socket is running on
        /// </summary>
        protected Thread Thread { get; set; }

        /// <summary>
        /// The current question counter
        /// </summary>
        protected Int32 QuestionCounter { get; set; }
        /// <summary>
        /// The list of outstanding questions
        /// </summary>
        protected Dictionary<Int32, TaskCompletionSource<Message>> Questions { get; set; }

        /// <summary>
        /// Creates a new ServiceClient, connects automatically to the server
        /// </summary>
        /// <param name="serviceName"></param>
        public ServiceClient(string serviceName)
        {
            ServiceName = serviceName;

            QuestionCounter = 1;
            Questions = new Dictionary<Int32, TaskCompletionSource<Message>>();

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

            IsConnected = true;
            Thread = new Thread(() =>
            {
                while (IsConnected)
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
                            HandleMessage(new Message(m));
                            break;
                    }
                }
                Client.Disconnect("");
                // Notify closed
                if(OnConnectionClosed != null)
                    OnConnectionClosed();
            });
            Thread.Start();
        }

        /// <summary>
        /// Handles the processing of messages
        /// </summary>
        /// <param name="msg">The message to process</param>
        protected void HandleMessage(Message msg)
        {
            if (msg.Type == MessageType.Control)
            {
                // Control packet
                ControlType t = (ControlType)msg.Packet.ReadByte();
                switch (t)
                {
                    case ControlType.Kill:
                        IsConnected = false;
                        break;
                    case ControlType.IdentifierNotFound:
                        lock (Questions)
                        {
                            if (!Questions.ContainsKey(msg.Thread))
                                return;
                            Questions[msg.Thread].SetException(new KeyNotFoundException("Identifier not found while sending message"));
                        }
                        break;
                }
            }
            else
            {
                // Normal message
                if (msg.Thread != 0)
                {
                    lock (Questions)
                    {
                        if (!Questions.ContainsKey(msg.Thread))
                            return;
                        Questions[msg.Thread].SetResult(msg);
                        Questions.Remove(msg.Thread);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new raw message
        /// </summary>
        /// <param name="type">The message type</param>
        /// <param name="destination">The destination</param>
        /// <param name="thread">The question id</param>
        /// <returns>A new message</returns>
        public Message CreateMessage(MessageType type, String destination, int thread)
        {
            if(!IsConnected)
                throw new ObjectDisposedException("ServiceClient");

            return new Message(Client.CreateMessage(), type, Identifier, destination, thread);
        }

        /// <summary>
        /// Creates a new raw message
        /// </summary>
        /// <param name="type">The message type</param>
        /// <param name="destination">The destination</param>
        /// <returns>A new message</returns>
        public Message CreateMessage(MessageType type, String destination)
        {
            return CreateMessage(type, destination, 0);
        }

        /// <summary>
        /// Creates a new question message
        /// </summary>
        /// <param name="type">The message type</param>
        /// <param name="destination">The destination</param>
        /// <returns>A new message with the thread set to an unique number</returns>
        public Message CreateQuestion(MessageType type, String destination)
        {
            QuestionCounter++;
            if(QuestionCounter == Int32.MaxValue)
                QuestionCounter = 1;
            return CreateMessage(type, destination, QuestionCounter);
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="msg">The message to send</param>
        public void SendMessage(Message msg)
        {
            Client.SendMessage((NetOutgoingMessage)msg.Packet, NetDeliveryMethod.ReliableUnordered);
        }

        /// <summary>
        /// Sends a question
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <returns>The task with the anser</returns>
        public Task<Message> SendQuestion(Message msg)
        {
            var t = new TaskCompletionSource<Message>(msg.Thread);
            lock(Questions)
                Questions.Add(msg.Thread, t);
            SendMessage(msg);
            return t.Task;
        }

        /// <summary>
        /// Cancels an outstanding question
        /// </summary>
        /// <param name="t"></param>
        public void CancelQuestion(Task<Message> t)
        {
            Int32 questionid = (Int32)t.AsyncState;
            lock (Questions)
            {
                if (!Questions.ContainsKey(questionid))
                    return;

                Questions[questionid].SetCanceled();
                Questions.Remove(questionid);
            }
        }

        /// <summary>
        /// Sends a message and waits for the answer
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <returns>The answer message</returns>
        public Message AskQuestion(Message msg)
        {
            var t = SendQuestion(msg);
            t.Wait(10000);
            if(!t.IsCompleted)
            {
                CancelQuestion(t);
                throw new TimeoutException("The question was not answered within 10 seconds.");
            }

            return t.Result;
        }

        public Message AskReliableQuestion(Message msg)
        {
            Message res = null;
            while (res == null)
            {
                try
                {
                    res = AskQuestion(msg);
                }
                catch (TimeoutException) { }
            }
            return res;
        }
    }
}
