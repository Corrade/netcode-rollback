using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        public event Action<ushort> SimulationProgressed;

        public ushort LatestSimulationTick { get; private set; }
        public ushort LatestOfficialSimulationTick { get; private set; }
        public ushort LatestUnofficialSimulationTick { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
        }

        public void ProgressSimulation(bool isSimulatingOfficially, ushort tick)
        {
            LatestSimulationTick = tick;

            if (isSimulatingOfficially)
            {
                LatestOfficialSimulationTick = tick;
            }
            else
            {
                LatestUnofficialSimulationTick = tick;
            }

            Physics2D.Simulate(TickService.TimeBetweenTicksSec);

            SimulationProgressed?.Invoke(tick);
        }
    }
}
