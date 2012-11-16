using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERA.Protocols.ServiceProtocol;
using MongoDB.Bson;
using System.Threading;

namespace ERA.Services.ResourceService
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
            Functions.Add("GetAsset", GetAsset);
            Functions.Add("GetAssetChunks", GetAssetChunks);
            Functions.Add("GetAssetChunk", GetAssetChunk);
            Functions.Add("Generate", Generate);
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
            foreach(var key in Functions.Keys)
                answer.Packet.Write(key);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets asset information
        /// </summary>
        /// <param name="msg"></param>
        public static void GetAsset(Message msg)
        {
            var filename = msg.Packet.ReadString();
            var answer = msg.Answer(EraSClient);
            var file = Asset.GetFile(filename);
            if (file == null)
                return;
            file.Pack(answer.Packet);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets asset chunks id's
        /// </summary>
        /// <param name="msg"></param>
        public static void GetAssetChunks(Message msg)
        {
            var fileid = new ObjectId(msg.Packet.ReadBytes(12));
            var answer = msg.Answer(EraSClient);
            Int32 chunkSize, length;
            var chunks = Asset.GetChunksById(fileid, out chunkSize, out length);
            if (chunks == null)
                return;
            answer.Packet.Write(chunkSize);
            answer.Packet.Write(length);
            answer.Packet.Write(chunks.Length); // Is this needed?
            foreach (var chunk in chunks)
                answer.Packet.Write(chunk.ToByteArray());
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets asset chunk binary data
        /// </summary>
        /// <param name="msg"></param>
        public static void GetAssetChunk(Message msg)
        {
            var chunkid = new ObjectId(msg.Packet.ReadBytes(12));
            var answer = msg.Answer(EraSClient);
            var data = Asset.GetChunkById(chunkid);
            if (data == null)
                return;
            answer.Packet.Write(data.Bytes.Length); // Is this needed?
            answer.Packet.Write(data.Bytes);
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
