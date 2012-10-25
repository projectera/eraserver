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
        /// The destination of the message
        /// </summary>
        public String Destination { get; protected set; }

        /// <summary>
        /// Creates a new outgoing message
        /// </summary>
        /// <param name="message">The network packet</param>
        /// <param name="type">The message type</param>
        /// <param name="destination">The destination</param>
        public Message(NetOutgoingMessage message, MessageType type, String destination)
        {
            Packet = message;
            Type = type;
            Packet.Write((Byte)Type);
            Destination = destination;
            Packet.Write(Destination);
        }

        /// <summary>
        /// Creates a new incoming message
        /// </summary>
        /// <param name="message">The incoming packet</param>
        public Message(NetIncomingMessage message)
        {
            Packet = message;
            Type = (MessageType)Packet.ReadByte();
            Destination = Packet.ReadString();
        }
    }
}
