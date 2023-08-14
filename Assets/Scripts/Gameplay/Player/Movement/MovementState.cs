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

        // Unfortunately can't elegantly wrap these listenable variables in a
        // templated class as the assignment/set operator can't be overloaded
        public Vector2 CandidateVelocity
        {
            get { return m_CandidateVelocity; }
            set
            {
                if (m_CandidateVelocity == value) return;
                m_CandidateVelocity = value;
                CandidateVelocityChanged?.Invoke(value);
            }
        }

        public bool IsGrounded
        {
            get { return m_IsGrounded; }
            set
            {
                if (m_IsGrounded == value) return;
                m_IsGrounded = value;
                IsGroundedChanged?.Invoke(value);
            }
        }

        public bool IsFacingLeft
        {
            get { return m_IsFacingLeft; }
            set
            {
                if (m_IsFacingLeft == value) return;
                m_IsFacingLeft = value;
                IsFacingLeftChanged?.Invoke(value);
            }
        }

        public bool IsKicking
        {
            get { return m_IsKicking; }
            set
            {
                if (m_IsKicking == value) return;
                m_IsKicking = value;
                IsKickingChanged?.Invoke(m_IsKicking);
            }
        }

        public event Action<Vector2> CandidateVelocityChanged;
        public event Action<bool> IsGroundedChanged;
        public event Action<bool> IsFacingLeftChanged;
        public event Action<bool> IsKickingChanged;

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
