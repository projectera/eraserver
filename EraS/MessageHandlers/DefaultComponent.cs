using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Connections;
using ServiceProtocol;

namespace EraS.MessageHandlers
{
    class DefaultComponent : IComponent
    {
        /// <summary>
        /// The name of this component
        /// </summary>
        public String Name { get; protected set; }
        /// <summary>
        /// The callable functions in the component
        /// </summary>
        public Dictionary<String, Action<MessageClient, Message>> Functions { get; protected set; }

        /// <summary>
        /// Creates a new component
        /// </summary>
        /// <param name="name">The name of the component</param>
        public DefaultComponent(String name)
        {
            Name = name;
            Functions = new Dictionary<string, Action<MessageClient, Message>>();
        }

        /// <summary>
        /// Passes a message to the right function
        /// </summary>
        /// <param name="con">The connection this message came from</param>
        /// <param name="msg">The message</param>
        public void HandleMessage(MessageClient con, Message msg)
        {
            var function = msg.Packet.ReadString();
            lock (Functions)
            {
                if (!Functions.ContainsKey(function))
                    return;
                Functions[function](con, msg);
            }
        }
    }
}
