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
        [SerializeField]
        float MotionTimeScale = 4f;

        Animator m_Animator;
        MovementManager m_MovementManager;
        MetadataManager m_MetadataManager;
        Player m_Player;

        float m_MotionTime;

        bool m_IsJumping = false;
        float m_Velocity = 0.0f;
        bool m_IsKicking = false; 
        AnimationState m_RollbackState = new AnimationState();

        void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_MovementManager = GetComponent<MovementManager>();
            m_MetadataManager = GetComponent<MetadataManager>();
            m_Player = GetComponent<Player>();

            m_MovementManager.CandidateVelocityChanged += WithNoOpDuringRollback<Vector2>(OnCandidateVelocityChanged);
            m_MovementManager.IsGroundedChanged += WithNoOpDuringRollback<bool>(OnIsGroundedChanged);
            m_MovementManager.IsKickingChanged += WithNoOpDuringRollback<bool>(OnIsKickingChanged);
            m_MetadataManager.LifeLost += WithNoOpDuringRollback<MetadataManager>(OnLifeLost);

            Assert.IsTrue(m_Animator.layerCount == 1);

            SimulationManager.Instance.SimulationProgressed += OnSimulationProgressed;

            m_Animator.StartPlayback();
        }

        public void Reset()
        {
            m_MotionTime = 0.0f;
            m_Animator.Play("Idle", layer: 0, normalizedTime: 0.0f);
            m_RollbackState.Reset();
        }

        public void Simulate()
        {
            m_MotionTime += MotionTimeScale * TickService.TimeBetweenTicksSec;
        }

        public void SaveRollbackState()
        {
            // todo this may be removable
            m_RollbackState.LoadFrom(m_Animator);
            m_RollbackState.NormalizedTime = m_MotionTime;

            // TODO temporary - tesing whethe ror not this state needs to be saved and rolled back
            m_IsJumping = m_Animator.GetBool("IsJumping");
            m_Velocity = m_Animator.GetFloat("Velocity");
            m_IsKicking = m_Animator.GetBool("IsKicking");

            if (m_MetadataManager.Id == 0)
                Debug.Log("curr anim name=" + m_RollbackState.Name);

            DebugUI.WriteSequenced(
                DebugGroup.Animation,
                $"{m_MetadataManager.Id} AnimationManager.SaveRollbackState()",
                $"id={m_MetadataManager.Id} AnimationManager.SaveRollbackState(): Name={m_RollbackState.Name}, NormalizedTime={m_RollbackState.NormalizedTime}, m_MotionTime={m_MotionTime}"
            );
        }

        public void Rollback()
        {
            /*
            DESIGN THOUGHTS

            Current approach: directly switch to the stored animation via
            Play().

            Previous idea #1: switch to the stored animation via a dedicated
            trigger transition.
            - Requires one extra transition per state
            - Modifying the time of the stored animation afterwards isn't
              easy

            Previous idea #2: transition to a rollback state and
            rely on the normal transitions to reach the stored animation.
            - Cleaner animation graph than previous idea #1 since only one
              transition (from "all states" to the rollback state) is needed
            - More difficult to get correct. The normal transitions are
              designed for state to flow in a natural sequence whereas rollback
              requires "random access".
            - Still hard to modify the time of the stored animation afterwards
            */

            m_Animator.Play(m_RollbackState.Name, layer: 0, m_RollbackState.NormalizedTime);
            //m_Animator.Play(m_RollbackState.Name, layer: 0, 0.0f);
            m_MotionTime = m_RollbackState.NormalizedTime;

            m_Animator.SetBool("IsJumping", m_IsJumping);
            m_Animator.SetFloat("Velocity", m_Velocity);
            m_Animator.SetBool("IsKicking", m_IsKicking);

            if (m_MetadataManager.Id == 0)
            {
                Debug.Log("rolling back to " + m_RollbackState.Name + " but it says anim name=" + GetCurrentAnimationName());
            }
        }

        // TODO Returns a stale value for some reason, which fucks the entire logic
        // by making SaveRollbackState() save an old state
        public string GetCurrentAnimationName()
        {
            AnimatorClipInfo[] currentClipInfo = m_Animator.GetCurrentAnimatorClipInfo(0);
            Assert.IsTrue(currentClipInfo.Length == 1);
            AnimatorClipInfo currentClip = currentClipInfo[0];
            return currentClip.clip.name;
        }

        void OnSimulationProgressed(ushort untilTickExclusive)
        {
            // We defer updating the actual parameter tied to the animator
            // to avoid jittery visuals
            m_Animator.SetFloat("MotionTime", m_MotionTime);
        }

        Action<T> WithNoOpDuringRollback<T>(Action<T> handler)
        {
            // The handlers below (OnXyzChanged) are invoked during the
            // movement manager's rollback, but we prevent this to ensure it
            // doesn't interfere with our rollback logic

            return (T value) => {
                if (!m_Player.IsRollingBack)
                {
                    handler(value);
                }
            };
        }

        void OnCandidateVelocityChanged(Vector2 candidateVelocity)
        {
            if (candidateVelocity.y > 0.0f && !Mathf.Approximately(candidateVelocity.y, 0.0f))
            {
                //m_RollbackState.Name = "Jump";
            }
            else if (candidateVelocity.magnitude > 0.01f)
            {
                //m_RollbackState.Name = "Move";
            }
            m_Animator.SetBool("IsJumping", candidateVelocity.y > 0.0f && !Mathf.Approximately(candidateVelocity.y, 0.0f));
            m_Animator.SetFloat("Velocity", candidateVelocity.magnitude);
        }

        void OnIsGroundedChanged(bool isGrounded)
        {
            //m_RollbackState.Name = "Land";
            m_Animator.SetTrigger("Landed");

            if (m_MetadataManager.Id == 0)
                Debug.Log("Land: " + GetCurrentAnimationName());
        }

        void OnIsKickingChanged(bool isKicking)
        {
            //m_RollbackState.Name = "Kick";
            m_Animator.SetBool("IsKicking", isKicking);

            if (m_MetadataManager.Id == 0)
                Debug.Log("Kick: " + GetCurrentAnimationName());
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            ///m_RollbackState.Name = "Hit";
            m_Animator.SetTrigger("GotHit");

            if (m_MetadataManager.Id == 0)
                Debug.Log("Hit: " + GetCurrentAnimationName());
        }
    }
}
