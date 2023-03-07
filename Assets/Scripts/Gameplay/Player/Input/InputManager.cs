using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DarkRift.Server;
using DarkRift.Server.Unity;

using Lockstep;

namespace Lockstep
{
    public abstract class InputManager : MonoBehaviour
    {
        public ushort StartInclusive => m_InputBuffer.StartInclusive;
        public ushort EndExclusive => m_InputBuffer.EndExclusive;

        protected InputBuffer m_InputBuffer = new InputBuffer();

        public virtual void Initialise() {}

        public virtual void ResetForRound(ushort startTick)
        {
            m_InputBuffer.Initialise(
                // Start with blank input in the buffer to enable immediate simulation
                startInclusive: TickService.Subtract(startTick, 5),
                endExclusive: startTick
            );

            for (ushort t = m_InputBuffer.StartInclusive; TickService.IsBefore(t, m_InputBuffer.EndExclusive); t = TickService.Add(t, 1))
            {
                m_InputBuffer.WriteInput(t, 0);
            }
        }

        // Recall that to simulate tick N, we need both tick N and its
        // predecessor. Therefore after completing all operations for tick N,
        // we still require tick N for tick N+1.
        public virtual void DisposeInputs(ushort untilTickExclusive)
        {
            m_InputBuffer.StartInclusive = TickService.Min(untilTickExclusive, m_InputBuffer.EndExclusive);
        }

        public bool HasInput(ushort tick)
        {
            return m_InputBuffer.HasInput(tick);
        }

        public bool GetInput(ushort tick, ushort inputMask)
        {
            return m_InputBuffer.GetInput(tick, inputMask);
        }

        public bool GetInputDown(ushort tick, ushort inputMask)
        {
            return m_InputBuffer.GetInputDown(tick, inputMask);
        }

        public bool GetInputUp(ushort tick, ushort inputMask)
        {
            return m_InputBuffer.GetInputUp(tick, inputMask);
        }

        public float GetMoveInput(ushort tick)
        {
            return m_InputBuffer.GetMoveInput(tick);
        }
    }
}
