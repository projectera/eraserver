using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Net;
using System.IO;

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
        /// Server address reference
        /// </summary>
        public static MongoServerAddress ServerAddress { get { return Server.Instance.Address; } }

        /// <summary>
        /// Database reference
        /// </summary>
        public static MongoDatabase Database { get; protected set; }

        /// <summary>
        /// Time of last HeartBeat
        /// </summary>
        public static DateTime HeartBeatTime { get; protected set; }

        /// <summary>
        /// Has flatlined boolean
        /// </summary>
        public static Boolean HasFlatlined
        {
            get
            {
                return NetTime.Now - _beatTime > 60 * FlatlineTime;
            }
        }

        /// <summary>
        /// Gets the heartbeat document
        /// </summary>
        public static BsonDocument Document { get
            {
                return new BsonDocument()
                    .Add("_id", HeartBeatService.Identifier)
                    .Add("HeartBeatTime", HeartBeatService.HeartBeatTime)
                    .Add("AliveTime", _beatTime)
                    .Add("IP", Utils.NetUtils.GetIPAddress().ToString());
            }
        }
        
        /// <summary>
        /// Starts the service
        /// </summary>
        public static Boolean Defibrillate()
        {
            // Create some and get some
            Identifier = ObjectId.GenerateNewId();
            
            var url = String.Empty;

            // Urls to test
            var addresses = new List<String> { 
                "http://server.projectera.org/mongo/",
                "http://era.derk-jan.org/mongo/",
                "http://amazon.derk-jan.org/era/mongo/",
            };

            // Test network url's
            while (String.IsNullOrWhiteSpace(url) && addresses.Count > 0)
            {
                var address = addresses[0];
                addresses.Remove(address);
                try
                {
                    WebClient wc = new WebClient();
                    url = wc.DownloadString(address);
                }
                catch (WebException) { }
                catch (IOException) { }
                
            }

            // Default
            if (String.IsNullOrWhiteSpace(url))
                url = "localhost";

			// HACK: don't have mongo :(
            if (url == "localhost")
            	url = "pegu.maxmaton.nl";

            // Connect to mongo
            try
            {
                Console.WriteLine("Heartbeatservice connecting to mongodb://{0}", url);
                Server = MongoServer.Create("mongodb://" + url);
                Database = Server.GetDatabase("era");
            }
            catch (Exception) { return false; }

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

            return true;
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
                if (HasFlatlined)
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

            Console.WriteLine("Heartbeatservice [local: {0}] has flatlined.", state);
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
        public static Dictionary<ObjectId, BsonDocument> GetServers()
        {
            var servers = GetCollection().FindAllAs<BsonDocument>();
            var identifiers = new Dictionary<ObjectId, BsonDocument>();
            foreach (var server in servers)
            {
                // Server is flatlinening
                if ((HeartBeatTime - server["HeartBeatTime"].AsDateTime).Minutes > FlatlineTime)
                {
                    Console.WriteLine("Heartbeatservice [{0}] has flatlined.", server["_id"]);
                    OnRemoteFlatline.Invoke(server["_id"].AsObjectId);
                }
                else
                {
                    identifiers.Add(server["_id"].AsObjectId, server);
                    Console.WriteLine("Heartbeatservice found heart [{0}].", server["_id"]);
                }
            }

            return identifiers;
        }
    }
}
