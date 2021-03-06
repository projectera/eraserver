﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Lidgren.Network;

namespace ERA.Protocols.ServiceProtocol
{
    public class StatisticsInfo : AbstractInfo
    {
        /// <summary>
        /// Creates a new StatisticsInfo object
        /// </summary>
        /// <param name="client">The client to use</param>
        public StatisticsInfo(ServiceClient client)
            : base("Statistics", client)
        {

        }

        /// <summary>
        /// Get all the timeslices
        /// </summary>
        /// <returns></returns>
        public List<Tuple<DateTime, List<Document>>> Get()
        {
            var res = Client.AskQuestion(CreateQuestion("Get"));
            return UnpackSlices(res.Packet);
        }

        /// <summary>
        /// Get all the slices between stime and etime
        /// </summary>
        /// <param name="stime">Start time</param>
        /// <param name="etime">End time</param>
        /// <returns></returns>
        public List<Tuple<DateTime, List<Document>>> GetSlice(DateTime stime, DateTime etime)
        {
            var m = CreateQuestion("GetSlice");
            m.Packet.Write(stime.ToUniversalTime().ToBinary());
            m.Packet.Write(etime.ToUniversalTime().ToBinary());
            var res = Client.AskQuestion(m);
            return UnpackSlices(res.Packet);
        }

        /// <summary>
        /// Get all the slices from stime to stime + span
        /// </summary>
        /// <param name="stime">Start time</param>
        /// <param name="span">Timespan to get</param>
        /// <returns></returns>
        public List<Tuple<DateTime, List<Document>>> GetSpan(DateTime stime, TimeSpan span)
        {
            return GetSlice(stime, stime + span);
        }

        /// <summary>
        /// Get all the slices from etime - span to etime
        /// </summary>
        /// <param name="span">Timespan to get</param>
        /// <param name="etime">End time</param>
        /// <returns></returns>
        public List<Tuple<DateTime, List<Document>>> GetSpan(TimeSpan span, DateTime etime)
        {
            return GetSlice(etime - span, etime);
        }

        /// <summary>
        /// Unpack timeslices from a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private List<Tuple<DateTime, List<Document>>> UnpackSlices(NetBuffer buffer)
        {
            var result = new List<Tuple<DateTime, List<Document>>>();
            var frames = buffer.ReadInt32();
            for (Int32 i = 0; i < frames; i++)
            {
                var timeslice = DateTime.FromBinary(buffer.ReadInt64());
                var sliceservices = buffer.ReadInt32();

                var timesliceres = new Tuple<DateTime, List<Document>>(timeslice, new List<Document>(sliceservices + 1));
                if (sliceservices == 0)
                    continue;

                //Console.WriteLine("On {0} there {1}.", timeslice.ToLongTimeString(), __n(sliceservices, "was {0} service", "where {0} services"));
                for (Int32 j = 0; j < sliceservices; j++)
                    timesliceres.Item2.Add(Document.Unpack(buffer));

                result.Add(timesliceres);
            }

            return result;
        }
        
        /// <summary>
        /// Get aggregated data
        /// </summary>
        /// <returns></returns>
        public Tuple<DateTime, List<Document>> GetTotal()
        {
            var res = Client.AskQuestion(CreateQuestion("GetTotal"));
            var buffer = res.Packet;

            var retrievalTime = DateTime.FromBinary(buffer.ReadInt64());
            var services = buffer.ReadInt32();
            var result = new Tuple<DateTime, List<Document>>(retrievalTime, new List<Document>(services + 1));

            for (Int32 j = 0; j < services; j++)
                result.Item2.Add(Document.Unpack(buffer));

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public class Document : BsonDocument
        {
            /// <summary>
            /// Id for the document
            /// </summary>
            [BsonId]
            public ObjectId Id;

            /// <summary>
            /// Time of creation
            /// </summary>
            public DateTime Time { get { return Id.CreationTime.ToUniversalTime(); } }

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
            public Document()
            {
                Id = ObjectId.GenerateNewId();
            }

            /// <summary>
            /// Merges this and doc in new document
            /// </summary>
            /// <param name="doc"></param>
            /// <returns>New merged doc</returns>
            public Document Merge(Document doc, Boolean leftId = true)
            {
                return new Document()
                {
                    Id = leftId ? this.Id : doc.Id,
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
            public Document Difference(Document doc, Boolean leftId = true)
            {
                return new Document()
                {
                    Id = leftId ? this.Id : doc.Id,
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
            public void Pack(NetBuffer buffer)
            {
                buffer.Write(Id.ToByteArray());
                buffer.Write(Name);
                buffer.Write(ReceivedBytes);
                buffer.Write(ReceivedPackets);
                buffer.Write(SentBytes);
                buffer.Write(SentPackets);
                buffer.Write(ResentMessages);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="netBuffer"></param>
            public static Document Unpack(NetBuffer buffer)
            {
                return new Document()
                {
                    Id = new ObjectId(buffer.ReadBytes(12)),
                    Name = buffer.ReadString(),
                    ReceivedBytes = buffer.ReadInt32(),
                    ReceivedPackets = buffer.ReadInt32(),
                    SentBytes = buffer.ReadInt32(),
                    SentPackets = buffer.ReadInt32(),
                    ResentMessages = buffer.ReadInt32(),
                };
            }
        }
    }
}
