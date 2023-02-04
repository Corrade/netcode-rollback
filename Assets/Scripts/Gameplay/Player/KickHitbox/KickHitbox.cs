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

1. OnTriggerEnter2D() gets called on both the trigger and the non-trigger collider.

2. We put KickHitbox on the hitbox so we expect that the argument to
OnTriggerEnter2D() is a player's collider.

3. If you don't put a RigidBody on an object with a collider, that collider
is re-parented to the nearest ancestor with a RigidBody.
If that collider is a trigger, then that ancestor gains the trigger.
For example, if we were to remove a RigidBody from a trigger child of a
player, then that player would receive the trigger.
*/

/*
CHANGES FROM LOCKSTEP

In the lockstep code, we put the collision script on the player and had it
damage itself upon being hit.
Here, we put the collision script on the hitbox and have it damage whatever it
hits.

Essentially, we now handle collision from the attacker's perspective.

We performed this refactor in order to integrate with prediction. We don't
want to deal damage during prediction. So, we want to intuitively write
something like "if predicting, then don't deal damage" (SelfKickHitbox).

This is expressed from the attacker's perspective, so naturally that's the
correct perspective to take here.
*/

namespace Lockstep
{
    public class KickHitbox : MonoBehaviour
    {
        public MetadataManager MetadataManager;

        protected virtual void Awake()
        {
            Assert.IsTrue(MetadataManager != null);
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            MetadataManager otherMetadataManager = other.GetComponent<MetadataManager>();

            // Other collider isn't a player
            if (otherMetadataManager == null)
            {
                return;
            }

            // Other collider is oneself
            if (MetadataManager.Id == otherMetadataManager.Id)
            {
                return;
            }

            otherMetadataManager.LoseLife();
        }
    }
}
