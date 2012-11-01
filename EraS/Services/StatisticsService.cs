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
    /// <summary>
    /// 
    /// </summary>
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

        public static List<Dictionary<Tuple<String, String>, StatisticsInfo.Document>> _history;
        public static Dictionary<String, Dictionary<String, StatisticsInfo.Document>> _instanceHistory;
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

            _history = new List<Dictionary<Tuple<String, String>, StatisticsInfo.Document>>() { new Dictionary<Tuple<String, String>, StatisticsInfo.Document>() };
            _instanceHistory = new Dictionary<String, Dictionary<String, StatisticsInfo.Document>>();
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
                   
            // Write this copy to the history queue because
            // the tick function will not be able to get it, since
            // the connection no longer exists.
            lock (_history)
            {
                var servicename = obj.Name;
                var servicekey = new Tuple<String, String>(servicename, obj.RemoteIdentifier);

                // Get or create last frame
                var last = _history.LastOrDefault();
                var stats = obj.Connection.Statistics;
                if (last == null)
                {
                    _history.Add(new Dictionary<Tuple<String, String>, StatisticsInfo.Document>());
                    last = _history.Last();
                }
                last.Remove(servicekey);

                // Create doc
                var doc = new StatisticsInfo.Document()
                {
                    Name = servicename,
                    ReceivedBytes = stats.ReceivedBytes,
                    ReceivedPackets = stats.ReceivedPackets,
                    SentBytes = stats.SentBytes,
                    SentPackets = stats.SentPackets,
                    ResentMessages = stats.ResentMessages,
                };
                last.Add(servicekey, doc);

                // Register in instance history
                Dictionary<String, StatisticsInfo.Document> servicedata;
                if (!_instanceHistory.TryGetValue(servicename, out servicedata))
                    _instanceHistory.Add(servicename, new Dictionary<string, StatisticsInfo.Document>());
                servicedata = _instanceHistory[servicename];

                servicedata.Remove(obj.RemoteIdentifier);
                servicedata.Add(obj.RemoteIdentifier, doc);
            }

            Monitor.Exit(_writeLock);
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
                        {
                            var servicekey = new Tuple<String, String>(service.Value.Name, service.Key);
                            // TODO: on disconnect, add to history
                            // this makes sure only active services (connected) are added
                            if (Program.Network.ServiceInstances.ContainsKey(service.Key))
                            {
                                stats.Add(servicekey, service.Value.Connection.Statistics);
                            }
                        }

                    }

                    // Get all the stats
                    var docs = new Dictionary<Tuple<String, String>, StatisticsInfo.Document>();
                    foreach (var stat in stats)
                    {
                        docs.Add(stat.Key, new StatisticsInfo.Document()
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
                        Dictionary<String, StatisticsInfo.Document> servicedata;
                        if (!_instanceHistory.TryGetValue(serviceName, out servicedata))
                            _instanceHistory.Add(serviceName, new Dictionary<string, StatisticsInfo.Document>());
                        servicedata = _instanceHistory[serviceName];

                        servicedata.Remove(doc.Key.Item2);
                        servicedata.Add(doc.Key.Item2, doc.Value);
                    }

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
            var pushServices = new Dictionary<String, StatisticsInfo.Document>();

            lock (_history)
            {
                // Keep aggregating over the keeptime
                while (_history.Count > 1 && (
                    _history.First().Count == 0 ||
                    _history.First().Any(a => DateTime.Now.ToUniversalTime() - a.Value.Time > WriteInterval)))
                {
                    // Peek at front queue
                    var first = _history.First();

                    var frameServices = new Dictionary<String, StatisticsInfo.Document>();
                    foreach (var docdict in first)
                    {
                        // Empty (no services)
                        if (docdict.Key.Item1 == docdict.Key.Item2 && docdict.Key.Item1 == String.Empty)
                            continue;

                        // Count per service name
                        StatisticsInfo.Document pushIndividual;
                        if (!frameServices.TryGetValue(docdict.Key.Item1, out pushIndividual))
                        {
                            pushIndividual = new StatisticsInfo.Document() { Name = docdict.Key.Item1 };
                            frameServices.Add(docdict.Key.Item1, pushIndividual);
                        }

                        frameServices[docdict.Key.Item1] = docdict.Value;
                    }

                    // Update replace by latest frame with this service
                    foreach (var service in frameServices)
                    {
                        StatisticsInfo.Document serviceTotal;
                        if (pushServices.TryGetValue(service.Key, out serviceTotal))
                            pushServices.Remove(service.Key);
                        pushServices.Add(service.Key, service.Value);
                    }
                    
                    // Shift from queue
                    _history.Remove(first);
                }
            }

            // TODO: add SERVER and push DELTAS
            //foreach (var push in pushServices.Values)
            //    GetCollection().Save(push);
        }

        /// <summary>
        /// Gets the statistics collection
        /// </summary>
        /// <returns></returns>
        public static MongoCollection<StatisticsInfo.Document> GetCollection()
        {
            return HeartBeatService.Database.GetCollection<StatisticsInfo.Document>("Stats");
        }

        /// <summary>
        /// Creates a deepclone of the current history
        /// </summary>
        /// <returns></returns>
        public static List<Dictionary<Tuple<String, String>, StatisticsInfo.Document>> GetHistory()
        {
            var result = new List<Dictionary<Tuple<String, String>, StatisticsInfo.Document>>();
            lock (_history)
            {
                foreach (var frame in _history)
                {
                    var resultframe = new Dictionary<Tuple<String, String>, StatisticsInfo.Document>();
                    foreach (var serviceInstance in frame)
                        resultframe.Add(new Tuple<String, String>(serviceInstance.Key.Item1, serviceInstance.Key.Item2),
                            serviceInstance.Value.Merge(new StatisticsInfo.Document()));
                    result.Add(resultframe);
                }
            }
            return result;
        }

        /// <summary>
        /// Creates a deepclone of the instance history
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<String, Dictionary<String, StatisticsInfo.Document>> GetInstanceHistory()
        {
            var result = new Dictionary<String, Dictionary<String, StatisticsInfo.Document>>();
            lock (_history)
            {
                foreach (var service in _instanceHistory)
                {
                    var resultservice = new Dictionary<String, StatisticsInfo.Document>();
                    foreach (var instance in service.Value)
                        resultservice.Add(instance.Key.Clone() as String,
                            instance.Value.Merge(new StatisticsInfo.Document()));
                    result.Add(service.Key.Clone() as String, resultservice);
                }
            }

            return result;
        }
    }
}
