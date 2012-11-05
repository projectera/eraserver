using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Net;
using System.IO;
using EraS.Connections;
using ServiceProtocol;

namespace EraS.Services
{
    /// <summary>
    /// Heartbeat delegate
    /// </summary>
    /// <param name="state">state</param>
    public delegate void HeartBeat(ObjectId state);

    /// <summary>
    /// 
    /// </summary>
    public static class HeartBeatService
    {
        public static readonly TimeSpan FlatlineTime = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan HeartBeatInterval = TimeSpan.FromMinutes(1);

        private static Timer _timer;
        private static Double _beatTime;

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
        public static ObjectId Identifier { get; private set; }
        
        /// <summary>
        /// Server reference
        /// </summary>
        public static MongoServer Server { get; private set; }

        /// <summary>
        /// Server address reference
        /// </summary>
        public static MongoServerAddress ServerAddress { get { return Server.Instance.Address; } }

        /// <summary>
        /// Database reference
        /// </summary>
        public static MongoDatabase Database { get; private set; }

        /// <summary>
        /// Time of last HeartBeat
        /// </summary>
        public static DateTime HeartBeatTime { get; private set; }

        /// <summary>
        /// Has flatlined boolean
        /// </summary>
        public static Boolean HasFlatlined
        {
            get
            {
                return TimeSpan.FromSeconds(NetTime.Now - _beatTime) > FlatlineTime;
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
                    if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                    {
                        url = String.Empty;
                        throw new WebException("The Uri retrieved is malformed.");
                    }
                }
                catch (WebException) { }
                catch (IOException) { }                
            }

            // Default fallback
            if (String.IsNullOrWhiteSpace(url))
                url = "localhost";

            // Connect to mongo
            try
            {
                Console.WriteLine("Heartbeatservice connecting to mongodb://{0}.", url);
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
                    FlatlineTime.Add(FlatlineTime)
                )
            );

            OnBeat += new HeartBeat(HeartBeatService_OnBeat);

            // Start beating this heart
            _timer = new Timer(
                HeartBeatService.Beat,
                HeartBeatService.Identifier,
                TimeSpan.Zero,
                HeartBeatInterval
            );

            return true;
        }

        /// <summary>
        /// Beats the heart
        /// </summary>
        /// <param name="state"></param>
        private static void Beat(Object state)
        {
            if (Monitor.TryEnter(_timer))
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
                catch (Exception e)
                {
                    // TODO Exception handling
                    Console.WriteLine("Beat error: {0}", e.Message);
                    if (HasFlatlined)
                        Flatline(state);
                }

                Monitor.Exit(_timer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Cleanup()
        {
            lock (_timer)
            {
                var time = DateTime.Now.ToUniversalTime() - FlatlineTime;
                var doc = Document;
                doc["HeartBeatTime"] = time;

                try
                {
                    var upsert = GetCollection().FindAndModify(
                        Query.EQ("_id", doc["_id"]),
                        SortBy.Null,
                        Update.Replace(doc),
                        false,
                        true
                    );

                    if (upsert.Ok)
                    {
                        upsert = GetCollection().FindAndModify(
                            Query.EQ("IP", Utils.NetUtils.GetIPAddress().ToString()),
                            SortBy.Null,
                            Update.Replace(doc),
                            false,
                            true
                        );
                    }
                }
                catch (MongoException) { }
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Runs when heartbeat succeeded
        /// </summary>
        /// <param name="state"></param>
        private static void HeartBeatService_OnBeat(ObjectId state)
        {
            try
            {
                GetServers();
            }
            catch (Exception)
            {

            }
        }


        /// <summary>
        /// Stops beating the heart
        /// </summary>
        private static void Flatline(Object state)
        {
            lock(_timer)
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            OnFlatline.Invoke(Identifier);

            Console.WriteLine("Heartbeatservice [local: {0}] has flatlined.", state);
        }

        /// <summary>
        /// Gets the heartbeat service server collection
        /// </summary>
        /// <returns></returns>
        private static MongoCollection GetCollection()
        {
            return Database.GetCollection("Servers");
        }


        private static List<ObjectId> _flatlined = new List<ObjectId>(), 
            _known = new List<ObjectId>();

        /// <summary>
        /// Gets servers
        /// </summary>
        /// <returns>List of identifiers</returns>
        public static Dictionary<ObjectId, BsonDocument> GetServers()
        {
            var previousflat = new List<ObjectId>();
            foreach (var flatline in _flatlined)
                previousflat.Add(flatline);

            var servers = GetCollection().FindAllAs<BsonDocument>();
            var identifiers = new Dictionary<ObjectId, BsonDocument>();
            var me = Utils.NetUtils.GetIPAddress();

            foreach (var server in servers)
            {
                IPAddress ip;
                var identifier = server["_id"].AsObjectId;
                // Server is flatlinening
                if ((HeartBeatTime - server["HeartBeatTime"].AsDateTime.ToUniversalTime()) > FlatlineTime)
                {
                    if (!_flatlined.Contains(identifier))
                    {
                        OnRemoteFlatline.Invoke(identifier);

                        _flatlined.Add(identifier);
                        _known.Remove(identifier);
                    }
                    else
                    {
                        previousflat.Remove(identifier);
                    }
                }
                else if (IPAddress.TryParse(server["IP"].AsString, out ip) && !IPAddress.None.Equals(ip) && !ip.Equals(me))
                {
                    identifiers.Add(identifier, server);
                    if (!_known.Contains(identifier))
                        _known.Add(identifier);
                }
            }

            foreach (var dead in previousflat)
                _flatlined.Remove(dead);

            return identifiers;
        }
    }
}
