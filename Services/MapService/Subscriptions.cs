using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using Lidgren.Network;

namespace ServiceProtocol
{
    // TODO REFACTOR
    public class Subscriptions
    {
        protected Dictionary<String, List<String>> _subscriptions;
        protected ServiceClient _client;

        /// <summary>
        /// Creates anew subscriptions object
        /// </summary>
        public Subscriptions(ServiceClient client)
        {
            _subscriptions = new Dictionary<String, List<String>>();
            _client = client;
        }

        /// <summary>
        /// Adds an available subscription list
        /// </summary>
        /// <param name="id"></param>
        public void AddSubscriptionList(String id)
        {
            lock (_subscriptions)
            {
                if (!_subscriptions.ContainsKey(id))
                    _subscriptions.Add(id, new List<String>());
            }
        }

        /// <summary>
        /// Gets the available subscription lists
        /// </summary>
        /// <returns></returns>
        public List<String> GetSubscriptionLists()
        {
            lock (_subscriptions)
            {
                return _subscriptions.Keys.ToList();
            }
        }

        /// <summary>
        /// Adds a new subscriber
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Boolean AddSubscriber(Message message)
        {
            String subscriber = message.Origin;
            String list = message.Packet.ReadString();

            return AddSubscriber(list, subscriber);
        }

        /// <summary>
        /// Adds a new subscriber
        /// </summary>
        /// <param name="list"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public Boolean AddSubscriber(String list, String subscriber)
        {
            lock (_subscriptions)
            {
                if (!_subscriptions.ContainsKey(list))
                    return false;
                _subscriptions[list].Add(subscriber);
            }
            return true;
        }

        /// <summary>
        /// Removes a subscriber
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public void RemoveSubscriber(Message message)
        {
            String subscriber = message.Origin;
            String list = message.Packet.ReadString();

            RemoveSubscriber(list, subscriber);
        }

        /// <summary>
        /// Removes a subscriber
        /// </summary>
        /// <param name="list"></param>
        /// <param name="subscriber"></param>
        public void RemoveSubscriber(String list, String subscriber)
        {
            lock (_subscriptions)
            {
                if (!_subscriptions.ContainsKey(list))
                    return;
                _subscriptions[list].Remove(subscriber);
            }
        }

        /// <summary>
        /// Pushes a packet over an subscription list
        /// </summary>
        /// <param name="list">List to push over</param>
        /// <param name="packet">Packet to push</param>
        public void PushPacket(String list, NetBuffer packet)
        {
            String[] array = null;

            lock (_subscriptions)
            {
                array = new String[_subscriptions[list].Count];
                _subscriptions[list].CopyTo(array);
            }

            foreach (var destination in array)
            {
                var message = _client.CreateMessage(MessageType.Public, destination);
                message.Packet.Write(packet.Data);
                _client.SendMessage(message);
            }
        }
    }
}
