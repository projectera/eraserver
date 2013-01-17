using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ERA.Protocols.InteractableProtocol
{
    public enum EquipmentPart : byte
    {
        None = 0,

        Left = 1,
        Right = 2,
        Over = 3,
        Top = 4,
        Bottom = 5,
        Head = 6,
        Hands = 7,
        Feet = 8,
        Neck = 9,
        ArmLeft = 10,
        ArmRight = 11,
        Extra = 12,
        KeyItem = 13
    }
}
