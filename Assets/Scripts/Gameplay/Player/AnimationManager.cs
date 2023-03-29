using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Rollback;

namespace Rollback
{
    [RequireComponent(typeof(Animator), typeof(MovementManager), typeof(MetadataManager))]
    public class AnimationManager : MonoBehaviour
    {
        Animator m_Animator;
        MovementManager m_MovementManager;
        MetadataManager m_MetadataManager;

        bool m_State;
        bool m_RollbackState;

        void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_MovementManager = GetComponent<MovementManager>();
            m_MetadataManager = GetComponent<MetadataManager>();
        }

        void Start()
        {
            // Observer: On movement manager change, listen => better. will need to use this approach for OnLifeLost, might as well do it for this. state doesn't always happen just after simulate() is called

            /*
            m_State.IsFacingLeftChanged += OnIsFacingLeftChanged;
            m_State.IsKickingChanged += OnIsKickingChanged;

                PeerPlayer.LifeLost += OnLifeLost;
            hit anim

            jump => movement state change (candidate velocity)
            kick => movement state change (is kicking)
            land => movement state change (is grounded)
            move => movement state change (candidate velocity)
            idle
            */
        }

        public void SaveRollbackState()
        {
            ;
        }

        public void Rollback()
        {
            ;
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            ;
        }
    }
}
