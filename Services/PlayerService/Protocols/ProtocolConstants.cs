using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERA.Services.PlayerService
{
    /// <summary>
    /// 
    /// </summary>
    public static class ProtocolConstants
    {
        public const Byte ExtensionByte = 0;

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
