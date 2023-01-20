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
    [RequireComponent(typeof(InputManager), typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public class MovementManager : MonoBehaviour
    {
        [Tooltip("Objects in these layers will be considered as obstacles")]
        [SerializeField]
        string[] ObstacleLayers = new string[]{ "Obstacle" };

        [Tooltip("Distance from the bottom of the character to raycast for the ground")]
        [SerializeField]
        float GroundCheckDistance = 0.05f;

        [Tooltip("The player will slide off ground planes steeper than this value (degrees)")]
        [SerializeField]
        float MaximumGroundAngle = 45f;

        [SerializeField]
        float GroundSpeed = 16f;

        [SerializeField]
        float AirStrafeSpeed = 7f;

        [Tooltip("Air strafe lerp speed")]
        [SerializeField]
        float AirStrafeInfluenceSpeed = 7f;

        [SerializeField]
        float GravityDownForce = 130f;

        [SerializeField]
        float JumpMagnitude = 40f;

        [SerializeField]
        float KickMagnitude = 35f;

        [Tooltip("Angle between the horizontal and the kick")]
        [SerializeField]
        float KickAngle = 60f;

        [SerializeField]
        GameObject KickCollider;

        Vector3 m_KickVector;

        InputManager m_InputManager;
        BoxCollider2D m_CollisionCollider;
        Rigidbody2D m_RB2D;

        Vector2 m_CandidateVelocity;
        bool m_IsGrounded;
        Vector2 m_GroundNormal;
        Collider2D m_GroundCollider;
        bool m_IsFacingLeft = false;
        Vector2 m_CandidatePosition;
        bool m_IsKicking = false;

        void Awake()
        {
            m_InputManager = GetComponent<InputManager>();
            m_CollisionCollider = GetComponent<BoxCollider2D>();
            m_RB2D = GetComponent<Rigidbody2D>();

            Assert.IsTrue(KickCollider != null);
            Reset();

            m_KickVector = (Quaternion.AngleAxis(KickAngle, Vector3.forward) * Vector3.left).normalized;
        }

        public void Reset()
        {
            m_CandidateVelocity = Vector2.zero;
            m_IsGrounded = false;
            m_GroundNormal = Vector2.zero;
            m_GroundCollider = null;
            m_IsFacingLeft = false;
            m_CandidatePosition = Vector2.zero;
            StopKick();
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
            AdjustVelocityForObstructions();

            // Move along the final m_CandidateVelocity
            m_CandidatePosition += m_CandidateVelocity * deltaTime;

            if (m_CandidatePosition != m_RB2D.position)
            {
                m_RB2D.MovePosition(m_CandidatePosition);
            }

            SetFacingDirection();
        }

        // Nullifies velocity
        public void Teleport(Vector2 position, bool faceLeft)
        {
            m_CandidateVelocity = Vector2.zero;
            m_IsFacingLeft = faceLeft;

            // Perform the teleport instantaneously (don't wait for the next physics step)
            transform.position = position;
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
                LayerMask.GetMask(ObstacleLayers)
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

                StopKick();

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
                // Airborne and not kicking
                else if (!m_IsKicking)
                {
                    if (m_InputManager.GetInputDown(tick, InputMasks.Kick))
                    {            
                        Kick();
                        return;
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
                JumpMagnitude
            );
        }

        void Kick()
        {
            m_IsKicking = true;
            KickCollider.SetActive(true);

            SetFacingDirection();

            m_CandidateVelocity = KickMagnitude * m_KickVector;
            
            if (!m_IsFacingLeft)
            {
                m_CandidateVelocity.x *= -1;
            };
        }

        void StopKick()
        {
            m_IsKicking = false;
            KickCollider.SetActive(false);
        }

        // Checks for obstructions and sets m_CandidatePosition and CandidateVelocity appropriately if an obstruction is detected
        void AdjustVelocityForObstructions()
        {
            // Cast the collision collider in the direction of CandidateVelocity
            foreach (RaycastHit2D hit in Physics2D.BoxCastAll(
                m_CandidatePosition,
                m_CollisionCollider.size,
                0f,
                m_CandidateVelocity.normalized,
                m_CandidateVelocity.magnitude * Time.fixedDeltaTime,
                LayerMask.GetMask(ObstacleLayers)
            ))
            {
                // Ignore if nothing was collided with
                if (hit.collider == null)
                {
                    continue;
                }

                // Ignore if the collider isn't actually being moved into, i.e. if the player is just inside/on the collider
                if (!IsMovingInto(m_CandidateVelocity.normalized, hit.normal))
                {
                    continue;
                }

                // Snap to the obstruction
                m_CandidatePosition = hit.centroid;

                // Subtract the distance that was moved by snapping
                float remainingMagnitude = (m_CandidateVelocity.x < 0 ? -1 : 1) * Mathf.Abs(Mathf.Abs(m_CandidateVelocity.magnitude) - hit.distance);

                if (m_IsGrounded)
                {
                    if (IsTooSteep(hit.normal))
                    {
                        // Moving from ground -> steep ground: stop motion
                        m_CandidateVelocity = Vector2.zero;
                    }
                    else
                    {
                        if (!IsTooSteep(m_GroundNormal))
                        {
                            // Moving from regular ground -> regular ground: reorientate movement along the next ground
                            m_CandidateVelocity = remainingMagnitude * VectorExtensions.VectorAlongSurface(hit.normal);
                        }
                        else
                        {
                            // Moving from steep ground -> regular ground: stop motion
                            m_CandidateVelocity = Vector2.zero;
                        }
                    }
                }
                else
                {
                    if (!IsCeiling(hit.normal) && m_CandidateVelocity.y > 0 && IsMovingInto(m_CandidateVelocity, hit.normal))
                    {
                        // Running into a non-ceiling ground while rising in the air: ignore horizontal movement and just keep rising
                        m_CandidateVelocity = new Vector2(0f, m_CandidateVelocity.y);
                    }
                    else
                    {
                        // Moving from air -> ground: stop motion
                        m_CandidateVelocity = Vector2.zero;
                    }
                }
            }
        }

        void SetFacingDirection()
        {
            Vector3 newScale = transform.localScale;

            if (m_CandidateVelocity.x < 0)
            {
                m_IsFacingLeft = true;

                newScale.x = -1 * Math.Abs(newScale.x);
                transform.localScale = newScale;
            }
            else if (m_CandidateVelocity.x > 0)
            {
                m_IsFacingLeft = false;

                newScale.x = Math.Abs(newScale.x);
                transform.localScale = newScale;
            }
        }

        // Returns whether or not direction is moving into the surface with the given normal. Assumes both parameters are normalized.
        bool IsMovingInto(Vector2 direction, Vector2 normal)
        {
            // If direction is within +-90 degrees of the vector moving into the surface
            return Vector2.Dot(-normal, direction) > 0.01f;
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
