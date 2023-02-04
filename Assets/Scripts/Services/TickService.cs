using UnityEngine;

using Lockstep;

namespace Lockstep
{
    public static class TickService
    {
        public const ushort Tickrate = 60;
        public const float TimeBetweenTicksSec = 1f / Tickrate;
        public const ushort StartTick = 0;
        public const ushort MaxTick = 65530; // A bit less than the actual maximum of a ushort so that incrementing won't overflow

        static readonly ushort LargeTickThreshold = (ushort)(MaxTick-SecondsToTicks(100));
        static readonly ushort SmallTickThreshold = SecondsToTicks(100);

        public static ushort Add(ushort tick, int x)
        {
            return (ushort)MathExtensions.Mod((int)tick + x, MaxTick);
        }

        public static ushort Subtract(ushort tick, int x)
        {
            return Add(tick, -x);
        }

        public static bool IsBefore(ushort tick1, ushort tick2)
        {
            // If tick1 is large and tick2 is small, then assume tick2 has
            // wrapped around and hence tick2 > tick1 => true

            // Similarly, if tick1 is small and tick2 is large, then assume
            // tick1 has wrapped around so tick1 > tick2 => false

            return IsLarge(tick1) && IsSmall(tick2)
                ? true
                : IsSmall(tick1) && IsLarge(tick2)
                    ? false
                    : tick1 < tick2;
        }

        public static bool IsBeforeOrEqual(ushort tick1, ushort tick2)
        {
            return IsBefore(tick1, tick2) || (tick1 == tick2);
        }

        public static bool IsAfter(ushort tick1, ushort tick2)
        {
            return !IsBeforeOrEqual(tick1, tick2);
        }

        public static bool IsAfterOrEqual(ushort tick1, ushort tick2)
        {
            return !IsBefore(tick1, tick2);
        }

        public static ushort Min(ushort tick1, ushort tick2)
        {
            return IsBefore(tick1, tick2) ? tick1 : tick2;
        }

        public static ushort Max(ushort tick1, ushort tick2)
        {
            return IsAfter(tick1, tick2) ? tick1 : tick2;
        }

        public static ushort SecondsToTicks(ushort seconds)
        {
            int res = Tickrate * seconds;
            if (res > MaxTick)
            {
                Debug.LogError("Overflow in SecondsToTicks()");
            }

            return (ushort)res;
        }

        static bool IsLarge(ushort tick)
        {
            return tick > LargeTickThreshold;
        }

        static bool IsSmall(ushort tick)
        {
            return tick < SmallTickThreshold;
        }
    }
}
