﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERA.Protocols.ServiceProtocol;
using System.Threading;
using MongoDB.Bson;

namespace ERA.Services.InteractableService
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
            Functions.Add("GetRunningMapInstances", GetRunningMapInstances);

            // Public Response
            Functions.Add("Subscribe", Subscribe);
            Functions.Add("Unsubscribe", Unsubscribe);

            Functions.Add("Get", Get);
           

            #endregion
            FunctionsLock.ExitWriteLock();
        }

        /// <summary>
        /// Gets the protocol version for this service
        /// </summary>
        /// <param name="msg"></param>
        public static void GetVersion(Message msg)
        {
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
            answer.Packet.Write((Int32)0);
            var interactables = InteractableSubscriptions.GetSubscriptionLists();
            answer.Packet.Write(interactables.Count);
            foreach (var interactable in interactables)
                answer.Packet.Write(new ObjectId(interactable).ToByteArray());

            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public static void GetRunningMapInstances(Message msg)
        {
            var answer = msg.Answer(EraSClient);
            answer.Packet.Write((Int32)0);
            var maps = MapInteractablesInstances.GetKeys();
            answer.Packet.Write(maps.Count);
            foreach (var map in maps)
            {
                answer.Packet.Write(map.ToByteArray());
                var instances = MapInteractablesInstances.GetKeysOf(map);
                answer.Packet.Write(instances.Count);
                foreach (var instance in instances)
                    answer.Packet.Write(instance.ToByteArray());
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
            answer.Packet.Write(InteractableSubscriptions.AddSubscriber(msg));
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Unsubscribes an origin from a list
        /// </summary>
        /// <param name="msg"></param>
        public static void Unsubscribe(Message msg)
        {
            InteractableSubscriptions.RemoveSubscriber(msg);
        }

        /// <summary>
        /// Gets map data
        /// </summary>
        /// <param name="msg"></param>
        public static void Get(Message msg)
        {
            var iId = new ObjectId(msg.Packet.ReadBytes(12));
            var answer = msg.Answer(EraSClient);

            Data.Interactable.Get(iId).ContinueWith((t) =>
            {
                if (t.IsCompleted)
                {
                    t.Result.Pack(answer.Packet);
                    EraSClient.SendMessage(answer);
                }
            });
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
