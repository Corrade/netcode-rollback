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
    [RequireComponent(typeof(MetadataManager), typeof(MovementManager), typeof(InputManager)), RequireComponent(typeof(CollisionManager))]
    public abstract class Player : MonoBehaviour
    {
        public int Id { get { return m_MetadataManager.Id; } }

        public event Action<MetadataManager> MetadataUpdated
        {
            add
            {
                m_MetadataManager.MetadataUpdated += value;
            }

            remove
            {
                m_MetadataManager.MetadataUpdated -= value;
            }
        }

        protected abstract InputManager m_InputManager { get; }
        protected MetadataManager m_MetadataManager;
        protected MovementManager m_MovementManager;
        protected CollisionManager m_CollisionManager;
        protected ConnectionManager m_ConnectionManager;

        protected virtual void Awake()
        {
            m_MetadataManager = GetComponent<MetadataManager>();
            m_MovementManager = GetComponent<MovementManager>();
            m_CollisionManager = GetComponent<CollisionManager>();
        }

        public void Initialise(int id, string name, ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
            m_MetadataManager.Initialise(id, name);
            m_InputManager.Initialise(connectionManager);
        }

        public bool HasInput(ushort tick)
        {
            return m_InputManager.HasInput(tick);
        }

        public void Simulate(ushort tick)
        {
            Assert.IsTrue(m_InputManager.HasInput(tick));
            m_MovementManager.RunMovement(tick);
        }

        public void Teleport(Vector2 position, bool faceLeft)
        {
            m_MovementManager.Teleport(position, faceLeft);
        }

        public void ResetLives()
        {
            m_MetadataManager.ResetLives();
        }
    }
}
