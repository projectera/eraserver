﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceProtocol
{
    public enum ControlType : byte
    {
        Kill = 1,

        Reserved = 255,
    }
}
