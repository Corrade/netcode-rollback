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
    public class AnimationState
    {
        public float MotionTime;
        public ushort LastLandedAtTick;
        public bool IsHit;

        public void Reset()
        {
            MotionTime = 0.0f;
            LastLandedAtTick = 0;
            IsHit = false;
        }

        public void Assign(AnimationState other)
        {
            MotionTime = other.MotionTime;
            LastLandedAtTick = other.LastLandedAtTick;
            IsHit = other.IsHit;
        }
    }
}
