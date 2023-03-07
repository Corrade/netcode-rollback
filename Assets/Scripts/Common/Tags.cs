using System;
using System.Collections;
using System.Collections.Generic;

namespace Lockstep
{
    static class Tags
    {
        public const ushort PlayerMetadata = 0;
        public const ushort Input = 1;
        public const ushort InputAck = 2;
        public const ushort Ping = 3;
        public const ushort PingAck = 4;

        public const ushort Count = 5;
    }
}
