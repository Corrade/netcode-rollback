using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;

using Lockstep;

namespace Lockstep
{
    public class Clock : MonoBehaviour
    {
        public static Clock Instance { get; private set; }

        public ushort CurrentTick { get; private set; }
        public bool Paused { get; private set; } = false;
        public event Action<ushort> TickUpdated;

        IEnumerator m_ClockCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
        }

        // Can be used to restart the clock as well
        public void Begin()
        {
            if (m_ClockCoroutine != null)
            {
                Stop();
            }

            CurrentTick = TickService.StartTick;
            Paused = false;
            m_ClockCoroutine = NextTick();
            StartCoroutine(m_ClockCoroutine);
        }

        public void PauseIncrementing()
        {
            Paused = true;
        }

        public void ResumeIncrementing()
        {
            Paused = false;
        }

        public void Stop()
        {
            StopCoroutine(m_ClockCoroutine);
        }

        IEnumerator NextTick()
        {
            while (true)
            {
                // Keep this first so that the start tick is ran
                TickUpdated?.Invoke(CurrentTick);

                yield return new WaitForSecondsRealtime(TickService.TimeBetweenTicksSec);

                // We implement pausing this way instead of restarting the coroutine
                // in order to preserve the original cadence
                if (!Paused)
                {
                    SetCurrentTick(TickService.Add(CurrentTick, 1));
                }
            }
        }

        public void SetCurrentTick(ushort tick)
        {
            CurrentTick = tick;
        }
    }
}
