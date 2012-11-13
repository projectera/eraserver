using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectERA.Protocols;
using Lidgren.Network;
using MongoDB.Bson;
using ERAUtils;
using ERAServer.Data.AI;
using System.Threading;

namespace ERAServer.Protocols.Client
{
    internal partial class Guild : Protocol
    {

        private ReaderWriterLockSlim _guildDataLock;
        private Data.Guild _guildData;

        /// <summary>
        /// 
        /// </summary>
        public Data.Guild GuildData
        {
            get
            {
                try
                {
                    _guildDataLock.EnterReadLock();
                    return _guildData;
                }
                finally
                {
                    _guildDataLock.ExitReadLock();
                }

            }

            set
            {
                _guildDataLock.EnterWriteLock();
                _guildData = value;
                _guildDataLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ObjectId Id
        {
            get
            {
                if (this.GuildData != null)
                    return GuildData.Id;

                return ObjectId.Empty;
            }
        }

        /// <summary>
        /// Private static list of this protocols instances
        /// </summary>
        private static List<Protocol> _instances;

        /// <summary>
        /// Player Instances
        /// </summary>
        /// <remarks>Static</remarks>
        public override List<Protocol> Instances
        {
            get
            {
                return _instances;
            }
            set
            {
                _instances = value;
            }
        }

        /// <summary>
        /// The id of this protocol
        /// </summary>
        public override Byte ProtocolIdentifier
        {
            get { return (Byte)ClientProtocols.Guild; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        public Guild(Connection connection)
            : base(connection)
        {
            _guildDataLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        internal override void IncomingMessage(NetIncomingMessage msg)
        {
            GuildAction action = (GuildAction)msg.ReadRangedInteger(0, (Int32)GuildAction.Max);
            msg.SkipPadBits();
            switch (action)
            {
                case GuildAction.Get:
                    ObjectId searchGuildId = new ObjectId(msg.ReadBytes(12));
                    
                    // Calling Find, which calls, so you would want to Queue this action
                    QueueAction(() =>
                    {
                        // Find the player 
                        Data.Guild guild = Find(searchGuildId) ?? new Data.Guild();

                        // Get temp if nothing fetched
                        Data.Guild tempGuild = null;
                        if (guild == null)
                            tempGuild = new Data.Guild();

                        // Create the message and encode data
                        NetOutgoingMessage getMsg = OutgoingMessage(GuildAction.Get);
                        getMsg.Write(searchGuildId.ToByteArray());
                        Guild.Pack(guild, ref getMsg);

                        // Send the message
                        this.Connection.SendMessage(getMsg, NetDeliveryMethod.ReliableUnordered);

                        // Recycle temp if needed
                        if (tempGuild != null)
                            tempGuild.Clear();
                    });
                    break;
            }
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingMessage(GuildAction action)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier);
            msg.WriteRangedInteger(0, (Int32)GuildAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingMessage(GuildAction action, Int32 initialCapacity)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier, initialCapacity + ERAUtils.BitManipulation.BytesToHold((Int32)GuildAction.Max));
            msg.WriteRangedInteger(0, (Int32)GuildAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }

        /// <summary>
        /// Encoded a player object from to an outgoing message
        /// </summary>
        /// <remarks>Needs to be synchronized with Decode de on Receiver</remarks>
        /// <param name="guild">Player to encode</param>
        /// <param name="msg">msg to encode to</param>
        /// <returns>msg with encoded player</returns>
        private static NetOutgoingMessage Pack(Data.Guild guild, ref NetOutgoingMessage msg)
        {
            // Write guild
            msg.Write(guild.Id.ToByteArray());
            msg.Write(guild.Name);
            msg.Write(Guild.PackMember(guild.Founder, ref msg));
            msg.Write(guild.FoundedDate.ToBinary());

            // Write members
            guild.EnterReadLock();
            msg.Write(guild.Members.Count);
            foreach (InteractableGuildMember member in guild)
            {
                msg.Write(Guild.PackMember(member, ref msg));
            }
            guild.ExitReadLock();

            // POINTS
            return msg;
        }

        /// <summary>
        /// Packs a Guild member into a message
        /// </summary>
        /// <param name="member">Member to pack</param>
        /// <param name="msg">Message to pack to</param>
        /// <returns></returns>
        /// <remarks>Member takes 29 bytes</remarks>
        private static NetOutgoingMessage PackMember(Data.AI.InteractableGuildMember member, ref NetOutgoingMessage msg)
        {
            msg.Write(member.InteractableId.ToByteArray());
            msg.Write((Byte)member.Permissions);
            msg.Write(member.Rank);
            msg.Write(member.MemberSince.ToBinary());

            return msg;
        }
    }
}
