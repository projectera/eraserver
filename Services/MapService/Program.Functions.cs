using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERA.Protocols.ServiceProtocol;
using MongoDB.Bson;
using ERA.Protocols.MapProtocol;
using System.Threading;

namespace ERA.Services.Map
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

            // InteractableJoinMap
            // InteractableLeaveMap

            Functions.Add("Get", Get);
            Functions.Add("GetTileset", GetTileset);
            Functions.Add("GetMapTileset", GetMapTileset);
            Functions.Add("GetHash", GetHash); 
            Functions.Add("GetTilesetHash", GetTilesetHash);
            Functions.Add("GetMapTilesetHash", GetMapTilesetHash);
            //Functions.Add("GetResources", GetResources); // Tileset + Autotile Graphics
            //Functions.Add("GetResourceHashes", GetResourceHashes);

            // TransferMapRequest

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
            var maps = MapInstances.GetKeys();
            foreach (var map in maps)
            {
                answer.Packet.Write(map.ToByteArray());

                // Write the instance ids
                var keys = MapInstances.GetKeysOf(map);
                if (keys == null)
                    answer.Packet.Write((Int32)0);
                else
                    answer.Packet.Write(keys.Count);
                foreach (var key in keys)
                    answer.Packet.Write(key.ToByteArray());
            }

            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets the instance keys for the running instances
        /// </summary>
        /// <param name="msg"></param>
        public static void GetRunningInstances(Message msg)
        {
            var mapId = new ObjectId(msg.Packet.ReadBytes(12));
            var answer = msg.Answer(EraSClient);

            var keys = MapInstances.GetKeysOf(mapId);
            if (keys == null)
                answer.Packet.Write((Int32)0);
            else
                answer.Packet.Write(keys.Count);
            foreach (var key in keys)
                answer.Packet.Write(key.ToByteArray());

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
        /// Gets map data
        /// </summary>
        /// <param name="msg"></param>
        public static void Get(Message msg)
        {
            var mapId = new ObjectId(msg.Packet.ReadBytes(12));
            var mapData = __GetMapData(mapId);
            if (mapData == null)
                return;
            var answer = msg.Answer(EraSClient);
            mapData.Write(answer.Packet);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets map data from active, cache or database
        /// </summary>
        /// <param name="mapid"></param>
        /// <returns></returns>
        protected static ERA.Protocols.MapProtocol.Map __GetMapData(ObjectId mapId)
        {
            var instances = MapInstances.GetKeysOf(mapId);

            ERA.Protocols.MapProtocol.Map mapData = null;
            if (instances != null && instances.Count > 0)
            {
                var instance = MapInstances.GetValueOf(mapId, instances.First());
                if (instance != null)
                    mapData = instance.MapData;
            }

            return mapData ?? Data.Map.GetBlocking(mapId, true);
        }

        /// <summary>
        /// Gets the tileset data
        /// </summary>
        /// <param name="msg"></param>
        public static void GetTileset(Message msg)
        {
            var tilesetId = new ObjectId(msg.Packet.ReadBytes(12));
            var tileset = Data.Tileset.GetBlocking(tilesetId, true);
            if (tileset == null)
                return;
            var answer = msg.Answer(EraSClient);
            tileset.Write(answer.Packet);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets the tileset data for a map
        /// </summary>
        /// <param name="msg"></param>
        public static void GetMapTileset(Message msg)
        {
            var mapId = new ObjectId(msg.Packet.ReadBytes(12));
            var mapData = __GetMapData(mapId);
            if (mapData == null)
                return;
            var tileset = Data.Tileset.GetBlocking(mapData.TilesetId, true);
            if (tileset == null)
                return;
            var answer = msg.Answer(EraSClient);
            tileset.Write(answer.Packet);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets the map data hash
        /// </summary>
        /// <param name="msg"></param>
        public static void GetHash(Message msg)
        {
            var mapId = new ObjectId(msg.Packet.ReadBytes(12));
            var mapData = __GetMapData(mapId);
            if (mapData == null)
                return;
            var answer = msg.Answer(EraSClient);
            answer.Packet.Write(mapData.GetHashCode());
            answer.Packet.Write(mapData.Version);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets the tileset hash
        /// </summary>
        /// <param name="msg"></param>
        public static void GetTilesetHash(Message msg)
        {
            var tilesetId = new ObjectId(msg.Packet.ReadBytes(12));
            var tileset = Data.Tileset.GetBlocking(tilesetId, true);
            var answer = msg.Answer(EraSClient);
            answer.Packet.Write(tileset.GetHashCode());
            answer.Packet.Write(tileset.Version);
            EraSClient.SendMessage(answer);
        }

        /// <summary>
        /// Gets the tileset hash from a map
        /// </summary>
        /// <param name="msg"></param>
        public static void GetMapTilesetHash(Message msg)
        {
            var mapId = new ObjectId(msg.Packet.ReadBytes(12));
            var mapData = __GetMapData(mapId);
            if (mapData == null)
                return;
            var tileset = Data.Tileset.GetBlocking(mapData.TilesetId, true);
            if (tileset == null)
                return;
            var answer = msg.Answer(EraSClient);
            answer.Packet.Write(tileset.GetHashCode());
            answer.Packet.Write(tileset.Version);
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
