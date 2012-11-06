using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using MongoDB.Bson;
using System.Threading;

namespace PlayerService
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

            // Functions needed:
            // Do we need a lobby for connects and picking an interactable and getting
            // the friend list and private messages list?

            // Subscribe << subscribes on the updates (online/offline status) of this person
            // JoinGame << sets the interactable, subscribes on a map, subscribes on an interactable
            // ^^ << sends the map resources, sends the interactable resources
            // LeaveGame << unsets the interactable, unsubscribes from a map, unsubscribes from an interactable

            // Private Message << receives (type == service/internal/public) or sends (type == cient) a message
            // FriendRequest << receives (type == service/internal/public) or sends (type == client) a friend request
            //  
            // Befriend << accepts the friend request
            // Unfriend << unfriends a friend
            // Block << blocks all messages from this person

            // THESE SHOULD BE IN INTERACTABLE
            /// -- Shout << tells the interactale to shout << not an actual function >
            /// -- MoveTo << tells the interactable to tell the map to try to move to xxx << Not an actual function, >
            /// -- Equip
            /// -- Trade
            /// -- Attack
            /// -- Action
            /// -- Guild stuff
            /// -- Team stuff
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
