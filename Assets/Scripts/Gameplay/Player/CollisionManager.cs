using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

// Hitboxes and hurtboxes

/*
    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }
        if (collision.relativeVelocity.magnitude > 2)
            audioSource.Play();
    }
*/

// TODO
// include in Player, PeerPlayer and SelfPlayer

using Lockstep;

namespace Lockstep
{
    public class CollisionManager : MonoBehaviour
    {
    }
}
