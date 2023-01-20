using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Lockstep;

/*
COLLIDER NOTES

OnTriggerEnter2D() gets called on both the trigger and the non-trigger collider.

We put CollisionManager on the player so we expect that the argument to
OnTriggerEnter2D() is the trigger.

If you don't put a RigidBody on an object with a collider, that collider
becomes a component of the nearest ancestor with a RigidBody.
If that collider is a trigger, then the parent gets a trigger.

Hence if we were to remove a RigidBody from a child of the player with a
trigger, then the player would receive a trigger.
*/

namespace Lockstep
{
    [RequireComponent(typeof(MetadataManager))]
    public class CollisionManager : MonoBehaviour
    {
        MetadataManager m_MetadataManager;

        void Awake()
        {
            m_MetadataManager = GetComponent<MetadataManager>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerReference playerReference = other.GetComponent<PlayerReference>();

            // Trigger doesn't belong to a player
            if (playerReference == null)
            {
                return;
            }

            Player owner = playerReference.Player;
            Assert.IsTrue(owner != null);

            // Trigger belongs to oneself
            if (owner.Id == m_MetadataManager.Id)
            {
                return;
            }

            m_MetadataManager.LoseLife();
        }
    }
}
