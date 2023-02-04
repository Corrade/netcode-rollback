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

        // Recall that to simulate tick N, we need both tick N and its
        // predecessor. Therefore after completing all operations for tick N,
        // in setting up for tick N+1, we still require tick N.
        public virtual void DisposeInputs(ushort tickJustSimulated)
        {
            m_InputBuffer.StartInclusive = tickJustSimulated;
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
