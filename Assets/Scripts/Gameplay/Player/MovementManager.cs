using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DarkRift.Server;
using DarkRift.Server.Unity;

using Lockstep;

namespace Lockstep
{
    [RequireComponent(typeof(InputManager), typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public class MovementManager : MonoBehaviour
    {
        [Tooltip("Objects in these layers will be considered as ground")]
        [SerializeField]
        string[] GroundLayers = new string[]{ "Ground" };

        [Tooltip("Distance from the bottom of the character to raycast for the ground")]
        [SerializeField]
        float GroundCheckDistance = 0.05f;

        [Tooltip("The player will slide off ground planes steeper than this value (degrees)")]
        [SerializeField]
        float MaximumGroundAngle = 45f;

        [Tooltip("Horizontal speed")]
        [SerializeField]
        float GroundSpeed = 16f;

        [Tooltip("Air strafe speed")]
        [SerializeField]
        float AirStrafeSpeed = 7f;

        [Tooltip("Air strafe lerp speed")]
        [SerializeField]
        float AirStrafeInfluenceSpeed = 7f;

        [Tooltip("Force applied downward when in the air")]
        [SerializeField]
        float GravityDownForce = 130f;

        [Tooltip("This velocity is immediately applied to the player when they jump")]
        [SerializeField]
        float JumpVelocity = 40f;

        InputManager m_InputManager;
        BoxCollider2D m_CollisionCollider;
        Rigidbody2D m_RB2D;

        Vector2 m_CandidateVelocity;
        bool m_IsGrounded;
        Vector2 m_GroundNormal;
        Collider2D m_GroundCollider;
        bool m_IsFacingLeft;
        Vector2 m_CandidatePosition;

        void Awake()
        {
            m_InputManager = GetComponent<InputManager>();
            m_CollisionCollider = GetComponent<BoxCollider2D>();
            m_RB2D = GetComponent<Rigidbody2D>();
        }

        public void RunMovement(ushort tick)
        {
            float deltaTime = TickService.TimeBetweenTicksSec;

            // Calls to this function should be independent from each other.
            // However, this function uses private member variables that preserve state from prior calls.
            // This isn't an issue for lockstep since we only call this function for consecutive ticks, but
            // things will need to be re-designed for more advanced network architectures.

            m_CandidatePosition = m_RB2D.position;

            GroundCheck();
            ProposeVelocity(tick, deltaTime);

            // Move along the final m_CandidateVelocity
            m_CandidatePosition += m_CandidateVelocity * deltaTime;

            if (m_CandidatePosition != m_RB2D.position)
            {
                m_RB2D.MovePosition(m_CandidatePosition);
                // MovePosition() is resolved during the next physics update
            }

            SetFacingDirection();
        }

        // Nullifies velocity
        public void Teleport(Vector2 position, bool faceLeft)
        {
            m_CandidateVelocity = Vector2.zero;
            m_RB2D.MovePosition(position);
            m_IsFacingLeft = faceLeft;
        }

        // Check whether or not the player is grounded and set the related variables appropriately
        void GroundCheck()
        {
            // Don't check grounding if ascending in the air, e.g. whilst in the upward section of jumping
            if (IsRising())
            {
                return;
            }

            // Reset grounding variables
            m_IsGrounded = false;
            m_GroundNormal = Vector2.up;
            m_GroundCollider = null;

            // Cast the collision collider down by GroundCheckDistance units
            foreach (RaycastHit2D hit in Physics2D.BoxCastAll(
                m_CandidatePosition,
                m_CollisionCollider.size,
                0f,
                Vector2.down,
                GroundCheckDistance,
                LayerMask.GetMask(GroundLayers)
            ))
            {
                // Ignore if nothing was collided with
                if (hit.collider == null)
                {
                    continue;
                }

                // The collider is a line spanning the whole height of the
                // character. Hence, it could incorrectly detect a platform
                // that only the character's mid/upper body is colliding with,
                // which would be considered a ceiling.
                if (IsCeiling(hit.normal))
                {
                    continue;
                }

                m_IsGrounded = true;
                m_GroundNormal = hit.normal;
                m_GroundCollider = hit.collider;
                m_CandidatePosition = hit.centroid;

                // Prevent scaling steep walls with jump resets
                if (!IsTooSteep(hit.normal))
                {
                    // Prioritise picking a flat ground so that if the player
                    // is at an intersection between flat and steep grounds,
                    // they will not be grounded on the steeper surface and
                    // forced to slide into the flat ground indefinitely
                    return;
                }
            }
        }

        // Set m_CandidateVelocity based on input and grounding
        void ProposeVelocity(ushort tick, float deltaTime)
        {
            float moveInput = m_InputManager.GetMoveInput(tick);

            // Grounded
            if (m_IsGrounded && !IsTooSteep(m_GroundNormal))
            {
                float input = moveInput * GroundSpeed;
                m_CandidateVelocity = input * VectorExtensions.VectorAlongSurface(m_GroundNormal);

                if (m_InputManager.GetInputDown(tick, InputMasks.Dive))
                {
                    Dive();
                }
            }
            else
            {
                // Sliding down a steep surface
                if (m_IsGrounded && IsTooSteep(m_GroundNormal))
                {
                    m_CandidateVelocity += VectorExtensions.VectorDownSurface(m_GroundNormal) * GravityDownForce * deltaTime;
                }
                // Airborne
                else
                {
                    if (m_InputManager.GetInputDown(tick, InputMasks.Kick))
                    {
                        Kick();
                    }

                    // Air strafing
                    float input = moveInput * AirStrafeSpeed;

                    // Apply air strafing
                    m_CandidateVelocity = new Vector2(
                        Mathf.Lerp(m_CandidateVelocity.x, input, AirStrafeInfluenceSpeed * deltaTime),
                        m_CandidateVelocity.y
                    );

                    // Apply gravity
                    m_CandidateVelocity = new Vector2(
                        m_CandidateVelocity.x,
                        m_CandidateVelocity.y - GravityDownForce * deltaTime
                    );
                }
            }
        }

        void Dive()
        {
            m_IsGrounded = false;

            m_CandidateVelocity = new Vector2(
                m_CandidateVelocity.x,
                JumpVelocity
            );
        }

        void Kick()
        {
            // TODO
        }

        void SetFacingDirection()
        {
            if (m_CandidateVelocity.x < 0)
            {
                m_IsFacingLeft = true;
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else if (m_CandidateVelocity.x > 0)
            {
                m_IsFacingLeft = false;
                transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        // Given the normal of a ground, returns whether or not it's too steep
        bool IsTooSteep(Vector2 normal)
        {
            return Vector2.Dot(normal, Vector2.up) < Mathf.Cos(MaximumGroundAngle * Mathf.Deg2Rad);
        }

        // Given the normal of a ground, returns whether or not it's a ceiling
        bool IsCeiling(Vector2 normal)
        {
            return normal.y < 0;
        }

        // Returns whether or not the character is in the air and moving upwards
        bool IsRising()
        {
            return !m_IsGrounded && m_CandidateVelocity.y > 0f;
        }
    }
}
