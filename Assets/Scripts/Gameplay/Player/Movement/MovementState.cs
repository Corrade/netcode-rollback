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
    public struct MovementState
    {
        public Vector2 Position;
        public Vector2 RigidbodyPosition;

        public Vector2 CandidateVelocity;
        public Vector2 CandidatePosition;
        public bool IsGrounded;
        public Vector2 GroundNormal;
        public Collider2D GroundCollider;

        public bool IsFacingLeft
        {
            get { return m_IsFacingLeft; }
            set
            {
                m_IsFacingLeft = value;
                IsFacingLeftChanged?.Invoke();
            }
        }

        public bool IsKicking
        {
            get { return m_IsKicking; }
            set
            {
                m_IsKicking = value;
                IsKickingChanged?.Invoke();
            }
        }

        public event Action IsFacingLeftChanged;
        public event Action IsKickingChanged;

        bool m_IsFacingLeft;
        bool m_IsKicking;

        public void Reset()
        {
            Position = Vector2.zero;
            RigidbodyPosition = Vector2.zero;

            CandidateVelocity = Vector2.zero;
            CandidatePosition = Vector2.zero;
            IsGrounded = false;
            GroundNormal = Vector2.zero;
            GroundCollider = null;

            IsFacingLeft = false;
            IsKicking = false;
        }

        // Assign this struct using this function to ensure that 1) event
        // listeners are preserved and 2) the events themselves are triggered
        public void Assign(MovementState other)
        {
            Position = other.Position;
            RigidbodyPosition = other.RigidbodyPosition;

            CandidateVelocity = other.CandidateVelocity;
            CandidatePosition = other.CandidatePosition;
            IsGrounded = other.IsGrounded;
            GroundNormal = other.GroundNormal;
            GroundCollider = other.GroundCollider;

            IsFacingLeft = other.IsFacingLeft;
            IsKicking = other.IsKicking;
        }
    }
}
