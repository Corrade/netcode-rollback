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
    }
}
