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
    [RequireComponent(typeof(MetadataManager), typeof(MovementManager), typeof(InputManager)), RequireComponent(typeof(SpriteRenderer))]
    public abstract class Player : MonoBehaviour
    {
        public int Id => MetadataManager.Id;
        public int Lives => MetadataManager.Lives;
        public bool IsDefeated => MetadataManager.IsDefeated;

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

        protected virtual void Awake()
        {
            MetadataManager = GetComponent<MetadataManager>();
            m_MovementManager = GetComponent<MovementManager>();
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialise(int id, string name)
        {
            MetadataManager.Initialise(id, name);
            m_InputManager.Initialise();
            m_MovementManager.Reset();
        }

        public void ResetForMatch()
        {
            MetadataManager.ResetLives();
            ResetForRound();
        }

        public void ResetForRound()
        {
            m_MovementManager.Reset();
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
            Assert.IsTrue(m_InputManager.HasInput(TickService.Subtract(tick, 1)));
            Assert.IsTrue(m_InputManager.HasInput(tick));

            m_MovementManager.RunMovement(tick);
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

        public void DisposeInputs(ushort tickJustSimulated)
        {
            m_InputManager.DisposeInputs(tickJustSimulated);
        }
    }
}
