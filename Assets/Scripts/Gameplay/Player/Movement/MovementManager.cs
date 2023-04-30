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
    [RequireComponent(typeof(InputManager), typeof(BoxCollider2D), typeof(Rigidbody2D)), RequireComponent(typeof(MetadataManager))]
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

        public Vector2 Position => m_RB2D.position;
        public Vector2 KickColliderPosition => KickCollider.transform.position;

        Vector3 m_KickVector;

        InputManager m_InputManager;
        BoxCollider2D m_CollisionCollider;
        Rigidbody2D m_RB2D;
        MetadataManager m_MetadataManager;

        MovementState m_State = new MovementState();
        MovementState m_RollbackState = new MovementState();

        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        MovementState[] m_DebugStateHistory = new MovementState[TickService.MaxTick];
        bool[] m_DebugStateSimulated = new bool[TickService.MaxTick];
        #endif

        void Awake()
        {
            m_InputManager = GetComponent<InputManager>();
            m_CollisionCollider = GetComponent<BoxCollider2D>();
            m_RB2D = GetComponent<Rigidbody2D>();
            m_MetadataManager = GetComponent<MetadataManager>();

            Assert.IsTrue(KickCollider != null);
            m_KickVector = (Quaternion.AngleAxis(KickAngle, Vector3.forward) * Vector3.left).normalized;

            m_State.IsFacingLeftChanged += OnIsFacingLeftChanged;
            m_State.IsKickingChanged += OnIsKickingChanged;

            SimulationManager.Instance.Simulated += OnSimulated;
        }

        void OnDestroy()
        {
            SimulationManager.Instance.Simulated -= OnSimulated;
        }

        public void Reset()
        {
            m_State.Reset();
            m_RollbackState.Reset();
        }

        // Doesn't execute until the next simulation step
        public void Simulate(ushort tick)
        {
            // (*) Invariant: m_RB2D.position == m_State.RigidbodyPosition

            float deltaTime = TickService.TimeBetweenTicksSec;

            m_State.CandidatePosition = m_RB2D.position;

            DebugUI.WriteSequenced(
                $"{m_MetadataManager.Id} Simulate() start",
                $"id={m_MetadataManager.Id} Simulate() start m_State.RigidbodyPosition={m_State.RigidbodyPosition}, transform.position={transform.position}"
            );

            GroundCheck();
            ProposeVelocity(tick, deltaTime);
            AdjustVelocityForObstructions();

            // Move along the final m_State.CandidateVelocity
            m_State.CandidatePosition += m_State.CandidateVelocity * deltaTime;

            if (m_State.CandidatePosition != m_RB2D.position)
            {
                // Assigns to m_RB2D.position *in the next simulation*
                m_RB2D.MovePosition(m_State.CandidatePosition);

                // It's tempting to believe that we just falsified the
                // invariant (*) by "assigning" to m_RB2D.position.
                // However, this assignment hasn't actually unfolded yet since
                // we've yet to run a simulation step.
                // The invariant is still true and will only become false
                // after simulation. So, we'll defer doing anything until
                // then (see OnSimulated()).
            }

            DebugUI.WriteSequenced(
                $"{m_MetadataManager.Id} Simulate() end",
                $"id={m_MetadataManager.Id} Simulate() end m_State.RigidbodyPosition={m_State.RigidbodyPosition}, transform.position={transform.position}"
            );

            UpdateIsFacingLeftBasedOnVelocity();
        }

        public void SaveRollbackState()
        {
            DebugUI.WriteSequenced(
                $"{m_MetadataManager.Id} SaveRollbackState()",
                $"id={m_MetadataManager.Id} SaveRollbackState() m_State.RigidbodyPosition={m_State.RigidbodyPosition}"
            );

            m_RollbackState.Assign(m_State);
        }

        public void Rollback()
        {
            DebugUI.WriteSequenced(
                $"{m_MetadataManager.Id} Rollback() start",
                $"id={m_MetadataManager.Id} Rollback() start transform.position={transform.position}, m_RB2D.position={m_RB2D.position}"
            );

            m_State.Assign(m_RollbackState);

            /*
            WHY WE NEED TO REFRESH THE KICK COLLIDER

            A single collision will trigger its related events exactly once.
            Furthermore, a colliding object will be unable to trigger
            more collisions for a short period of time, i.e. there is a
            cooldown between collisions (?).

            https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter2D.html

            This cooldown in particular can cause bugs when combined with
            rollback. For example:

            1. t=1 Official simulation
            2. t=2 Unofficial simulation: two objects collide with each other
            3. Rollback to t=1
            4. t=2 Official simulation: the objects should collide, but the
            collision isn't registered because it recently occurred in the
            unofficial simulation and is therefore on cooldown.

            To fix this, we "refresh" the kick collider during every rollback
            by turning it off and on again (if it's supposed to be on).
            */

            KickCollider.SetActive(false);
            SyncKickColliderWithState();

            // Preserve invariant (*)
            m_RB2D.position = m_State.RigidbodyPosition;

            /*
            WHY TELEPORTING (MODIFYING TRANSFORM.POSITION DIRECTLY) IS
            ESSENTIAL, AND HOW TO IMPLEMENT IT CORRECTLY

            Consider the following situation where we rollback by setting
            rigidbody position, which is resolved using interpolation, instead
            of actually teleporting:

            1. Save rollback at a normal simulation state
            2. Unofficial simulation: suppose we detect a collision at tick t,
            but we block it because we're in an unofficial simulation (*)
            3. Next game loop
            4. Rollback to the normal simulation state (**) using rigidbody
            position
            5. Official simulation: we interpolate to the rollback state (**)
            from the current state (*) because moving via rigidbody position
            is performed using interpolation
            6. Collision detected!
            => Collision in the official simulation is caused when we interpolate
            from a blocked collision in the previous tick's unofficial state.

            To fix this, we should evaluate the rollback by cleanly teleporting
            without interpolation.

            But we still want interpolation for regular movement for
            performance and to prevent colliders from potentially
            skipping over each other.

            So, in this overall project, we set positions via both the
            transform and the rigidbody. To ensure this is safe, after every
            direct transform adjustment, we call Physics2D.SyncTransforms().
            Not calling this results in strange behaviour where running the
            simulation doesn't do anything.
            */

            transform.position = m_State.RigidbodyPosition;
            Physics2D.SyncTransforms();
            
            Assert.IsTrue(m_RB2D.position == m_State.RigidbodyPosition);

            DebugUI.WriteSequenced(
                $"{m_MetadataManager.Id} Rollback() end",
                $"id={m_MetadataManager.Id} Rollback() end transform.position={transform.position}, m_RB2D.position={m_RB2D.position}"
            );
        }

        // Nullifies velocity
        public void Teleport(Vector2 position, bool faceLeft)
        {
            m_State.CandidateVelocity = Vector2.zero;
            m_State.IsFacingLeft = faceLeft;

            // Preserve invariant (*)
            m_State.RigidbodyPosition = position;
            m_RB2D.position = position;
        }

        public void StopKicking()
        {
            m_State.IsKicking = false;
        }

        void OnSimulated(ushort untilTickExclusive)
        {
            // The simulation has just applied any changes from Simulate()
            // to m_RB2D.position

            // Preserve invariant (*)
            m_State.RigidbodyPosition = m_RB2D.position;

            DebugUI.WriteSequenced(
                $"{m_MetadataManager.Id} OnSimulated()",
                $"id={m_MetadataManager.Id} OnSimulated() m_State.RigidbodyPosition={m_State.RigidbodyPosition}, transform.position={transform.position}"
            );

            // AssertSimulatedStateEqualsPrior(untilTickExclusive);
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
            m_State.IsGrounded = false;
            m_State.GroundNormal = Vector2.up;
            m_State.GroundCollider = null;

            // Cast the collision collider down by GroundCheckDistance units
            foreach (RaycastHit2D hit in Physics2D.BoxCastAll(
                m_State.CandidatePosition,
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

                m_State.IsGrounded = true;
                m_State.GroundNormal = hit.normal;
                m_State.GroundCollider = hit.collider;
                m_State.CandidatePosition = hit.centroid;

                StopKicking();

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

        // Set m_State.CandidateVelocity based on input and grounding
        void ProposeVelocity(ushort tick, float deltaTime)
        {
            float moveInput = m_InputManager.GetMoveInput(tick);

            // Grounded
            if (m_State.IsGrounded && !IsTooSteep(m_State.GroundNormal))
            {
                float input = moveInput * GroundSpeed;
                m_State.CandidateVelocity = input * VectorExtensions.VectorAlongSurface(m_State.GroundNormal);

                if (m_InputManager.GetInputDown(tick, InputMasks.Dive))
                {
                    Dive();
                }
            }
            else
            {
                // Sliding down a steep surface
                if (m_State.IsGrounded && IsTooSteep(m_State.GroundNormal))
                {
                    m_State.CandidateVelocity += VectorExtensions.VectorDownSurface(m_State.GroundNormal) * GravityDownForce * deltaTime;
                }
                // Airborne and not kicking
                else if (!m_State.IsKicking)
                {
                    if (m_InputManager.GetInputDown(tick, InputMasks.Kick))
                    {            
                        Kick();
                        return;
                    }

                    float input = moveInput * AirStrafeSpeed;

                    // Air strafing
                    m_State.CandidateVelocity = new Vector2(
                        Mathf.Lerp(m_State.CandidateVelocity.x, input, AirStrafeInfluenceSpeed * deltaTime),
                        m_State.CandidateVelocity.y
                    );

                    // Gravity
                    m_State.CandidateVelocity = new Vector2(
                        m_State.CandidateVelocity.x,
                        m_State.CandidateVelocity.y - GravityDownForce * deltaTime
                    );
                }
            }
        }

        void Dive()
        {
            m_State.IsGrounded = false;

            m_State.CandidateVelocity = new Vector2(
                m_State.CandidateVelocity.x,
                JumpMagnitude
            );
        }

        void Kick()
        {
            m_State.IsKicking = true;

            UpdateIsFacingLeftBasedOnVelocity();

            m_State.CandidateVelocity = KickMagnitude * m_KickVector;
            
            if (!m_State.IsFacingLeft)
            {
                m_State.CandidateVelocity = new Vector2(-m_State.CandidateVelocity.x, m_State.CandidateVelocity.y);
            };
        }

        // Checks for obstructions and sets m_State.CandidatePosition and CandidateVelocity appropriately if an obstruction is detected
        void AdjustVelocityForObstructions()
        {
            // Cast the collision collider in the direction of CandidateVelocity
            foreach (RaycastHit2D hit in Physics2D.BoxCastAll(
                m_State.CandidatePosition,
                m_CollisionCollider.size,
                0f,
                m_State.CandidateVelocity.normalized,
                m_State.CandidateVelocity.magnitude * Time.fixedDeltaTime,
                LayerMask.GetMask(ObstacleLayers)
            ))
            {
                // Ignore if nothing was collided with
                if (hit.collider == null)
                {
                    continue;
                }

                // Ignore if the collider isn't actually being moved into, i.e. if the player is just inside/on the collider
                if (!IsMovingInto(m_State.CandidateVelocity.normalized, hit.normal))
                {
                    continue;
                }

                // Snap to the obstruction
                m_State.CandidatePosition = hit.centroid;

                // Subtract the distance that was moved by snapping
                float remainingMagnitude = (m_State.CandidateVelocity.x < 0 ? -1 : 1) * Mathf.Abs(Mathf.Abs(m_State.CandidateVelocity.magnitude) - hit.distance);

                if (m_State.IsGrounded)
                {
                    if (IsTooSteep(hit.normal))
                    {
                        // Moving from ground -> steep ground: stop motion
                        m_State.CandidateVelocity = Vector2.zero;
                    }
                    else
                    {
                        if (!IsTooSteep(m_State.GroundNormal))
                        {
                            // Moving from regular ground -> regular ground: reorientate movement along the next ground
                            m_State.CandidateVelocity = remainingMagnitude * VectorExtensions.VectorAlongSurface(hit.normal);
                        }
                        else
                        {
                            // Moving from steep ground -> regular ground: stop motion
                            m_State.CandidateVelocity = Vector2.zero;
                        }
                    }
                }
                else
                {
                    if (!IsCeiling(hit.normal) && m_State.CandidateVelocity.y > 0 && IsMovingInto(m_State.CandidateVelocity, hit.normal))
                    {
                        // Running into a non-ceiling ground while rising in the air: ignore horizontal movement and just keep rising
                        m_State.CandidateVelocity = new Vector2(0f, m_State.CandidateVelocity.y);
                    }
                    else
                    {
                        // Moving from air -> ground: stop motion
                        m_State.CandidateVelocity = Vector2.zero;
                    }
                }
            }
        }

        void UpdateIsFacingLeftBasedOnVelocity()
        {
            // Preserve the existing facing direction if there's been no
            // change in velocity
            if (Mathf.Approximately(m_State.CandidateVelocity.x, 0))
            {
                return;
            }

            m_State.IsFacingLeft = (m_State.CandidateVelocity.x < 0);
        }

        void OnIsFacingLeftChanged()
        {
            Vector3 newScale = transform.localScale;

            newScale.x = m_State.IsFacingLeft
                ? -1 * Math.Abs(newScale.x)
                : Math.Abs(newScale.x);

            transform.localScale = newScale;
        }

        void OnIsKickingChanged()
        {
            DebugUI.WriteSequenced(
                $"{m_MetadataManager.Id} OnIsKickingChanged()",
                $"id={m_MetadataManager.Id} OnIsKickingChanged() active={m_State.IsKicking}"
            );

            SyncKickColliderWithState();
        }

        void SyncKickColliderWithState()
        {
            KickCollider.SetActive(m_State.IsKicking);
        }

        // If the given tick has been simulated before, then assert that the
        // current state is equal to the state of that prior simulation.
        // For the sake of simplicity and since this is only for debugging,
        // we assume that ticks do not wrap around.
        // NOTE: This is only useful for singleplayer tests. This assertion
        // obviously fails for real simulations done under prediction (enemy
        // input might invalidate the prediction) and often fails for peer
        // extrapolation.
        void AssertSimulatedStateEqualsPrior(ushort tick)
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_DebugStateSimulated[tick])
            {
                Assert.IsTrue(m_State.Equals(m_DebugStateHistory[tick]));
            }
            else
            {
                m_DebugStateSimulated[tick] = true;
                m_DebugStateHistory[tick].Assign(m_State);
            }
            #endif
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
            return !m_State.IsGrounded && m_State.CandidateVelocity.y > 0f;
        }
    }
}
