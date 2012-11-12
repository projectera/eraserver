using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerService.Protocols.Server
{

    public enum ServerProtocols : byte
    {
        Extension = 0,

        /// <summary>
        /// 
        /// </summary>
        PeerExchange = 1,

        /// <summary>
        /// Does Network management like exchanging peers and possibly routing
        /// </summary>
        NetworkManagement = 3,

        /// <summary>
        /// Miscellaneous actions that don't need a full network protocol
        /// </summary>
        Misc = 255,

        /// <summary>
        /// 
        /// </summary>
        Max = ProtocolConstants.NetworkMaxValue
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ProtocolConstants
    {
        public const Int32 NetworkMaxValue = 255;       // byte

        public const Int32 UpdateMaxValue = 7;          // 3 bit
        public const Int32 ModerationMaxValue = 31;     // 5 bit
        public const Int32 PlayerMaxValue = 15;         // 4 bit
        public const Int32 CharacterMaxValue = 31;      // 5 bit

        public const Int32 BattleMaxValue = 254;
        public const Int32 TeamMaxValue = 254;
        public const Int32 GuildMaxValue = 254;
        public const Int32 FactionMaxValue = 254;
        public const Int32 ChatMaxValue = 254;
        public const Int32 TradeMaxValue = 254;
        public const Int32 ShopMaxValue = 254;

        public const Int32 MapMaxValue = 31;            // 5 bit

        public const Int32 DataIntegrityMaxValue = 7;   // 3 bit
    }
}
