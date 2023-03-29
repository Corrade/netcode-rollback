using System;
using System.Collections;
using System.Collections.Generic;

namespace Rollback
{
    static class InputMasks
    {
        public const ushort MoveLeft    = 0b0000_0000_0000_0001;
        public const ushort MoveRight   = 0b0000_0000_0000_0010;
        public const ushort Dive        = 0b0000_0000_0000_0100;
        public const ushort Kick        = 0b0000_0000_0000_1000;

        public static readonly List<ushort> AllMasks = new List<ushort>{  MoveLeft, MoveRight, Dive, Kick };
        public const ushort Count = 4;
    }
}
