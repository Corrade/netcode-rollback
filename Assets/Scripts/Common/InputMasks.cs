using System;
using System.Collections;
using System.Collections.Generic;

namespace Lockstep
{
    static class InputMasks
    {
        public const ushort MoveLeft    = 0b0000_0000_0000_0001;
        public const ushort MoveRight   = 0b0000_0000_0000_0010;
        public const ushort Dive        = 0b0000_0000_0000_0100;
        public const ushort Kick        = 0b0000_0000_0000_1000;
        public const ushort Invalid     = 0b1111_1111_1111_1111;

        public static readonly List<ushort> AllMasks = new List<ushort>{  MoveLeft, MoveRight, Dive, Kick };
        public const ushort Count = 4;

        public static bool IsInvalid(ushort inputMask)
        {
            return inputMask == Invalid;
        }
    }
}
