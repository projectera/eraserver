using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace ServiceProtocol
{
    /// <summary>
    /// Represents a message packet
    /// </summary>
    class Message
    {
        /// <summary>
        /// The network package
        /// </summary>
        public NetBuffer Packet { get; protected set; }
        /// <summary>
        /// The type of message
        /// </summary>
        public MessageType Type { get; protected set; }
        /// <summary>
        /// The Origin of the message
        /// </summary>
        public String Origin { get; protected set; }
        /// <summary>
        /// The destination of the message
        /// </summary>
        public String Destination { get; protected set; }
        /// <summary>
        /// The message thread this message belongs to
        /// </summary>
        public Int32 Thread { get; protected set; }

        /// <summary>
        /// Creates a new outgoing message
        /// </summary>
        /// <param name="message">The network packet</param>
        /// <param name="type">The message type</param>
        /// <param name="destination">The destination</param>
        public Message(NetOutgoingMessage message, MessageType type, String origin, String destination, int thread)
        {
            Packet = message;
            Type = type;
            Origin = origin;
            Destination = destination;
            Thread = thread;

            Packet.Write((Byte)Type);
            Packet.Write(Origin);
            Packet.Write(Destination);
            Packet.Write(thread);
        }

        /// <summary>
        /// Creates a new incoming message
        /// </summary>
        /// <param name="message">The incoming packet</param>
        public Message(NetIncomingMessage message)
        {
            Packet = message;
            Type = (MessageType)Packet.ReadByte();
            Origin = Packet.ReadString();
            Destination = Packet.ReadString();
            Thread = Packet.ReadInt32();
        }

        /// <summary>
        /// Creates an answer message
        /// </summary>
        /// <param name="client">The client to send it with</param>
        /// <returns>An answer message</returns>
        public Message Answer(ServiceClient client)
        {
            return client.CreateMessage(Type, Origin, Thread);
        }
    }
}
