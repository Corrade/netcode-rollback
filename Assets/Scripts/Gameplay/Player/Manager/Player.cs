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
    [RequireComponent(typeof(MetadataManager), typeof(MovementManager), typeof(InputManager)), RequireComponent(typeof(SpriteRenderer), typeof(SimulationStateManager))]
    public abstract class Player : MonoBehaviour
    {
        public int Id => MetadataManager.Id;
        public int Lives => MetadataManager.Lives;
        public bool IsDefeated => MetadataManager.IsDefeated;
        public Vector2 Position => m_MovementManager.Position;
        public Vector2 KickColliderPosition => m_MovementManager.KickColliderPosition;

        public bool IsSimulatingOfficially
        {
            get { return m_SimulationStateManager.IsSimulatingOfficially; }
            set { m_SimulationStateManager.IsSimulatingOfficially = value; }
        }

        public event Action<MetadataManager> MetadataUpdated
        {
            add { MetadataManager.MetadataUpdated += value; }
            remove { MetadataManager.MetadataUpdated -= value; }
        }

        public event Action<MetadataManager> LifeLost
        {
            add { MetadataManager.LifeLost += value; }
            remove { MetadataManager.LifeLost -= value; }
        }

        public MetadataManager MetadataManager { get; private set; }

        protected abstract InputManager m_InputManager { get; }
        protected MovementManager m_MovementManager;
        protected SpriteRenderer m_SpriteRenderer;
        protected SimulationStateManager m_SimulationStateManager;

        protected virtual void Awake()
        {
            MetadataManager = GetComponent<MetadataManager>();
            m_MovementManager = GetComponent<MovementManager>();
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            m_SimulationStateManager = GetComponent<SimulationStateManager>();
        }

        public void Initialise(int id, string name)
        {
            MetadataManager.Initialise(id, name);
            m_InputManager.Initialise();
            m_MovementManager.Reset();
        }

        // This should only be called for non-first rounds since
        // Initialise() handles the setup for the first round.
        // Specifically, Initialise() establishes the input managers'
        // networking so resetting them afterwards might overwrite recently
        // received data.
        public void ResetForNonFirstRound(ushort startTick)
        {
            m_MovementManager.Reset();
            m_InputManager.ResetForRound(startTick);
        }

        public bool HasInput(ushort tick)
        {
            return m_InputManager.HasInput(tick);
        }

        public void SetSpriteVisible(bool visible)
        {
            m_SpriteRenderer.enabled = visible;
        }

        public void Simulate(ushort tick)
        {
            // Prior ticks are needed for GetInputDown() and GetInputUp()
            Assert.IsTrue(m_InputManager.HasInput(tick));
            Assert.IsTrue(m_InputManager.HasInput(TickService.Subtract(tick, 1)));

            m_MovementManager.Simulate(tick);
        }

        public void SaveRollbackState()
        {
            m_MovementManager.SaveRollbackState();
        }

        public void Rollback()
        {
            m_MovementManager.Rollback();
        }

        public void Teleport(Vector2 position, bool faceLeft)
        {
            m_MovementManager.Teleport(position, faceLeft);
        }

        public void LoseLife()
        {
            MetadataManager.LoseLife();
        }

        public void DisposeInputs(ushort untilTickExclusive)
        {
            m_InputManager.DisposeInputs(untilTickExclusive);
        }
    }
}
