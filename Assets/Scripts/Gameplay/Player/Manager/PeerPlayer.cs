using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Lockstep;

namespace Lockstep
{
    [RequireComponent(typeof(PeerInputManager))]
    public class PeerPlayer : Player
    {
        protected override InputManager m_InputManager { get { return m_PeerInputManager; } }

        PeerInputManager m_PeerInputManager;

        protected override void Awake()
        {
            base.Awake();
            m_PeerInputManager = GetComponent<PeerInputManager>();
        }

        public void SimulateWithExtrapolation()
        {
            // Extrapolate by taking the tick with the most recent input
            ushort tick = TickService.Subtract(m_PeerInputManager.EndExclusive, 1);

            // Prior ticks are needed for GetInputDown() and GetInputUp()
            Assert.IsTrue(m_PeerInputManager.HasInput(TickService.Subtract(tick, 1)));
            Assert.IsTrue(m_PeerInputManager.HasInput(tick));

            m_MovementManager.Simulate(tick);
        }
    }
}
