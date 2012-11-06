using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using Lidgren.Network;
using System.Threading;
using MongoDB.Bson;

namespace NetworkStatistics
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = ServiceClient.Connect("NetworkStatistics");

            var statisticsinfo = new StatisticsInfo(client);
            try
            {
                var stats = statisticsinfo.Get();
                var stime = DateTime.MinValue;
                
                foreach(var timeslice in stats)
                {
                    if (timeslice.Item2.Count == 0)
                        continue;

                    Console.WriteLine("On {0} there {1}.", timeslice.Item1.ToLongTimeString(), __n(timeslice.Item2.Count, "was {0} service", "where {0} services"));
                    foreach (var service in timeslice.Item2) 
                    {
                        if (service.ReceivedPackets == 0 && service.SentPackets == 0)
                        {
                            Console.WriteLine(" - {0}\n    - inactive", service.Name);
                            continue;
                        }
                        Console.WriteLine(" - {0}\n    - recv {1:###0} in {2:###0} packets\n    - sent {3:###0} in {5:###0}{4} packets",
                            service.Name, 
                            ReadableBytes(service.ReceivedBytes), 
                            service.ReceivedPackets, 
                            ReadableBytes(service.SentBytes), 
                            service.ResentMessages == 0 ? "" : 
                                String.Format("(+{0:###0})", service.ResentMessages),
                            service.SentPackets - service.ResentMessages);
                    }
                    stime = stime > timeslice.Item1 ? stime : timeslice.Item1;
                }

                Console.WriteLine("\n");

                var total = statisticsinfo.GetTotal();
                Console.WriteLine("Total stats retrieval timestamp {0} {1}.", total.Item1.ToLongTimeString(), __n(total.Item2.Count, "saw {0} service", "seen {0} services"));
                foreach (var service in total.Item2)
                    Console.WriteLine(" - {0}\n    - recv {1:###0} in {2:###0} packets\n    - sent {3:###0} in {5:###0}{4} packets",
                        service.Name,
                        ReadableBytes(service.ReceivedBytes),
                        service.ReceivedPackets,
                        ReadableBytes(service.SentBytes),
                        service.ResentMessages == 0 ? "" :
                            String.Format("(+{0:###0})", service.ResentMessages),
                        service.SentPackets - service.ResentMessages);

                Console.WriteLine("\n");

                token = new CancellationTokenSource();
                Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

                while(!token.IsCancellationRequested) {
                    System.Threading.Thread.Sleep(1000);

                    var etime = DateTime.Now.ToUniversalTime();
                    var slicestats = statisticsinfo.GetSlice(stime, etime);

                    foreach (var timeslice in slicestats)
                    {
                        if (timeslice.Item2.Count == 0)
                            continue;

                        // TODO: Still some error in send protocol
                        if (timeslice.Item1 < stime || timeslice.Item1 > etime)
                            continue;

                        Console.WriteLine("On {0} there {1}.", timeslice.Item1.ToLongTimeString(), __n(timeslice.Item2.Count, "was {0} service", "where {0} services"));
                        foreach (var service in timeslice.Item2)
                        {
                            if (service.ReceivedPackets == 0 && service.SentPackets == 0)
                            {
                                Console.WriteLine(" - {0}\n    - inactive", service.Name);
                                continue;
                            }
                            Console.WriteLine(" - {0}\n    - recv {1:###0} in {2:###0} packets\n    - sent {3:###0} in {5:###0}{4} packets",
                                service.Name,
                                ReadableBytes(service.ReceivedBytes),
                                service.ReceivedPackets,
                                ReadableBytes(service.SentBytes),
                                service.ResentMessages == 0 ? "" :
                                    String.Format("(+{0:###0})", service.ResentMessages),
                                service.SentPackets - service.ResentMessages);
                        }
                        stime = stime > timeslice.Item1 ? stime : timeslice.Item1;
                    }
                }
                
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Statistics not available.");
            }

            // TODO: graphs, output, bytes/second etc

            client.Stop();
            Console.WriteLine("Goodbye");
            Console.ReadKey();
        }

        static CancellationTokenSource token;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            token.Cancel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        static String ReadableBytes(Double bytes)
        {
            String[] fix = new String[] { __n((Int32)bytes, "Byte", "Bytes"), "KiB", "MiB", "GiB", "TiB" };
            Int32 i = 0;
            while (bytes > 1024 && fix.Length > ++i)
                bytes /= 1024;

            return String.Format("{0} {1}", Math.Round(bytes, 2), fix[i]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <param name="single"></param>
        /// <param name="multiple"></param>
        /// <returns></returns>
        static String __n(Int64 count, String single, String multiple)
        {
            return String.Format(count == 1 ? single : multiple, count);
        }
    }
}
