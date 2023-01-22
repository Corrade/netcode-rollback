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

        public event Action PauseChanged;
        public event Action<ushort> TickUpdated;

        public ushort CurrentTick { get; private set; }
        public bool Paused { get; private set; } = false;

        ushort m_PausedAtTick;
        bool m_JustUnpaused;

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
            ResumeIncrementing();
            m_ClockCoroutine = NextTick();
            StartCoroutine(m_ClockCoroutine);
        }

        public void PauseIncrementing()
        {
            if (Paused)
            {
                return;
            }

            Paused = true;
            m_PausedAtTick = CurrentTick;
            PauseChanged?.Invoke();
        }

        // Resumes at the tick after the tick that was paused at
        public void ResumeIncrementing()
        {
            if (!Paused)
            {
                return;
            }

            m_JustUnpaused = true;
            Paused = false;
            PauseChanged?.Invoke();
        }

        public void Stop()
        {
            StopCoroutine(m_ClockCoroutine);
        }

        IEnumerator NextTick()
        {
            while (true)
            {
                if (m_JustUnpaused)
                {
                    m_JustUnpaused = false;
                    CurrentTick = TickService.Add(m_PausedAtTick, 1);
                }

                // Keep this first so that the start tick is ran
                TickUpdated?.Invoke(CurrentTick);

                yield return new WaitForSecondsRealtime(TickService.TimeBetweenTicksSec);

                // We implement pausing this way instead of restarting the coroutine
                // in order to preserve the original cadence
                if (!Paused)
                {
                    IncrementCurrentTick();
                }
            }
        }

        public void IncrementCurrentTick()
        {
            CurrentTick = TickService.Add(CurrentTick, 1);
        }
    }
}
