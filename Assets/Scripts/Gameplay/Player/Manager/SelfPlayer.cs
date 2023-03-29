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

using Rollback;

namespace Rollback
{
    [RequireComponent(typeof(SelfInputManager))]
    public class SelfPlayer : Player
    {
        protected override InputManager m_InputManager { get { return m_SelfInputManager; } }

        SelfInputManager m_SelfInputManager;

        protected override void Awake()
        {
            base.Awake();
            m_SelfInputManager = GetComponent<SelfInputManager>();
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
