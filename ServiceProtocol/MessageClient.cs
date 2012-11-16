using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Threading.Tasks;

namespace ERA.Protocols.ServiceProtocol
{
    public class MessageClient
    {
        public const byte Version = 0;

        /// <summary>
        /// The connection
        /// </summary>
        public NetConnection Connection { get; protected set; }

        /// <summary>
        /// The identifier of this client, address known by the server
        /// </summary>
        public String Identifier { get; protected set; }

        /// <summary>
        /// The identifier address of the server
        /// </summary>
        public String RemoteIdentifier { get; protected set; }

        /// <summary>
        /// The message handlers
        /// </summary>
        public Dictionary<MessageType, Action<Message>> MessageHandlers { get; protected set; }
        /// <summary>
        /// Gets called when the connection closes, should clean up stuff
        /// </summary>
        public event Action OnConnectionClosed = delegate { };

        /// <summary>
        /// The current question counter
        /// </summary>
        protected Int32 QuestionCounter { get; set; }
        /// <summary>
        /// The list of outstanding questions
        /// </summary>
        protected Dictionary<Int32, TaskCompletionSource<Message>> Questions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="Identifier"></param>
        public MessageClient(NetConnection connection, String identifier, String remoteIdentifier)
        {
            Identifier = identifier;
            RemoteIdentifier = remoteIdentifier;
            QuestionCounter = 1;
            Questions = new Dictionary<Int32, TaskCompletionSource<Message>>();
            MessageHandlers = new Dictionary<MessageType, Action<Message>>();
            Connection = connection;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void RaiseOnConnectionClosed()
        {
            OnConnectionClosed.Invoke();
        }

        /// <summary>
        /// Handles the processing of messages
        /// </summary>
        /// <param name="msg">The message to process</param>
        public void HandleMessage(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.Control:
                    // Control packet
                    ControlType t = (ControlType)msg.Packet.ReadByte();
                    switch (t)
                    {
                        case ControlType.Kill:
                            Connection.Disconnect("kill");
                            throw new ApplicationException("Killed by client");
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
            return new Message(CreateOutgoingMessage(32), type, Identifier, destination, thread);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public NetOutgoingMessage CreateOutgoingMessage(Int32 size = 32)
        {
            return Connection.Peer.CreateMessage(size);
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
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Message CloneMessage(Message message)
        {
            var result = new Message(Connection.Peer.CreateMessage(32), message.Type, message.Origin, message.Destination, message.Thread);
            var leftover = message.Packet.Data.Skip(result.Packet.LengthBytes);
            result.Packet.Write(leftover.ToArray()); 
            return result;
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
            Connection.SendMessage((NetOutgoingMessage)msg.Packet, NetDeliveryMethod.ReliableUnordered, 0);
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

        /// <summary>
        /// Tries to send the message for 100 seconds
        /// </summary>
        /// <param name="msg">The question to send</param>
        /// <returns>The answer message</returns>
        public Message AskReliableQuestion(Message msg)
        {
            for(int i = 0; i < 10; i++)
            {
                try
                {
                    return AskQuestion(msg);
                }
                catch (TimeoutException) { }

                // Can not send same message twice
                msg = CloneMessage(msg);
            }
            throw new TimeoutException("Server did not answer within 100 seconds, invalid assumption");
        }
    }
}
