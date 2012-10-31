using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using EraS.Listeners;
using EraS.Connections;
using Lidgren.Network;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ServiceProtocol;

namespace EraS.Services
{
    public static class StatisticsService
    {
        public const String SERVICE_VERSION = "1.0.0";

        /// <summary>
        /// Interval to get the statistics
        /// </summary>
        public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Interval to write to the server
        /// </summary>
        public static readonly TimeSpan WriteInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 
        /// </summary>
        public static readonly Int64 CappedSize = 24 * 60;

        public static List<Dictionary<String, StatsDocument>> _history;
        private static Timer _timer, _writeTimer;
        private static Object _writeLock = new Object();

        /// <summary>
        /// Starts the statistics service
        /// </summary>
        public static void Start()
        {
            // check for version and branch
            if (!HeartBeatService.Database.CollectionExists("Stats"))
                HeartBeatService.Database.CreateCollection("Stats", CollectionOptions.SetMaxDocuments(CappedSize).SetMaxSize(CappedSize * 2048).SetCapped(true));

            _history = new List<Dictionary<String, StatsDocument>>() { new Dictionary<String, StatsDocument>() };
            Program.Services.OnDisconnect += new Action<ServiceConnection>(Services_OnDisconnect);

            _timer = new Timer(Tick, null, TimeSpan.Zero, TickInterval);
            _writeTimer = new Timer(PushHistory, null, WriteInterval, WriteInterval);
        }

        /// <summary>
        /// On service disconnect
        /// </summary>
        /// <param name="obj"></param>
        private static void Services_OnDisconnect(ServiceConnection obj)
        {
            //  If we are in the tick lock, we need to wait
            while (!Monitor.TryEnter(_writeLock))
            {
                Thread.MemoryBarrier();
                Thread.Sleep(100);
            }
            Monitor.Exit(_writeLock);

       
            // TODO: write this copy to the history queue because
            // the tick function will not be able to get it, since
            // the connection no longer exists.
        }

        /// <summary>
        /// One statistics tick
        /// </summary>
        /// <param name="state"></param>
        private static void Tick(Object state)
        {
            // We only want one tick call at the same time
            if (Monitor.TryEnter(_writeLock))
            {
                try
                {
                    Dictionary<String, StatsDocument> lasts = null;
                    lock (_history)
                        lasts = _history[_history.Count - 1];

                    // Get all the statistics per service name, unique key
                    var stats = new Dictionary<Tuple<String, String>, NetConnectionStatistics>();
                    lock (Program.Network)
                    {
                        foreach (var service in Program.Services.Connections)
                        {
                            Topography.Service topography;
                            if (Program.Network.ServiceInstances.TryGetValue(service.Key, out topography))
                            {
                                stats.Add(new Tuple<String, String>(topography.Name, service.Key),
                                    service.Value.Connection.Statistics);
                            }
                        }
                    }

                    // Aggregate per service name
                    var sums = new Dictionary<String, StatsDocument>();
                    foreach (var stat in stats)
                    {
                        StatsDocument service;
                        if (!sums.TryGetValue(stat.Key.Item1, out service))
                        {
                            service = new StatsDocument() { Name = stat.Key.Item1 };
                            sums.Add(stat.Key.Item1, service);
                        }

                        sums[stat.Key.Item1] = service.Merge(new StatsDocument()
                        {
                            Name = stat.Key.Item1,
                            ReceivedBytes = stat.Value.ReceivedBytes,
                            ReceivedPackets = stat.Value.ReceivedPackets,
                            SentBytes = stat.Value.SentBytes,
                            SentPackets = stat.Value.SentPackets,
                            ResentMessages = stat.Value.ResentMessages,
                        });
                    }

                    // Comparision with previous frames
                    var results = new Dictionary<String, StatsDocument>();
                    foreach (var stat in sums)
                    {
                        StatsDocument last;
                        if (!lasts.TryGetValue(stat.Key, out last))
                            last = new StatsDocument() { Name = stat.Key };


                        // TODO: fix this. Off course this doesn't work xD - incrementing with the difference?
                        results.Add(stat.Key, new StatsDocument()
                        {
                            Name = stat.Key,
                            ReceivedBytes = stat.Value.ReceivedBytes, // - last.ReceivedBytes,
                            ReceivedPackets = stat.Value.ReceivedPackets, // - last.ReceivedPackets,
                            SentBytes = stat.Value.SentBytes, // - last.SentBytes,
                            SentPackets = stat.Value.SentPackets, // - last.SentPackets,
                            ResentMessages = stat.Value.ResentMessages, // - last.ResentMessages,
                        });
                    }

                    // Add those results
                    lock (_history)
                        _history.Add(results);
                }
                catch (Exception)
                {
                    // TODO exception reporting
                } 

                Monitor.Exit(_writeLock);
            }
        }

