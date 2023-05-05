using System;
using System.Collections;
using System.Collections.Generic;

namespace Rollback
{
    public enum DebugGroup : ushort
    {
        Core = 0b0000_0000_0000_0001,
        Movement = 0b0000_0000_0000_0010,
        Networking = 0b0000_0000_0000_0100,
        Input = 0b0000_0000_0000_1000,
        Animation = 0b0000_0000_0001_0000,
        All = 0b1111_1111_1111_1111,
    }
}
