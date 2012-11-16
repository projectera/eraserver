using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Threading;
using ServiceProtocol;

namespace ERA.Protocols.SubscriptionProtocol
{
    public class Subscriptions
    {
        protected Dictionary<String, List<String>> _subscriptions;
        protected ServiceClient _client;
        protected ReaderWriterLockSlim _subscriptionsLock;

        /// <summary>
        /// Creates anew subscriptions object
        /// </summary>
        public Subscriptions(ServiceClient client)
        {
            _subscriptions = new Dictionary<String, List<String>>();
            _client = client;
            _subscriptionsLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Adds an available subscription list
        /// </summary>
        /// <param name="id"></param>
        public void AddSubscriptionList(String id)
        {
            _subscriptionsLock.EnterWriteLock();
            if (!_subscriptions.ContainsKey(id))
                _subscriptions.Add(id, new List<String>());
            _subscriptionsLock.ExitWriteLock();
        }

        /// <summary>
        /// Gets the available subscription lists
        /// </summary>
        /// <returns></returns>
        public List<String> GetSubscriptionLists()
        {
            _subscriptionsLock.EnterReadLock();
            var result = _subscriptions.Keys.ToList();
            _subscriptionsLock.ExitReadLock();
            return result;
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
            _subscriptionsLock.EnterWriteLock();
            if (!_subscriptions.ContainsKey(list))
            {
                _subscriptionsLock.ExitWriteLock();
                return false;
            }
            _subscriptions[list].Add(subscriber);
            _subscriptionsLock.ExitWriteLock();
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
            _subscriptionsLock.EnterWriteLock();
            if (!_subscriptions.ContainsKey(list))
            {
                _subscriptionsLock.ExitWriteLock();
                return;
            }
            _subscriptions[list].Remove(subscriber);
            _subscriptionsLock.ExitWriteLock();
        }

        /// <summary>
        /// Pushes a packet over an subscription list
        /// </summary>
        /// <param name="list">List to push over</param>
        /// <param name="packet">Packet to push</param>
        public void PushPacket(String list, NetBuffer packet)
        {
            String[] array = null;

            _subscriptionsLock.EnterReadLock();
            if (!_subscriptions.ContainsKey(list))
            {
                _subscriptionsLock.ExitReadLock();
                return;
            }

            array = new String[_subscriptions[list].Count];
            _subscriptions[list].CopyTo(array);
            _subscriptionsLock.ExitReadLock();

            foreach (var destination in array)
            {
                var message = _client.CreateMessage(MessageType.Public, destination);
                message.Packet.Write(packet.Data);
                _client.SendMessage(message);
            }
        }

        /// <summary>
        /// Removes a subscriptionlist
        /// </summary>
        /// <param name="p"></param>
        public void RemoveSubscriptionList(String id)
        {
            _subscriptionsLock.EnterWriteLock();
            _subscriptions.Remove(id);
            _subscriptionsLock.ExitWriteLock();
        }
    }
}
