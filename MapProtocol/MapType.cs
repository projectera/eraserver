using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERA.Protocols.MapProtocol
{
    /// <summary>
    /// The MapType Enumeration can be used: monsters respond and defend against
    /// enemies, as well as by the global wheather system, which uses region id + maptype to
    /// determine the current wheather.
    /// </summary>
    public enum MapType : byte
    {
        /// <summary>
        /// 0: Not Specified.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// 1: Plains, usually pretty open fields and area's with quite some
        /// habitation. Can be crawled over by enemies and filled with quests.
        /// </summary>
        Plains = 1,

        /// <summary>
        /// 2: Sunny Forest, a forest with not so dense tree and a lot of light.
        /// </summary>
        SunnyForest = 2,

        /// <summary>
        /// 3: Dark forest. Dark forest have larger trees then sunny forests
        /// and are more dense all together. There is less light because of this.
        /// </summary>
        DarkForest = 3,

        /// <summary>
        /// 4: Rainforest
        /// </summary>
        RainForest = 4,

        /// <summary>
        /// 10: Village
        /// </summary>
        Village = 10,

        /// <summary>
        /// 11: Town
        /// </summary>
        Town = 11,

        /// <summary>
        /// 12: City
        /// </summary>
        City = 12,

        /// <summary>
        /// 13: Capital
        /// </summary>
        Capital = 13,

        /// <summary>
        /// 20: Dungeon
        /// </summary>
        Dungeon = 20,

        /// <summary>
        /// 21: Cave
        /// </summary>
        Cave = 21,

        /// <summary>
        /// 22: Mine
        /// </summary>
        Mine = 22,

        /// <summary>
        /// 30: Beach
        /// </summary>
        Beach = 30,

        /// <summary>
        /// 40: Mountain Top
        /// </summary>
        MountainTop = 40,

        /// <summary>
        /// 41: Mountain Path
        /// </summary>
        MountainPath = 41,

        /// <summary>
        /// 42: Mountain Village
        /// </summary>
        MountainVillage = 42,

        /// <summary>
        /// 43: Hills
        /// </summary>
        Hills = 43,

        /// <summary>
        /// 44: Lake
        /// </summary>
        Lake = 44,

        /// <summary>
        /// 45: Fort
        /// </summary>
        Fort = 45,

        /// <summary>
        /// 55: Religious -> Temples, etc.
        /// </summary>
        Religious = 55,

        /// <summary>
        /// 56: Trade -> Markets, Trade Routes etc.
        /// </summary>
        Trade = 56,
    }
}
