using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ERAUtils;
using ERAUtils.Logger;
using System.Net;
using Lidgren.Network;
using ProjectERA.Protocols;

namespace ERAServer.Protocols.Server.Misc
{
    internal partial class Misc : Protocol
    {
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

        protected Action<NetIncomingMessage, MiscAction>[] functions = new Action<NetIncomingMessage, MiscAction>[255];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public Misc(Connection connection)
            : base(connection)
        {
            functions[(Int32)MiscAction.UserTransfer] = (NetIncomingMessage msg, MiscAction action) => UserTransfer(msg); // No sub actions for usertransfer
            functions[(Int32)MiscAction.MapBroadcast] = (NetIncomingMessage msg, MiscAction action) => MapBroadcast(msg);
        }

        /// <summary>
        /// 
        /// </summary>
        public override Byte ProtocolIdentifier
        {
            get { return (Byte)ServerProtocols.Misc; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        internal override void IncomingMessage(Lidgren.Network.NetIncomingMessage msg)
        {
            MiscAction action = (MiscAction)msg.ReadRangedInteger(0, (Int32)MiscAction.Max);
            if(functions[(Int32)action] == null)
                throw new NetException("No such action in this protocol " + this.GetType());

            msg.SkipPadBits();

            functions[(Int32)action](msg, action);
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingMessage(MiscAction action)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier);
            msg.WriteRangedInteger(0, (Int32)MiscAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }

        /// <summary>
        /// Creates an Outgoing Message with the specified action integer written to it.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private NetOutgoingMessage OutgoingMessage(MiscAction action, Int32 initialCapacity)
        {
            NetOutgoingMessage msg = Connection.MakeMessage(ProtocolIdentifier, initialCapacity + ERAUtils.BitManipulation.BytesToHold((Int32)MiscAction.Max));
            msg.WriteRangedInteger(0, (Int32)MiscAction.Max, (Int32)action);
            msg.WritePadBits();
            return msg;
        }
    }
}
