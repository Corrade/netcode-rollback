using System;
using System.Collections;
using System.Collections.Generic;

namespace Lockstep
{
    static class Tags
    {
        public const ushort SetupComplete = 0;
        public const ushort PlayerMetadata = 1;
        public const ushort Input = 2;
        public const ushort InputAck = 3;

        public const ushort Count = 4;
    }
}
