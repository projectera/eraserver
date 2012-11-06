using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using MongoDB.Bson;
using MapProtocol;
using System.Threading;

namespace MapService
{
    partial class Program
    {
        public const String SERVICE_VERSION = "1.0.0";
        public static Dictionary<String, Action<Message>> Functions { get; protected set; }
        public static ReaderWriterLockSlim FunctionsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// 
        /// </summary>
        public static void RegisterFunctions()
        {
            FunctionsLock.EnterWriteLock();
            Functions = new Dictionary<String, Action<Message>>();

            #region Register Functions
            Functions.Add("GetVersion", GetVersion);
            Functions.Add("GetFunctions", GetFunctions);

            // Broadcast Response
            Functions.Add("GetRunning", GetRunning);
            Functions.Add("GetRunningInstances", GetRunningInstances);
            
            // Public Response
            Functions.Add("Subscribe", Subscribe);
            Functions.Add("Unsubscribe", Unsubscribe);
            #endregion
            FunctionsLock.ExitWriteLock();
        }

        /// <summary>
        /// Gets the protocol version for this service
        /// </summary>
        /// <param name="msg"></param>
        public static void GetVersion(Message msg) {
            var answer = msg.Answer(EraSClient);
            answer.Packet.Write(SERVICE_VERSION);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets a list of function names for this service
        /// </summary>
        /// <param name="msg"></param>
        public static void GetFunctions(Message msg)
        {
            var answer = msg.Answer(EraSClient);
            answer.Packet.Write(Functions.Keys.Count);
            foreach (var key in Functions.Keys)
                answer.Packet.Write(key);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets a list of keys of running map instances
        /// </summary>
        /// <param name="msg"></param>
        public static void GetRunning(Message msg)
        {
            var answer = msg.Answer(EraSClient);
            lock (MapInstances)
            {
                answer.Packet.Write(MapInstances.Keys.Count);
                foreach (var instance in MapInstances)
                    answer.Packet.Write(instance.Key.ToByteArray());
            }
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets the instance keys for the running instances
        /// </summary>
        /// <param name="msg"></param>
        public static void GetRunningInstances(Message msg)
        {
            var key = new ObjectId(msg.Packet.ReadBytes(12));
            var answer = msg.Answer(EraSClient);
            lock (MapInstances)
            {
                Dictionary<ObjectId, Data.MapInstance> instances = null;
                if (!MapInstances.TryGetValue(key, out instances)) {
                    answer.Packet.Write((Int32)0);
                } else {
                    answer.Packet.Write(instances.Keys.Count);
                    foreach (var instance in instances)
                        answer.Packet.Write(instance.Key.ToByteArray());
                }
            }
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Subscribers the origin to a list
        /// </summary>
        /// <param name="msg"></param>
        public static void Subscribe(Message msg)
        {
            var answer = msg.Answer(EraSClient);
            answer.Packet.Write(MapSubscriptions.AddSubscriber(msg));
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Unsubscribes an origin from a list
        /// </summary>
        /// <param name="msg"></param>
        public static void Unsubscribe(Message msg)
        {
            MapSubscriptions.RemoveSubscriber(msg);
        }

        /// <summary>
        /// Handles messages
        /// </summary>
        /// <param name="m"></param>
        public static void HandleMessages(Message m)
        {
            var function = m.Packet.ReadString();

            FunctionsLock.EnterReadLock();
            if (!Functions.ContainsKey(function))
                return;
            Functions[function](m);
            FunctionsLock.ExitReadLock();
        }
    }
}
