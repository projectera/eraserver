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
        public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Interval to write to the server
        /// </summary>
        public static readonly TimeSpan WriteInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 
        /// </summary>
        public static readonly Int64 CappedSize = 24 * 60;

        public static List<Dictionary<Tuple<String, String>, StatsDocument>> _history;
        public static Dictionary<String, Dictionary<String, StatsDocument>> _instanceHistory;
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

            _history = new List<Dictionary<Tuple<String, String>, StatsDocument>>() { new Dictionary<Tuple<String, String>, StatsDocument>() };
            _instanceHistory = new Dictionary<String, Dictionary<String, StatsDocument>>();
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
                    // Get all the statistics per service name and unique key
                    var stats = new Dictionary<Tuple<String, String>, NetConnectionStatistics>();
                    lock (Program.Network)
                    {
                        foreach (var service in Program.Services.Connections)
                            stats.Add(new Tuple<String, String>(service.Value.Name, service.Key),
                                service.Value.Connection.Statistics);
                    }

                    // Get all the stats
                    var docs = new Dictionary<Tuple<String, String>, StatsDocument>();
                    foreach (var stat in stats)
                    {
                        docs.Add(stat.Key, new StatsDocument()
                        {
                            Name = stat.Key.Item1,
                            ReceivedBytes = stat.Value.ReceivedBytes,
                            ReceivedPackets = stat.Value.ReceivedPackets,
                            SentBytes = stat.Value.SentBytes,
                            SentPackets = stat.Value.SentPackets,
                            ResentMessages = stat.Value.ResentMessages,
                        });
                    }

                    // Save the last of each instance
                    foreach (var doc in docs)
                    {
                        var serviceName = doc.Value.Name;
                        Dictionary<String, StatsDocument> servicedata;
                        if (!_instanceHistory.TryGetValue(serviceName, out servicedata))
                            _instanceHistory.Add(serviceName, new Dictionary<string, StatsDocument>());
                        servicedata = _instanceHistory[serviceName];

                        servicedata.Remove(doc.Key.Item2);
                        servicedata.Add(doc.Key.Item2, doc.Value);
                    }

                    //if (docs.Count == 0)
                    //    docs.Add(new Tuple<String, String>(String.Empty, String.Empty), new StatsDocument());

                    // Add those results
                    lock (_history)
                        _history.Add(docs);
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
            var pushServices = new Dictionary<String, StatsDocument>();
            lock (_history)
            {
                // Keep aggregating over the keeptime
                while (_history.Count > 1 && (
                    _history.First().Count == 0 ||
                    _history.First().Any(a => DateTime.Now.ToUniversalTime() - a.Value.Time > WriteInterval)))
                {
                    // Peek at front queue
                    var first = _history.First();

                    var frameServices = new Dictionary<String, StatsDocument>();
                    foreach (var docdict in first)
                    {
                        // Empty (no services)
                        if (docdict.Key.Item1 == docdict.Key.Item2 && docdict.Key.Item1 == String.Empty)
                            continue;

                        // Count per service name
                        StatsDocument pushIndividual;
                        if (!frameServices.TryGetValue(docdict.Key.Item1, out pushIndividual))
                        {
                            pushIndividual = new StatsDocument() { Name = docdict.Key.Item1 };
                            frameServices.Add(docdict.Key.Item1, pushIndividual);
                        }

                        frameServices[docdict.Key.Item1] = docdict.Value;
                    }

                    // Update replace by latest frame with this service
                    foreach (var service in frameServices)
                    {
                        StatsDocument serviceTotal;
                        if (pushServices.TryGetValue(service.Key, out serviceTotal))
                            pushServices.Remove(service.Key);
                        pushServices.Add(service.Key, service.Value);
                    }
                    
                    // Shift from queue
                    _history.Remove(first);
                }
            }

            //pushServices.Add("_", pushTotal);

            foreach (var push in pushServices.Values)
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
                    { "GetStatisticsTotal", GetStatisticsTotal },
                    { "GetStatisticsSlice", GetStatisticsSlice },                    
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
            var results = new Stack<Dictionary<String, StatsDocument>>();

            lock (_history)
            {
                answer.Packet.Write(_history.Count);

                // In reverse order, call the history. If a service has no delta
                // it means it was terminated that frame. Store it as delta. Each
                // frame save the difference between delta and frame value. This is
                // what was actually sent in that frame. Store the frame as the new
                // delta. If there is no frame, but there is a delta, then this frame
                // is the frame before the service started. Save it as a whole.
                //
                // [ { A:0 }, { A:1, B:0}, { A:2, B:1 }, { A:3 } ]
                //
                // * set A:3 to delta[A]
                // * save delta[A]=3 - A:2
                //   set A:2 to delta[A]
                //   set B:1 to delta[B]
                // * save delta[A]=2 - A:1
                //   save delta[B]=1 - B:0
                //   set A:1 to delta[A]
                //   set B:0 to delta[B]
                // * save delta[A]=1 - A:0
                //   save delta[B]=0
                //   set A:0 to delta[A]
                // * save delta[A] = 0
                //
                // Each * denouncens an interation.

                var deltas = new Dictionary<String, StatsDocument>();
                for (Int32 i = _history.Count - 1; i >= 0; i--)
                {
                    var frame = _history[i];
                    var aggregatedServices = new Dictionary<String, StatsDocument>();

                    // Merge the instances to stats per service
                    foreach (var serviceInstance in frame)
                    {
                        var servicename = serviceInstance.Value.Name;
                        StatsDocument aggregateStats;
                        if (!aggregatedServices.TryGetValue(servicename, out aggregateStats))
                            aggregatedServices.Add(servicename, new StatsDocument());
                        aggregatedServices[servicename] = serviceInstance.Value.Merge(aggregatedServices[servicename]);
                    }

                    // Copy the old deltas
                    Dictionary<String, StatsDocument> fixeddeltas = new Dictionary<string, StatsDocument>();
                    foreach (var delta in deltas)
                        fixeddeltas.Add(delta.Key, delta.Value);

                    // If there is no delta doc, add this doc as delta
                    var result = new Dictionary<String, StatsDocument>();
                    foreach (var aggregate in aggregatedServices)
                    {
                        StatsDocument deltadoc;
                        if (!deltas.TryGetValue(aggregate.Key, out deltadoc))
                            deltas.Add(aggregate.Key, aggregate.Value);
                    }

                    // For the the old deltas, if the current aggregation doesn't
                    // have the service, last frame was the first frame the service
                    // was up. So add that frame as a result and remove it from deltas
                    foreach (var delta in fixeddeltas)
                    {
                        StatsDocument aggredoc;
                        if (!aggregatedServices.TryGetValue(delta.Key, out aggredoc))
                        {
                            result.Add(delta.Key, delta.Value);
                            deltas.Remove(delta.Key);
                            continue;
                        }

                        // Difference since last frame
                        deltas[delta.Key] = aggredoc;

                        var diff = delta.Value.Difference(aggredoc);
                        if (diff.SentPackets == 0 && diff.ReceivedPackets == 0)
                            continue;
                        result.Add(delta.Key, diff);
                    }

                    results.Push(result);
                }

                // Add the delta's left
                var finalresult = new Dictionary<String, StatsDocument>();
                foreach (var delta in deltas)
                    finalresult.Add(delta.Key, delta.Value);

                // Push on the stack
                results.Push(finalresult);
            }

            while (results.Count > 0)
                WriteStatisticsFrame(results.Pop(), answer.Packet);

            c.SendMessage(answer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void GetStatisticsTotal(ServiceConnection c, Message m)
        {
            var answer = m.Answer(c);
            lock (_history)
            {
                // Services
                answer.Packet.Write(DateTime.Now.ToUniversalTime().ToBinary());
                answer.Packet.Write(_instanceHistory.Count);
                foreach (var service in _instanceHistory)
                {
                    var frame = new StatsDocument();
                    foreach (var instance in service.Value)
                        frame = instance.Value.Merge(frame);
                    frame.Pack(answer.Packet);
                }
            }
            c.SendMessage(answer);
        }

        /// <summary>
        /// Answers the stats in the slice
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void GetStatisticsSlice(ServiceConnection c, Message m)
        {
            //var stime = m.Packet.ReadInt64();
            //var etime = m.Packet.ReadInt64();

            var answer = m.Answer(c);
            
            Dictionary<Tuple<String, String>, StatsDocument> last = null;
            lock (_history)
                last = _history[_history.Count - 1];
            //WriteStatisticsFrame(last, answer.Packet);

            // TODO: read from mongo and/or history to compose the slice data

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
            /*else if (frame.Count == 1 && frame.First().Key == String.Empty)
            {
                buffer.Write(frame.First().Value.Time.ToBinary());
                buffer.Write(0);
            } */
            else 
            {
                buffer.Write(frame.First().Value.Time.ToBinary());
                buffer.Write(frame.Count);

                // TODO: aggregate per server
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
            public StatsDocument Merge(StatsDocument doc, Boolean saveId = true)
            {
                return new StatsDocument()
                {
                    Id = this.Id,
                    Name = this.Name ?? doc.Name,
                    ReceivedBytes = this.ReceivedBytes + doc.ReceivedBytes,
                    ReceivedPackets = this.ReceivedPackets + doc.ReceivedPackets,
                    SentBytes = this.SentBytes + doc.SentBytes,
                    SentPackets = this.SentPackets + doc.SentPackets,
                    ResentMessages = this.ResentMessages + doc.ResentMessages,
                };
            }

            /// <summary>
            /// Subtracts doc from this
            /// </summary>
            /// <param name="doc"></param>
            /// <returns></returns>
            public StatsDocument Difference(StatsDocument doc, Boolean saveId = true)
            {
                return new StatsDocument()
                {
                    Id = this.Id,
                    Name = this.Name ?? doc.Name,
                    ReceivedBytes = this.ReceivedBytes - doc.ReceivedBytes,
                    ReceivedPackets = this.ReceivedPackets - doc.ReceivedPackets,
                    SentBytes = this.SentBytes - doc.SentBytes,
                    SentPackets = this.SentPackets - doc.SentPackets,
                    ResentMessages = this.ResentMessages - doc.ResentMessages,
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
