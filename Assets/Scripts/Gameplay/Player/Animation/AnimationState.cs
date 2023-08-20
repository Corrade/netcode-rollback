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
        public string Name;
        public float NormalizedTime;

        public void Reset()
        {
            Name = "Idle";
            NormalizedTime = 0.0f;
        }

        public void LoadFrom(Animator animator)
        {
            AnimatorClipInfo[] currentClipInfo = animator.GetCurrentAnimatorClipInfo(0);
            AnimatorStateInfo currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Only one active clip at a time (no blending)
            Assert.IsTrue(currentClipInfo.Length == 1);
            AnimatorClipInfo currentClip = currentClipInfo[0];

            Name = currentClip.clip.name;
            NormalizedTime = currentStateInfo.normalizedTime;
        }
    }
}
