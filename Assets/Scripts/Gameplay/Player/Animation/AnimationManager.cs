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

        [SerializeField]
        ushort LandAnimationDurationTick = 5;

        // This can't be included in the AnimationState variables. If it was,
        // then we'd assign it during rollback and thereby decouple it from
        // the actual current animation name. Then, after the corresponding
        // game loop, we'd encounter an incorrect value in SwitchToAnimation.
        public string CurrentAnimationName { get; private set; }

        Animator m_Animator;
        MovementManager m_MovementManager;
        MetadataManager m_MetadataManager;
        Player m_Player;

        AnimationState m_State = new AnimationState();
        AnimationState m_RollbackState = new AnimationState();

        void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_MovementManager = GetComponent<MovementManager>();
            m_MetadataManager = GetComponent<MetadataManager>();
            m_Player = GetComponent<Player>();

            m_MovementManager.IsGroundedChanged += OnIsGroundedChanged;
            m_MetadataManager.LifeLost += OnLifeLost;

            Assert.IsTrue(m_Animator.layerCount == 1);

            m_Animator.StartPlayback();
        }

        public void Reset()
        {
            m_State.Reset();
            m_RollbackState.Reset();
            SwitchToAnimation("Idle");
        }

        public void Simulate()
        {
            // Defer propagating these changes to the animator until rendering
            m_State.MotionTime += MotionTimeScale * TickService.TimeBetweenTicksSec;
        }

        public void SaveRollbackState()
        {
            m_RollbackState.Assign(m_State);
        }

        public void Rollback()
        {
            m_State.Assign(m_RollbackState);
        }

        public void Render()
        {
            m_Animator.SetFloat("MotionTime", m_State.MotionTime);

            if (m_State.IsHit)
            {
                SwitchToAnimation("Hit");
                return;
            }

            if (RecentlyLanded())
            {
                SwitchToAnimation("Land");
                return;
            }

            if (m_MovementManager.IsKicking)
            {
                SwitchToAnimation("Kick");
                return;
            }

            if (m_MovementManager.Velocity.y > 0.0f && !Mathf.Approximately(m_MovementManager.Velocity.y, 0.0f))
            {
                SwitchToAnimation("Jump");
                return;
            }

            if (!Mathf.Approximately(m_MovementManager.Velocity.x, 0.0f))
            {
                SwitchToAnimation("Move");
                return;
            }

            SwitchToAnimation("Idle");
        }

        void OnIsGroundedChanged(bool isGrounded)
        {
            if (isGrounded)
            {
                // Actually this name is misleading. We just take the
                // current/latest tick, not the tick associated with the
                // simulation in which the player just landed. Thankfully our
                // approach still works and is simpler.
                m_State.LastLandedAtTick = Clock.Instance.CurrentTick;
            }
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            m_State.IsHit = true;
        }

        void SwitchToAnimation(string name)
        {
            if (name == CurrentAnimationName)
            {
                return;
            }

            m_Animator.Play(name, layer: 0, normalizedTime: m_State.MotionTime % 1.0f);
            CurrentAnimationName = name;

            // Debug.Log($"Switched to {CurrentAnimationName}");
        }

        bool RecentlyLanded()
        {
            return TickService.Subtract(Clock.Instance.CurrentTick, m_State.LastLandedAtTick) < LandAnimationDurationTick;
        }
    }
}
