using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Lockstep;

namespace Lockstep
{
    public class SelfKickHitbox : KickHitbox
    {
        public SelfPredictionManager SelfPredictionManager;

        protected override void Awake()
        {
            base.Awake();
            Assert.IsTrue(SelfPredictionManager != null);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (SelfPredictionManager.IsPredicting)
            {
                return;
            }

            base.OnTriggerEnter2D(other);
        }
    }
}
