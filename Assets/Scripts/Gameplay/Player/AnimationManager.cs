using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Rollback;

/*
https://answers.unity.com/questions/787508/how-do-i-determine-the-priority-in-which-animation.html

Transitions are resolved in descending order of inspector vertical position.
*/

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

            // gameManager.RoundStarted += OnRoundStarted;

            // TODO
            /*
            m_MovementManager.CandidateVelocityChanged += OnCandidateVelocityChanged;
            m_MovementManager.IsGroundedChanged += OnIsGroundedChanged;
            m_MovementManager.IsKickingChanged += OnIsKickingChanged;
            */

            m_MetadataManager.LifeLost += OnLifeLost;
        }

        public void SaveRollbackState()
        {
            /*
            save the animation and the time remaining on it
            */
            ;
        }

        public void Rollback()
        {
            /*
            load saved data
            but note that the delegates below will also be called...
            */
            ;
        }

        void OnRoundStarted()
        {
            // reset
        }

        void OnCandidateVelocityChanged()
        {
            // velocity.y => jump => movement state change (candidate velocity)
            // velocity.x => move => movement state change (candidate velocity)
        }

        void OnIsGroundedChanged()
        {
            // land => movement state change (is grounded)
        }

        void OnIsKickingChanged()
        {
            // kick => movement state change (is kicking)
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            // => hit anim until round started
        }
    }
}
