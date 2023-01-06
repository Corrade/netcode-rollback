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
        protected ConnectionManager m_ConnectionManager;

        // m_InputHistory[tick] = input bitarray during that tick
        protected ushort[] m_InputHistory = new ushort[TickService.MaxTick];

        protected virtual void Awake()
        {
            for (int i = 0; i < TickService.MaxTick; i++)
            {
                m_InputHistory[i] = InputMasks.Invalid;
            }
        }

        public virtual void Initialise(ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }

        public bool HasInput(ushort tick)
        {
            return !InputMasks.IsInvalid(m_InputHistory[tick]);
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
            return !GetInput(TickService.SubtractTick(tick, 1), inputMask)
                && GetInput(tick, inputMask);
        }

        // " GetButtonUp/GetKeyUp
        public bool GetInputUp(ushort tick, ushort inputMask)
        {
            return GetInput(TickService.SubtractTick(tick, 1), inputMask)
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
    }
}
