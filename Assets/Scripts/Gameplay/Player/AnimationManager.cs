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

Transitions are resolved in descending order of their height in the inspector.
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

            the rollback state should be in one class, accessed by both the animation manager and the movement manager.
            store it in player
            "RollbackState"
            */

            m_MetadataManager.LifeLost += OnLifeLost;

            Assert.IsTrue(m_Animator.layerCount == 1);
        }

        public void SaveRollbackState()
        {
            /*
            save the animation and the time remaining on it
            */

            /*
            //Fetch the current Animation clip information for the base layer

            AnimatorStateInfo info = m_Animator.GetCurrentAnimatorStateInfo(0);



            RunPresentation() from game loop so that this code only runs once per frame
            */

            AnimatorClipInfo[] m_CurrentClipInfo = m_Animator.GetCurrentAnimatorClipInfo(0);
            //Access the current length of the clip

            // Only one active clip at a time (no blending)
            Assert.IsTrue(m_CurrentClipInfo.Length == 1);

            float m_CurrentClipLength = m_CurrentClipInfo[0].clip.length;
            //Access the Animation clip name
            string m_ClipName = m_CurrentClipInfo[0].clip.name;

            AnimatorStateInfo m_CurrentStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            float len = m_CurrentStateInfo.length;
            float t = m_CurrentStateInfo.normalizedTime;
            /*
            name=Idle
            length is 0.16666 (animation length)
            len is 0.666 (state length = animation length * speed)
            t is the running time! nice
            */

            DebugUI.Write(DebugGroup.Animation, "h", $"! name={m_ClipName}, length={m_CurrentClipLength}, len={len}, t={t}");
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
