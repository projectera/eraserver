using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using Lidgren.Network;

namespace NetworkStatistics
{
    class Program
    {
        static void Main(string[] args)
        {
            var manual = false;

            var client = ServiceClient.Connect("NetworkStatistics");
            NetworkInfo n = new NetworkInfo(client);

            try
            {
                var message = client.CreateQuestion(MessageType.EraS, "Self");
                message.Packet.Write("GetStatisticsVersion");
                var answer = client.AskReliableQuestion(message);
                var version = answer.Packet.ReadString();
                Console.WriteLine("Statistics version {0}.", version);
                // branch by version

                message = client.CreateQuestion(MessageType.EraS, "Self");
                message.Packet.Write("GetStatistics");
                answer = client.AskReliableQuestion(message);
                var buffer = answer.Packet;

                var frames = buffer.ReadInt32();
                for (Int32 i = 0; i < frames; i++)
                {
                    var timeslice = DateTime.FromBinary(buffer.ReadInt64());
                    var sliceservices = buffer.ReadInt32();
                    Console.WriteLine("On {0} there {1}.", timeslice.ToLongTimeString(), __n(sliceservices, "was {0} service", "where {0} services"));
                    for (Int32 j = 0; j < sliceservices; j++)
                    {
                        var id = buffer.ReadBytes(12);
                        var name = buffer.ReadString();
                        var rbytes = buffer.ReadInt32();
                        var rpackets = buffer.ReadInt32();
                        var sbytes = buffer.ReadInt32();
                        var spackets = buffer.ReadInt32();
                        var rsmsgs = buffer.ReadInt32();
                        Console.WriteLine("{0}\t read {1:###0} in {2:###0} packets   send {3:###0} in {4:###0} packets",
                            name, ReadableBytes(rbytes), rpackets, ReadableBytes(sbytes), spackets);
                    }

                    if (manual && i + 1 < frames)
                    {
                        Console.WriteLine("There are {0} frames left.", frames - 1 - i);
                        Console.ReadKey();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        static String ReadableBytes(Double bytes)
        {
            String[] fix = new String[] { "B", "KiB", "MiB", "GiB", "TiB" };
            Int32 i = 0;
            while (bytes > 1024 && fix.Length > ++i)
                bytes /= 1024;

            return String.Format("{0}{1}", Math.Round(bytes, 2), fix[i]);
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
