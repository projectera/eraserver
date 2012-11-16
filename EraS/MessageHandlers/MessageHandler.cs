using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERA.Protocols.ServiceProtocol;

namespace EraS.MessageHandlers
{
    class MessageHandler
    {
        /// <summary>
        /// The list of components
        /// </summary>
        Dictionary<String, IComponent> Components { get; set; }

        /// <summary>
        /// Creates a new MessageHandler
        /// </summary>
        public MessageHandler()
        {
            Components = new Dictionary<String, IComponent>();
        }

        public void Add(IComponent component)
        {
            if(Components.ContainsKey(component.Name))
                return;

            Components.Add(component.Name, component);
        }

        public void Remove(IComponent component)
        {
            if (!Components.ContainsKey(component.Name))
                return;

            Components.Remove(component.Name);
        }

        /// <summary>
        /// Passes a message to the right component
        /// </summary>
        /// <param name="con">The connection the message came from</param>
        /// <param name="msg">The message</param>
        public void HandleMessage(MessageClient con, Message msg)
        {
            String component = msg.Packet.ReadString();
            if (!Components.ContainsKey(component))
                return;

            Components[component].HandleMessage(con, msg);
        }
    }
}
