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

            m_MovementManager.CandidateVelocityChanged += OnCandidateVelocityChanged;
            m_MovementManager.IsGroundedChanged += OnIsGroundedChanged;
            m_MovementManager.IsKickingChanged += OnIsKickingChanged;

            //the rollback state should be in one class, accessed by both the animation manager and the movement manager.
            //store it in player
            //"RollbackState"

            m_MetadataManager.LifeLost += OnLifeLost;

            Assert.IsTrue(m_Animator.layerCount == 1);
        }

        public void Reset()
        {
            // reset
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
            AnimatorStateInfo m_CurrentStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);

            // Only one active clip at a time (no blending)
            Assert.IsTrue(m_CurrentClipInfo.Length == 1);
            AnimatorClipInfo m_CurrentClip = m_CurrentClipInfo[0];

            string name = m_CurrentClip.clip.name;
            float originalLength = m_CurrentClip.clip.length;
            float speedAdjustedLength = m_CurrentStateInfo.length;
            float t = m_CurrentStateInfo.normalizedTime;

            DebugUI.WriteSequenced(
                DebugGroup.Animation,
                $"{m_MetadataManager.Id} AnimationManager.SaveRollbackState()",
                $"id={m_MetadataManager.Id} AnimationManager.SaveRollbackState(): name={name}, originalLength={originalLength}, speedAdjustedLength={speedAdjustedLength}, t={t}"
            );
        }

        public void Rollback()
        {
            /*
            load saved data
            */

            // The delegates below will be called beforehand as the movement
            // manager is rolled back first.
            ;
        }

        void OnCandidateVelocityChanged(Vector2 candidateVelocity)
        {
            m_Animator.SetBool("IsJumping", candidateVelocity.y > 0);
            // say in this state for n ms UNLESS we're rolling back or kicking, in which case transition back immediately

            m_Animator.SetFloat("Velocity", candidateVelocity.magnitude);
        }

        void OnIsGroundedChanged(bool isGrounded)
        {
            m_Animator.SetTrigger("Landed");
            // say in this state for n ms UNLESS we're rolling back, in which case transition back immediately
        }

        void OnIsKickingChanged(bool isKicking)
        {
            m_Animator.SetBool("IsKicking", isKicking);
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            m_Animator.SetTrigger("GotHit");
            // => hit anim until reset
        }
    }
}
