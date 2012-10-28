using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Threading.Tasks;

namespace ServiceProtocol
{
    public class MessageClient
    {
        public const byte Version = 0;

        /// <summary>
        /// The socket used to connect to the server
        /// </summary>
        public NetPeer Peer { get; protected set; }
        /// <summary>
        /// The connection
        /// </summary>
        public NetConnection Connection { get; protected set; }

        /// <summary>
        /// Whether the client is still connected
        /// </summary>
        public Boolean IsConnected { get; set; }
        /// <summary>
        /// The identifier received from the server
        /// </summary>
        public String Identifier { get; protected set; }
        /// <summary>
        /// The message handlers
        /// </summary>
        public Dictionary<MessageType, Action<Message>> MessageHandlers { get; protected set; }
        /// <summary>
        /// Gets called when the connection closes, should clean up stuff
        /// </summary>
        public event Action OnConnectionClosed;

        /// <summary>
        /// The last queued task
        /// </summary>
        protected Task LastTask { get; set; }
        /// <summary>
        /// The current question counter
        /// </summary>
        protected Int32 QuestionCounter { get; set; }
        /// <summary>
        /// The list of outstanding questions
        /// </summary>
        protected Dictionary<Int32, TaskCompletionSource<Message>> Questions { get; set; }

        public MessageClient(NetPeer peer, NetConnection connection, String Identifier)
        {
            QuestionCounter = 1;
            Questions = new Dictionary<Int32, TaskCompletionSource<Message>>();
            MessageHandlers = new Dictionary<MessageType, Action<Message>>();
            Peer = peer;
            Connection = connection;

            IsConnected = true;
            LastTask = Task.Factory.StartNew(ReadMessages, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Tries to read a message from the internal client and adds itself to the task again
        /// </summary>
        protected virtual void ReadMessages()
        {
            if (!IsConnected)
            {
                if (OnConnectionClosed != null)
                    OnConnectionClosed();
                Peer.Shutdown("");
                return;
            }

            var m = Peer.ReadMessage();
            if (m != null && m.MessageType == NetIncomingMessageType.Data)
                HandleMessage(new Message(m));

            LastTask = LastTask.
                ContinueWith((_) => { Peer.MessageReceivedEvent.WaitOne(100); }).
                ContinueWith((_) => ReadMessages());
        }

        /// <summary>
        /// Handles the processing of messages
        /// </summary>
        /// <param name="msg">The message to process</param>
        protected void HandleMessage(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.Control:
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
                    break;
                case MessageType.Answer:
                    lock (Questions)
                    {
                        if (!Questions.ContainsKey(msg.Thread))
                            return;
                        Questions[msg.Thread].SetResult(msg);
                        Questions.Remove(msg.Thread);
                    }
                    break;
                default:
                    lock (MessageHandlers)
                    {
                        if(MessageHandlers.ContainsKey(msg.Type))
                            MessageHandlers[msg.Type](msg);
                    }
                    break;
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
            if (!IsConnected)
                throw new ObjectDisposedException("ServiceClient");

            return new Message(Peer.CreateMessage(32), type, Identifier, destination, thread);
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
            if (QuestionCounter == Int32.MaxValue)
                QuestionCounter = 1;
            return CreateMessage(type, destination, QuestionCounter);
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="msg">The message to send</param>
        public void SendMessage(Message msg)
        {
            Peer.SendMessage((NetOutgoingMessage)msg.Packet, Connection, NetDeliveryMethod.ReliableUnordered);
        }

        /// <summary>
        /// Sends a question
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <returns>The task with the anser</returns>
        public Task<Message> SendQuestion(Message msg)
        {
            var t = new TaskCompletionSource<Message>(msg.Thread);
            lock (Questions)
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
            if (!t.IsCompleted)
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
