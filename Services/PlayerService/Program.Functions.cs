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
