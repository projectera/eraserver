using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InteractableProtocol
{
    public enum InteractableComponentType : byte
    {
        None = 0,

        Appearance = 1,
        Movement = 2,


        Max = 7,
    }
}
