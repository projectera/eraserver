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

namespace EraS
{
    public static class StatisticsService
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan WriteInterval = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan KeepTime = TimeSpan.FromHours(1);

        public static readonly Int64 CappedSize = 24 * 60;

        public static List<Dictionary<String, StatsDocument>> _history;

        private static Timer _timer, _writeTimer;

        /// <summary>
        /// 
        /// </summary>
        public static void Start()
        {
            if (!HeartBeatService.Database.CollectionExists("Stats"))
                HeartBeatService.Database.CreateCollection("Stats", CollectionOptions.SetMaxDocuments(CappedSize).SetMaxSize(CappedSize * 2048).SetCapped(true));

            _history = new List<Dictionary<String, StatsDocument>>() { new Dictionary<String, StatsDocument>() };
            _timer = new Timer(Tick, null, TimeSpan.Zero, TickInterval);
            _writeTimer = new Timer(PushHistory, null, WriteInterval, WriteInterval);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void Tick(Object state)
        {
            Dictionary<String, StatsDocument> lasts = null;
            lock (_history)
                lasts = _history[_history.Count - 1];

            var stats = new Dictionary<String, NetConnectionStatistics>();
            lock (Program.Network)
                foreach (var service in Program.Services.Connections)
                    stats.Add(service.Key, service.Value.Connection.Statistics);

            var results = new Dictionary<String, StatsDocument>();
            foreach (var stat in stats)
            {
                StatsDocument last;
                if (!lasts.TryGetValue(stat.Key, out last))
                    last = new StatsDocument() { Name = stat.Key };

                results.Add(stat.Key, new StatsDocument()
                {
                    Name = stat.Key,
                    ReceivedBytes = stat.Value.ReceivedBytes - last.ReceivedBytes,
                    ReceivedPackets = stat.Value.ReceivedPackets - last.ReceivedPackets,
                    SendBytes = stat.Value.SentBytes - last.SendBytes,
                    SendPackets = stat.Value.SentPackets - last.SendPackets,
                    ResentMessages = stat.Value.ResentMessages - last.ResentMessages,
                });
            }

            lock (_history)
                _history.Add(results);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private static void PushHistory(Object state)
        {
            var pushTotal = new StatsDocument() { Name = "_" };
            var pushIndividuals = new Dictionary<String, StatsDocument>();
            lock (_history)
            {
                while (_history.First().Count == 0 || _history.First().Any(a => a.Value.Time - DateTime.Now > KeepTime))
                {
                    var first = _history.First();
                    foreach (var docdict in first)
                    {
                        pushTotal = pushTotal.Merge(docdict.Value);

                        StatsDocument pushIndividual;
                        if (!pushIndividuals.TryGetValue(docdict.Key, out pushIndividual))
                        {
                            pushIndividual = new StatsDocument() { Name = docdict.Key };
                            pushIndividuals.Add(docdict.Key, pushIndividual);
                        }
                        pushIndividuals[docdict.Key] = pushIndividual.Merge(docdict.Value);
                    }
                    _history.RemoveAt(0);
                }
            }

            pushIndividuals.Add("_", pushTotal);

            foreach (var push in pushIndividuals.Values)
                GetCollection().Save(push);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static MongoCollection<StatsDocument> GetCollection()
        {
            return HeartBeatService.Database.GetCollection<StatsDocument>("Stats");
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
            /// 
            /// </summary>
            public String Name;

            public Int32 ReceivedBytes;
            public Int32 ReceivedPackets;
            public Int32 SendBytes;
            public Int32 SendPackets;
            public Int32 ResentMessages;

            /// <summary>
            /// 
            /// </summary>
            public StatsDocument() {
                Id = ObjectId.GenerateNewId();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="doc"></param>
            /// <returns></returns>
            public StatsDocument Merge(StatsDocument doc)
            {
                return new StatsDocument()
                {
                    Name = this.Name ?? doc.Name,
                    ReceivedBytes = this.ReceivedBytes + doc.ReceivedBytes,
                    ReceivedPackets = this.ReceivedPackets + doc.ReceivedPackets,
                    SendBytes = this.SendBytes + doc.SendBytes,
                    SendPackets = this.SendPackets + doc.SendPackets,
                    ResentMessages = this.ResentMessages = doc.ResentMessages,
                };
            }
        }
    }
}
