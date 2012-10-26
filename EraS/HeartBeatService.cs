using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace EraS
{
    /// <summary>
    /// Heartbeat delegate
    /// </summary>
    /// <param name="state">state</param>
    public delegate void HeartBeat(ObjectId state);

    /// <summary>
    /// 
    /// </summary>
    public class HeartBeatService
    {
        public const Double FlatlineTime = 5;
        public const Double HeartBeatInterval = 1;

        protected static Timer _timer;
        protected static Double _beatTime;

        /// <summary>
        /// Event is called when the heart beats
        /// </summary>
        public static event HeartBeat OnBeat = delegate { };

        /// <summary>
        /// Event is called when the heart stopped beating
        /// </summary>
        public static event HeartBeat OnFlatline = delegate { };

        /// <summary>
        /// Event is called when another server stopped beating
        /// </summary>
        public static event HeartBeat OnRemoteFlatline = delegate { };

        /// <summary>
        /// Identifier for this instance
        /// </summary>
        public static ObjectId Identifier { get; protected set; }
        
        /// <summary>
        /// Server reference
        /// </summary>
        public static MongoServer Server { get; protected set; }

        /// <summary>
        /// Database reference
        /// </summary>
        public static MongoDatabase Database { get; protected set; }

        /// <summary>
        /// Time of last HeartBeat
        /// </summary>
        public static DateTime HeartBeatTime { get; protected set; }

        /// <summary>
        /// Gets the heartbeat document
        /// </summary>
        public static BsonDocument Document { get
            {
                return new BsonDocument()
                    .Add("_id", HeartBeatService.Identifier)
                    .Add("HeartBeatTime", HeartBeatService.HeartBeatTime)
                    .Add("AliveTime", _beatTime);
            }
        }
        
        /// <summary>
        /// Starts the service
        /// </summary>
        public static void Defibrillate()
        {
            // Create some and get some
            Identifier = ObjectId.GenerateNewId();
            Server = MongoServer.Create("mongodb://localhost");
            Database = Server.GetDatabase("era");

            // Create the collection
            if (!Database.CollectionExists("Servers"))
                Database.CreateCollection("Servers", CollectionOptions.SetAutoIndexId(true));

            // Make sure it's an Time To Live collection
            GetCollection().EnsureIndex(
                IndexKeys.Ascending("HeartBeatTime"), 
                IndexOptions.SetTimeToLive(
                    TimeSpan.FromMinutes(FlatlineTime * 2)
                )
            );

            OnBeat += new HeartBeat(HeartBeatService_OnBeat);

            // Start beating this heart
            _timer = new Timer(
                HeartBeatService.Beat,
                HeartBeatService.Identifier,
                TimeSpan.FromMinutes(0),
                TimeSpan.FromMinutes(HeartBeatInterval)
            );
        }

        /// <summary>
        /// Beats the heart
        /// </summary>
        /// <param name="state"></param>
        protected static void Beat(Object state)
        {
            HeartBeatTime = DateTime.Now.ToUniversalTime();

            try
            {
                var upsert = GetCollection().FindAndModify(
                    Query.EQ("_id", Identifier),
                    SortBy.Null,
                    Update.Replace(Document),
                    false,
                    true
                );

                if (upsert.Ok)
                {
                    _beatTime = NetTime.Now;
                    OnBeat.Invoke(Identifier);
                }
                else
                {
                    throw new Exception(upsert.ErrorMessage);
                }
            }
            catch (Exception)
            {
                if (NetTime.Now - _beatTime > FlatlineTime)
                    Flatline(state);
            }
        }

        /// <summary>
        /// Runs when heartbeat succeeded
        /// </summary>
        /// <param name="state"></param>
        protected static void HeartBeatService_OnBeat(ObjectId state)
        {
            GetServers();
        }


        /// <summary>
        /// Stops beating the heart
        /// </summary>
        protected static void Flatline(Object state)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            OnFlatline.Invoke(Identifier);
        }

        /// <summary>
        /// Gets the heartbeat service server collection
        /// </summary>
        /// <returns></returns>
        protected static MongoCollection GetCollection()
        {
            return Database.GetCollection("Servers");
        }

        /// <summary>
        /// Gets servers
        /// </summary>
        /// <returns>List of identifiers</returns>
        protected static List<ObjectId> GetServers()
        {
            var servers = GetCollection().FindAllAs<BsonDocument>();
            var identifiers = new List<ObjectId>();
            foreach (var server in servers)
            {
                // Server is flatlinening
                if ((HeartBeatTime - server["HeartBeatTime"].AsDateTime).Minutes > FlatlineTime)
                {
                    OnRemoteFlatline.Invoke(server["_id"].AsObjectId);
                }
                else
                {
                    identifiers.Add(server["_id"].AsObjectId);
                }
            }

            return identifiers;
        }
    }
}
