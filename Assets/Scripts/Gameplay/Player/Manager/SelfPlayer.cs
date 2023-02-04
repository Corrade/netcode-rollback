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
    [RequireComponent(typeof(SelfInputManager), typeof(SelfPredictionManager))]
    public class SelfPlayer : Player
    {
        public bool IsPredicting
        {
            get { return m_SelfPredictionManager.IsPredicting; }
            set { m_SelfPredictionManager.IsPredicting = value; }
        }

        protected override InputManager m_InputManager { get { return m_SelfInputManager; } }

        SelfInputManager m_SelfInputManager;
        SelfPredictionManager m_SelfPredictionManager;

        protected override void Awake()
        {
            base.Awake();
            m_SelfInputManager = GetComponent<SelfInputManager>();
            m_SelfPredictionManager = GetComponent<SelfPredictionManager>();
        }

        public void WriteInput(ushort currentTick)
        {
            m_SelfInputManager.WriteInput(currentTick);
        }

        public void SendUnackedInputs(ushort untilTickExclusive)
        {
            m_SelfInputManager.SendUnackedInputs(untilTickExclusive);
        }
    }
}
