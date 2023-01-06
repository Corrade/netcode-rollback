using UnityEngine;

using Lockstep;

namespace Lockstep
{
    public static class TickService
    {
        public const ushort Tickrate = 60;
        public const float TimeBetweenTicksSec = 1f / Tickrate;
        public const ushort MaxTick = 65530; // A bit less than the actual maximum of a ushort so that incrementing won't overflow
        public static readonly ushort LargeTickThreshold = (ushort)(MaxTick-TicksInSeconds(30));
        public static readonly ushort SmallTickThreshold = TicksInSeconds(30);

        public static ushort AddTick(ushort tick, int x)
        {
            return (ushort)MathExtensions.Mod((int)tick + x, MaxTick);
        }

        public static ushort SubtractTick(ushort tick, int x)
        {
            return AddTick(tick, -x);
        }

        public static bool IsTickBefore(ushort tick1, ushort tick2)
        {
            // If tick1 is large and tick2 is small, then assume tick2 has
            // wrapped around and hence tick2 > tick1 => true

            // Similarly, if tick1 is small and tick2 is large, then assume
            // tick1 has wrapped around so tick1 > tick2 => false

            return IsTickLarge(tick1) && IsTickSmall(tick2)
                ? true
                : IsTickSmall(tick1) && IsTickLarge(tick2)
                    ? false
                    : tick1 < tick2;
        }

        public static bool IsTickAfter(ushort tick1, ushort tick2)
        {
            return !IsTickBefore(tick1, tick2);
        }

        public static ushort TicksInSeconds(ushort seconds)
        {
            int res = Tickrate * seconds;
            if (res > MaxTick)
            {
                Debug.LogError("Overflow in TicksInSeconds()");
            }

            return (ushort)res;
        }

        public static bool IsTickLarge(ushort tick)
        {
            return tick > LargeTickThreshold;
        }

        public static bool IsTickSmall(ushort tick)
        {
            return tick < SmallTickThreshold;
        }
    }
}
