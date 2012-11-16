using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERA.Protocols.InteractableProtocol
{
    public enum BodyPart : byte
    {
        Skin = 0,
        Eyes = (1 << 0),
        Hair = (1 << 1),
        Asset = (1 << 2),
        Tile = (1 << 3),
        Weapon = (1 << 4),
        Armor = (1 << 5),
        Accessoiry = (1 << 6),
        KeyItem = (1 << 7),

        /// <summary>
        /// Holds the bits of parts that use a byte
        /// </summary>
        Byte = Skin,

        /// <summary>
        /// Holds the bits of parts that use a byte array
        /// </summary>
        ByteArr = Eyes | Hair,

        /// <summary>
        /// Holds the bits of parts that use an integer
        /// </summary>
        Integer = Tile | Weapon | Armor | Accessoiry | KeyItem,

        /// <summary>
        /// Holds the bits of parts that use a string
        /// </summary>
        String = Asset,
    }
}