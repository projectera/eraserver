using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Services;
using ServiceProtocol;
using Lidgren.Network;

namespace EraS.MessageHandlers.ErasComponents
{
    public class StatisticsComponent : DefaultComponent
    {
        /// <summary>
        /// 
        /// </summary>
        public StatisticsComponent() : base("Statistics")
        {
            Functions.Add("Get", Get);
            Functions.Add("GetTotal", GetTotal);
        }

        /// <summary>
        /// Answers with all the frames
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void Get(MessageClient c, Message m)
        {
            var answer = m.Answer(c);
            var results = new Stack<Dictionary<String, StatisticsInfo.Document>>();
            var history = StatisticsService.GetHistory();

            // In reverse order, call the history. If a service has no delta
            // it means it was terminated that frame. Store it as delta. Eacsh
            // frame save the difference between delta and frame value. This is
            // what was actually sent in that frame. Store the frame as the new
            // delta. If there is no frame, but there is a delta, then this frame
            // is the frame before the service started. Save it as a whole.
            //C:\Users\Derk-Jan\Documents\Visual Studio 2010\Projects\Project ERA Server\EraS\MessageHandlers\ErasComponents\StatisticsComponent.cs
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

            var deltas = new Dictionary<String, StatisticsInfo.Document>();
            for (Int32 i = history.Count - 1; i >= 0; i--)
            {
                var frame = history[i];
                var aggregatedServices = new Dictionary<String, StatisticsInfo.Document>();

                // Merge the instances to stats per service
                foreach (var serviceInstance in frame)
                {
                    var servicename = serviceInstance.Value.Name;
                    StatisticsInfo.Document aggregateStats;
                    if (!aggregatedServices.TryGetValue(servicename, out aggregateStats))
                        aggregatedServices.Add(servicename, new StatisticsInfo.Document());
                    aggregatedServices[servicename] = serviceInstance.Value.Merge(aggregatedServices[servicename]);
                }

                // Copy the old deltas
                Dictionary<String, StatisticsInfo.Document> fixeddeltas = new Dictionary<string, StatisticsInfo.Document>();
                foreach (var delta in deltas)
                    fixeddeltas.Add(delta.Key, delta.Value);

                // If there is no delta doc, add this doc as delta
                var result = new Dictionary<String, StatisticsInfo.Document>();
                foreach (var aggregate in aggregatedServices)
                {
                    StatisticsInfo.Document deltadoc;
                    if (!deltas.TryGetValue(aggregate.Key, out deltadoc))
                        deltas.Add(aggregate.Key, aggregate.Value);
                }

                // For the the old deltas, if the current aggregation doesn't
                // have the service, last frame was the first frame the service
                // was up. So add that frame as a result and remove it from deltas
                foreach (var delta in fixeddeltas)
                {
                    StatisticsInfo.Document aggredoc;
                    if (!aggregatedServices.TryGetValue(delta.Key, out aggredoc))
                    {
                        result.Add(delta.Key, delta.Value);
                        deltas.Remove(delta.Key);
                        continue;
                    }

                    // Difference since last frame
                    deltas[delta.Key] = aggredoc;

                    var diff = delta.Value.Difference(aggredoc);
                    //if (diff.SentPackets == 0 && diff.ReceivedPackets == 0)
                    //    continue;
                    result.Add(delta.Key, diff);
                }

                if (result.Count > 0)
                    results.Push(result);
            }

            // Add the delta's left
            var finalresult = new Dictionary<String, StatisticsInfo.Document>();
            foreach (var delta in deltas)
                finalresult.Add(delta.Key, delta.Value);

            // Push on the stack
            results.Push(finalresult);

            answer.Packet.Write(results.Count);
            while (results.Count > 0)
                WriteFrame(results.Pop(), answer.Packet);

            c.SendMessage(answer);
        }

        /// <summary>
        /// Get accumulated data
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        private static void GetTotal(MessageClient c, Message m)
        {
            var answer = m.Answer(c);
            var instanceHistory = StatisticsService.GetInstanceHistory();

            // Services
            answer.Packet.Write(DateTime.Now.ToUniversalTime().ToBinary());
            answer.Packet.Write(instanceHistory.Count);
            foreach (var service in instanceHistory)
            {
                var frame = new StatisticsInfo.Document();
                foreach (var instance in service.Value)
                    frame = instance.Value.Merge(frame);
                frame.Pack(answer.Packet);
            }
            
            c.SendMessage(answer);
        }


        /// <summary>
        /// Writes a single statistics frame to a netbuffer
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="buffer"></param>
        private static void WriteFrame(Dictionary<String, StatisticsInfo.Document> frame, NetBuffer buffer)
        {
            if (frame.Count == 0)
            {
                buffer.Write(new StatisticsInfo.Document().Time.ToBinary());
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
    }
}
