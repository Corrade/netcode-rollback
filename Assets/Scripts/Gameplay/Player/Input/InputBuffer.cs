using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Lockstep;

namespace Lockstep
{
    public class InputBuffer
    {
        public ushort StartInclusive
        {
            get { return m_StartInclusive; }

            set
            {
                m_StartInclusive = value;
                AssertStartIsBeforeOrEqualToEnd();
            }
        }

        public ushort EndExclusive
        {
            get { return m_EndExclusive; }

            set
            {
                m_EndExclusive = value;

                // Assert that the end is not close to wrapping around to be before the start
                Assert.IsFalse(TickService.IsBefore(TickService.Add(EndExclusive, 100), StartInclusive));

                AssertStartIsBeforeOrEqualToEnd();
            }
        }

        // m_InputHistory[tick] = input bitarray during that tick
        ushort[] m_InputHistory = new ushort[TickService.MaxTick];
        ushort m_StartInclusive;
        ushort m_EndExclusive;

        public void Initialise(ushort startInclusive, ushort endExclusive)
        {
            StartInclusive = startInclusive;
            EndExclusive = endExclusive;
        }

        public bool HasInput(ushort tick)
        {
            return TickService.IsAfterOrEqual(tick, StartInclusive) && TickService.IsBefore(tick, EndExclusive);
        }

        public void WriteInput(ushort tick, ushort input)
        {
            m_InputHistory[tick] = input;
        }

        public ushort GetRawInput(ushort tick)
        {
            Assert.IsTrue(HasInput(tick));
            return m_InputHistory[tick];
        }

        // Same as GetButton/GetKey from Unity's old Input system,
        // except tick-based instead of frame-based
        public bool GetInput(ushort tick, ushort inputMask)
        {
            Assert.IsTrue(HasInput(tick));
            return (m_InputHistory[tick] & inputMask) != 0;
        }

        // " GetButtonDown/GetKeyDown
        public bool GetInputDown(ushort tick, ushort inputMask)
        {
            return !GetInput(TickService.Subtract(tick, 1), inputMask)
                && GetInput(tick, inputMask);
        }

        // " GetButtonUp/GetKeyUp
        public bool GetInputUp(ushort tick, ushort inputMask)
        {
            return GetInput(TickService.Subtract(tick, 1), inputMask)
                && !GetInput(tick, inputMask);
        }

        public float GetMoveInput(ushort tick)
        {
            float res = 0f;

            if (GetInput(tick, InputMasks.MoveLeft))
            {
                res -= 1f;
            }

            if (GetInput(tick, InputMasks.MoveRight))
            {
                res += 1f;
            }

            return res;
        }

        void AssertStartIsBeforeOrEqualToEnd()
        {
            Assert.IsTrue(TickService.IsBeforeOrEqual(StartInclusive, EndExclusive));
        }
    }
}
