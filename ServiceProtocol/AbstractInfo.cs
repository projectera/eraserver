using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace ServiceProtocol
{
    public abstract class AbstractInfo
    {
         /// <summary>
        /// The socket used to connect to the server
        /// </summary>
        public MessageClient Client { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public String ComponentName { get; protected set; }

        /// <summary>
        /// Creates a new StatisticsInfo object
        /// </summary>
        /// <param name="client">The client to use</param>
        public AbstractInfo(String component, MessageClient client)
        {
            Client = client;
            ComponentName = component;
        }

        /// <summary>
        /// Creates a question
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public Message CreateQuestion(String function)
        {
            var m = Client.CreateQuestion(MessageType.EraS, "Self");
            m.Packet.Write(ComponentName);
            m.Packet.Write(function);
            return m;
        }

        /// <summary>
        /// Gets a string
        /// </summary>
        /// <param name="function">Function to ask</param>
        /// <returns></returns>
        protected String GetString(String function)
        {
            var res = Client.AskQuestion(CreateQuestion(function));
            return res.Packet.ReadString();
        }

        /// <summary>
        /// Gets a string
        /// </summary>
        /// <param name="function">Function to ask</param>
        /// <param name="argument">Argument</param>
        /// <returns></returns>
        protected String GetString(String function, String argument)
        {
            var m = CreateQuestion(function);
            m.Packet.Write(argument);
            var res = Client.AskQuestion(m);
            return res.Packet.ReadString();
        }

        /// <summary>
        /// Gets a list of strings
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        protected List<String> GetStringList(String function)
        {
            var res = Client.AskQuestion(CreateQuestion(function));

            Int32 count = res.Packet.ReadInt32();
            var l = new List<String>(count);
            for (int i = 0; i < count; i++)
                l.Add(res.Packet.ReadString());

            return l;
        }

        /// <summary>
        /// Gets a list of strings
        /// </summary>
        /// <param name="function"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        protected List<String> GetStringList(String function, String argument)
        {
            var m = CreateQuestion(function);
            m.Packet.Write(argument);
            var res = Client.AskQuestion(m);

            Int32 count = res.Packet.ReadInt32();
            var l = new List<String>(count);
            for (int i = 0; i < count; i++)
                l.Add(res.Packet.ReadString());

            return l;
        }

    }
}