        /// <summary>
        /// Pushes history to the server (aggregates all data)
        /// </summary>
        /// <param name="state"></param>
        private static void PushHistory(Object state)
        {
            var pushTotal = new StatsDocument() { Name = "_" };
            var pushIndividuals = new Dictionary<String, StatsDocument>();
            lock (_history)
            {
                // Keep aggregating over the keeptime
                while (_history.Count > 1 && (
                    _history.First().Count == 0 ||
                    _history.First().Any(a => DateTime.Now.ToUniversalTime() - a.Value.Time > WriteInterval)))
                {
                    // Peek at front queue
                    var first = _history.First();
                    foreach (var docdict in first)
                    {
                        // Make a total counter
                        pushTotal = pushTotal.Merge(docdict.Value);

                        // Count per service name too
                        StatsDocument pushIndividual;
                        if (!pushIndividuals.TryGetValue(docdict.Key, out pushIndividual))
                        {
                            pushIndividual = new StatsDocument() { Name = docdict.Key };
                            pushIndividuals.Add(docdict.Key, pushIndividual);
                        }
                        pushIndividuals[docdict.Key] = pushIndividual.Merge(docdict.Value);
                    }

                    // Shift from queue
                    _history.Remove(first);
                }
            }

            pushIndividuals.Add("_", pushTotal);

            foreach (var push in pushIndividuals.Values)
                GetCollection().Save(push);
        }

        /// <summary>
        /// Gets the statistics collection
        /// </summary>
        /// <returns></returns>
        public static MongoCollection<StatsDocument> GetCollection()
        {
            return HeartBeatService.Database.GetCollection<StatsDocument>("Stats");
        }

        /// <summary>
        /// Functions provided by this service
        /// </summary>
        public static Dictionary<String, Action<ServiceConnection, Message>> Functions
        {
            get
            {
                return new Dictionary<String, Action<ServiceConnection, Message>>() {
                    { "GetStatisticsVersion", GetStatisticsVersion },
                    { "GetStatistics" , GetStatistics }, 
                    { "GetStatisticsFrame", GetStatisticsFrame },                    
                };
            }
        }

        /// <summary>
        /// Gets tatistics version
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void GetStatisticsVersion(ServiceConnection c, Message m)
        {
            var answer = m.Answer(c);
            answer.Packet.Write(SERVICE_VERSION);
            c.SendMessage(answer);
        }

        /// <summary>
        /// Answers with all the frames
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void GetStatistics(ServiceConnection c, Message m)
        {
            var answer = m.Answer(c);
            lock (_history)
            {
                answer.Packet.Write(_history.Count);
                foreach(var frame in _history)
                    WriteStatisticsFrame(frame, answer.Packet);
            }

            c.SendMessage(answer);
        }

        /// <summary>
        /// Answers with the last frame 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void GetStatisticsFrame(ServiceConnection c, Message m)
        {
            var answer = m.Answer(c);
            
            Dictionary<String, StatsDocument> last = null;
            lock (_history)
                last = _history[_history.Count - 1];
            WriteStatisticsFrame(last, answer.Packet);

            c.SendMessage(answer);
        }

        /// <summary>
        /// Writes a single statistics frame to a netbuffer
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="buffer"></param>
        private static void WriteStatisticsFrame(Dictionary<String, StatsDocument> frame, NetBuffer buffer)
        {
            if (frame.Count == 0)
            {
                buffer.Write(new StatsDocument().Time.ToBinary());
                buffer.Write(0);
            }
            else
            {
                buffer.Write(frame.First().Value.Time.ToBinary());
                buffer.Write(frame.Count);
                foreach (var service in frame)
                    service.Value.Pack(buffer);
            }
        }

        /// <summary>
        /// Get Mongo address
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void GetMongo(ServiceConnection c, Message m)
        {
            var ans = m.Answer(c);
            ans.Packet.Write(HeartBeatService.ServerAddress.Host);
            ans.Packet.Write(HeartBeatService.ServerAddress.Port);
            c.SendMessage(ans);
        }

        /// <summary>
        /// 
        /// </summary>
        public class StatsDocument : BsonDocument 
        {
            /// <summary>
            /// Id for the document
            /// </summary>
            [BsonId]
            public ObjectId Id;

            /// <summary>
            /// Time of creation
            /// </summary>
            public DateTime Time { get { return Id.CreationTime; }}

            /// <summary>
            /// Name of the service
            /// </summary>
            public String Name;

            /// <summary>
            /// Number of bytes received
            /// </summary>
            public Int32 ReceivedBytes;

            /// <summary>
            /// Number of packets recieved
            /// </summary>
            public Int32 ReceivedPackets;

            /// <summary>
            /// Number of bytes sent
            /// </summary>
            public Int32 SentBytes;

            /// <summary>
            /// Number of packets sent
            /// </summary>
            public Int32 SentPackets;

            /// <summary>
            ///  Number of messages resent (reliable)
            /// </summary>
            public Int32 ResentMessages;

            /// <summary>
            /// Creates a new stats document
            /// </summary>
            public StatsDocument() {
                Id = ObjectId.GenerateNewId();
            }

            /// <summary>
            /// Merges this and doc in new document
            /// </summary>
            /// <param name="doc"></param>
            /// <returns>New merged doc</returns>
            public StatsDocument Merge(StatsDocument doc)
            {
                return new StatsDocument()
                {
                    Name = this.Name ?? doc.Name,
                    ReceivedBytes = this.ReceivedBytes + doc.ReceivedBytes,
                    ReceivedPackets = this.ReceivedPackets + doc.ReceivedPackets,
                    SentBytes = this.SentBytes + doc.SentBytes,
                    SentPackets = this.SentPackets + doc.SentPackets,
                    ResentMessages = this.ResentMessages = doc.ResentMessages,
                };
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="netBuffer"></param>
            internal void Pack(NetBuffer buffer)
            {
                buffer.Write(Id.ToByteArray());
                buffer.Write(Name);
                buffer.Write(ReceivedBytes);
                buffer.Write(ReceivedPackets);
                buffer.Write(SentBytes);
                buffer.Write(SentPackets);
                buffer.Write(ResentMessages);
            }
        }
    }
}
