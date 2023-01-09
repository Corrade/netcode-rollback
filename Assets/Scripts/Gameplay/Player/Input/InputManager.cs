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
        protected InputBuffer m_InputBuffer = new InputBuffer();

        public virtual void Initialise() {}

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
