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
    public class MovementState
    {
        public Vector2 RigidbodyPosition;
        public Vector2 CandidatePosition;
        public Vector2 GroundNormal;
        public Collider2D GroundCollider;

        public Vector2 CandidateVelocity
        {
            get { return m_CandidateVelocity; }
            set
            {
                m_CandidateVelocity = value;
                CandidateVelocityChanged?.Invoke();
            }
        }

        public bool IsGrounded
        {
            get { return m_IsGrounded; }
            set
            {
                m_IsGrounded = value;
                IsGroundedChanged?.Invoke();
            }
        }

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

        public event Action CandidateVelocityChanged;
        public event Action IsGroundedChanged;
        public event Action IsFacingLeftChanged;
        public event Action IsKickingChanged;

        Vector2 m_CandidateVelocity;
        bool m_IsGrounded;
        bool m_IsFacingLeft;
        bool m_IsKicking;

        public void Reset()
        {
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
            RigidbodyPosition = other.RigidbodyPosition;
            CandidateVelocity = other.CandidateVelocity;
            CandidatePosition = other.CandidatePosition;
            IsGrounded = other.IsGrounded;
            GroundNormal = other.GroundNormal;
            GroundCollider = other.GroundCollider;
            IsFacingLeft = other.IsFacingLeft;
            IsKicking = other.IsKicking;
        }

        public bool Equals(MovementState other)
        {
            return RigidbodyPosition == other.RigidbodyPosition
                && CandidateVelocity == other.CandidateVelocity
                && CandidatePosition == other.CandidatePosition
                && IsGrounded == other.IsGrounded
                && GroundNormal == other.GroundNormal
                && GroundCollider == other.GroundCollider
                && IsFacingLeft == other.IsFacingLeft
                && IsKicking == other.IsKicking;
        }

        public override string ToString()
        {
            return $"RigidbodyPosition={RigidbodyPosition}\n"
                + $"CandidateVelocity={CandidateVelocity}\n"
                + $"CandidatePosition={CandidatePosition}\n"
                + $"IsGrounded={IsGrounded}\n"
                + $"GroundNormal={GroundNormal}\n"
                + $"GroundCollider={GroundCollider}\n"
                + $"IsFacingLeft={IsFacingLeft}\n"
                + $"IsKicking={IsKicking}";
        }
    }
}
