using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Rollback;

namespace Rollback
{
    public class GameController : MonoBehaviour
    {
        [SerializeField]
        SpawnManager SpawnManager;

        [SerializeField]
        RollbackManager RollbackManager;

        [SerializeField]
        public SelfPlayer SelfPlayer;

        [SerializeField]
        public PeerPlayer PeerPlayer;

        public event Action MatchStarted;
        public event Action MatchEnded;
        public event Action RoundStarted;
        public event Action RoundEnded;

        bool m_IsFirstRound;
        bool m_IsInIntermission;
        ushort m_IntermissionStartTick;
        ushort m_IntermissionFinishTick;
        bool m_PeerPlayerMetadataReceived = false;
        Action<ushort> m_GameLoop;
        const float m_IntermissionDurationSec = 0.8f;

        ushort m_DebugRoundStartTick;
        int[] m_DebugTimesSimulatedOfficially = new int[TickService.MaxTick];

        void Awake()
        {
            Assert.IsTrue(SpawnManager != null);
            Assert.IsTrue(RollbackManager != null);
            Assert.IsTrue(SelfPlayer != null);
            Assert.IsTrue(PeerPlayer != null);

            // Progress physics only when Physics2D.Simulate() is called, as
            // opposed to automatically in FixedUpdate()
            Physics2D.simulationMode = SimulationMode2D.Script;

            m_GameLoop = DebugFlags.IsDebuggingSingleplayer ? DebugSingleplayerGameLoop : GameLoop;
        }

        void Start()
        {
            // Debug singleplayer
            if (DebugFlags.IsDebuggingSingleplayer)
            {
                SelfPlayer.LifeLost += OnLifeLost;
                PeerPlayer.LifeLost += OnLifeLost;
                SelfPlayer.Initialise(id: Settings.SelfPlayerId, name: "self");
                PeerPlayer.Initialise(id: 1 - SelfPlayer.Id, name: "peer");
                StartMatch();
                return;
            }

            ConnectionManager.Instance.SetupComplete += OnConnectionSetupComplete;
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);

            SelfPlayer.LifeLost += OnLifeLost;
            PeerPlayer.LifeLost += OnLifeLost;

            SelfPlayer.Initialise(id: Settings.SelfPlayerId, name: Settings.SelfPlayerName);
        }

        void OnDestroy()
        {
            Clock.Instance.TickUpdated -= ExecuteGameLoop;
        }

        void OnConnectionSetupComplete()
        {
            StartCoroutine(PreGame());
        }

        IEnumerator PreGame()
        {
            SendPlayerMetadata();

            yield return new WaitUntil(() => m_PeerPlayerMetadataReceived);

            StartMatch();
        }

        void StartMatch()
        {
            ResetForMatch();
            MatchStarted?.Invoke();
            RoundStarted?.Invoke();
        }

        void StartRound()
        {
            ResetForRound();
            RoundStarted?.Invoke();
        }

        void ResetForMatch()
        {
            m_IsFirstRound = true;
            m_IsInIntermission = false;
            m_IntermissionStartTick = 0;
            m_IntermissionFinishTick = 0;

            RollbackManager.ResetForMatch();

            ResetForRound();

            Clock.Instance.TickUpdated += ExecuteGameLoop;
            Clock.Instance.Begin();
        }

        void ResetForRound()
        {
            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "ResetForRound()", $"ResetForRound(): Clock.Instance.CurrentTick={Clock.Instance.CurrentTick}");

            // This value must be the same for all players because it determines
            // the point at which the teleports are applied
            ushort roundStartTick = m_IsFirstRound
                ? TickService.StartTick
                : m_IntermissionFinishTick;

            if (!m_IsFirstRound)
            {
                SelfPlayer.ResetForNonFirstRound(roundStartTick);
                PeerPlayer.ResetForNonFirstRound(roundStartTick);
            }

            SpawnManager.TeleportToSpawn(SelfPlayer);
            SpawnManager.TeleportToSpawn(PeerPlayer);

            RollbackManager.SaveRollbackState(roundStartTick);

            if (DebugFlags.IsDebugging)
            {
                m_DebugRoundStartTick = roundStartTick;
            }
        }

        void StopRound()
        {
            m_IsFirstRound = false;
        }

        void ExecuteGameLoop(ushort currentTick)
        {
            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "GameLoop() start", $"GameLoop() start: currentTick={currentTick}");

            RunPreGameLoop();
            m_GameLoop(currentTick);
            RunPostGameLoop();

            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "GameLoop() end", $"GameLoop() end");
        }

        void GameLoop(ushort currentTick)
        {
            if (m_IsInIntermission)
            {
                // Ensure the peer has enough input to get to intermission
                SelfPlayer.SendUnackedInputs(untilTickExclusive: TickService.Add(m_IntermissionStartTick, 1));
                return;
            }

            SelfPlayer.WriteInput(currentTick);
            SelfPlayer.SendUnackedInputs(untilTickExclusive: TickService.Add(currentTick, 1));

            // Rollback to the gamestate and tick saved by the latest call
            // to SaveRollbackState().
            // In a client-server scheme, this tick would be the latest
            // tick received from the server, i.e. the latest authoritative
            // tick. In our peer-to-peer setup, authority is democratised. The
            // latest authoritative tick is the latest one for which we have
            // all players' information, i.e. consensus.
            // Here, specifically, we end up rolling back to the latest tick
            // for which we had both players' inputs for *in the previous game loop*.
            ushort t = RollbackManager.Rollback();

            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "Rolled back", $"Rolled back: tick={t}");

            // t <= currentTick
            Assert.IsTrue(TickService.IsBeforeOrEqual(t, currentTick));

            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "Official simulation start", $"Official simulation start: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");

            // Simulate both players for as many ticks as we have both players'
            // inputs for *now*, starting from and including t
            for (; PeerPlayer.HasInput(t) && TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                SelfPlayer.Simulate(t);
                PeerPlayer.Simulate(t);
                // Actually run the simulation using Physics2D.Simulate()
                RunSimulation(isSimulatingOfficially: true, tick: t);
            }

            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "Official simulation end", $"Official simulation end: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");

            AssertSimulatedOfficiallyExactlyOnceUpTo(tickExclusive: t);

            if (DebugFlags.IsDebugging)
            {
                DebugUI.ShowGhost(DebugGroup.Core, "Self ghost", SelfPlayer.Position);
                DebugUI.ShowGhost(DebugGroup.Core, "Self kick collider", SelfPlayer.KickColliderPosition, useAlternateSprite: true);
                DebugUI.ShowGhost(DebugGroup.Core, "Peer ghost", PeerPlayer.Position);
                DebugUI.ShowGhost(DebugGroup.Core, "Peer kick collider", PeerPlayer.KickColliderPosition, useAlternateSprite: true);
            }

            // t <= currentTick+1
            Assert.IsTrue(TickService.IsBeforeOrEqual(t, TickService.Add(currentTick, 1)));

            // By now, t represents the next tick that needs to be simulated
            RollbackManager.SaveRollbackState(t);

            // From the guard of the previous loop
            Assert.IsTrue(!PeerPlayer.HasInput(t) || TickService.IsAfter(t, currentTick));

            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "Unofficial simulation start", $"Unofficial simulation start: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");

            // Finish the simulation if needed by performing prediction and
            // extrapolation
            for (; TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                Assert.IsTrue(!PeerPlayer.HasInput(t));

                // "Client"-side prediction
                SelfPlayer.Simulate(t);
                // Entity extrapolation, aka. dead reckoning
                PeerPlayer.SimulateWithExtrapolation();
                // We ignore collisions when isSimulatingOfficially is false
                RunSimulation(isSimulatingOfficially: false, tick: t);
            }

            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "Unofficial simulation end", $"Unofficial simulation end: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");
        }

        void DebugSingleplayerGameLoop(ushort currentTick)
        {
            SelfPlayer.WriteInput(currentTick);

            // We still rollback to be able to test rollback-related
            // mechanics, e.g. animation rollback
            ushort t = RollbackManager.Rollback();

            // Force rollback to occur by never simulating up to more than
            // 8 ticks (arbitrary magic number) behind the current tick
            for (; TickService.IsBeforeOrEqual(t, TickService.Subtract(currentTick, 8)); t = TickService.Add(t, 1))
            {
                SelfPlayer.Simulate(t);
                RunSimulation(isSimulatingOfficially: true, tick: t);
            }

            RollbackManager.SaveRollbackState(t);

            for (; TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                SelfPlayer.Simulate(t);
                RunSimulation(isSimulatingOfficially: false, tick: t);
            }
        }

        void RunPreGameLoop()
        {
            SetSpritesVisible(false);
        }

        void RunPostGameLoop()
        {
            SetSpritesVisible(true);

            // As per common practice, we run rendering-related functions once
            // after the game loop instead of during every tick's simulation.
            SelfPlayer.RenderAnimation();
            PeerPlayer.RenderAnimation();
        }

        void SetSpritesVisible(bool visible)
        {
            SelfPlayer.SetSpriteVisible(visible);
            PeerPlayer.SetSpriteVisible(visible);
        }

        void RunSimulation(bool isSimulatingOfficially, ushort tick)
        {
            SelfPlayer.IsSimulatingOfficially = isSimulatingOfficially;
            PeerPlayer.IsSimulatingOfficially = isSimulatingOfficially;

            SimulationManager.Instance.ProgressSimulation(isSimulatingOfficially, tick);

            if (DebugFlags.IsDebugging && isSimulatingOfficially)
            {
                m_DebugTimesSimulatedOfficially[tick]++;
            }
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            StartCoroutine(Intermission(matchIsOver: (metadataManager.IsDefeated)));
        }

        IEnumerator Intermission(bool matchIsOver)
        {
            if (m_IsInIntermission)
            {
                yield break;
            }

            m_IsInIntermission = true;

            // Intermission can only be triggered from an official simulation
            Assert.IsTrue(SimulationManager.Instance.LatestOfficialSimulationTick == SimulationManager.Instance.LatestSimulationTick);

            m_IntermissionStartTick = SimulationManager.Instance.LatestOfficialSimulationTick;
            m_IntermissionFinishTick = TickService.Add(m_IntermissionStartTick, TickService.SecondsToTicks(m_IntermissionDurationSec));

            if (DebugFlags.IsDebugging)
                DebugUI.WriteSequenced(DebugGroup.Core, "Intermission()", $"Intermission(): m_IntermissionStartTick={m_IntermissionStartTick}, m_IntermissionFinishTick={m_IntermissionFinishTick}");

            StopRound();

            if (matchIsOver)
            {
                MatchEnded?.Invoke();
            }
            else
            {
                RoundEnded?.Invoke();
            }

            yield return new WaitUntil(() => TickService.IsAfterOrEqual(Clock.Instance.CurrentTick, m_IntermissionFinishTick));

            if (matchIsOver)
            {
                Debug.Log("Match over");
            }
            else
            {
                StartRound();
            }

            m_IsInIntermission = false;
        }

        // Assert that all ticks [m_DebugRoundStartTick, tickExclusive) have
        // been simulated under confirmation exactly once.
        // For the sake of simplicity and since this is only for debugging,
        // we assume that tickExclusive is after or equal to t (by TickService
        // standards).
        void AssertSimulatedOfficiallyExactlyOnceUpTo(ushort tickExclusive)
        {
            if (!DebugFlags.IsDebugging)
                return;

            for (ushort t = m_DebugRoundStartTick; TickService.IsBefore(t, tickExclusive); t = TickService.Add(t, 1))
            {
                Assert.IsTrue(m_DebugTimesSimulatedOfficially[t] > 0);
                Assert.IsTrue(m_DebugTimesSimulatedOfficially[t] < 2);
            }
        }

        void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.PlayerMetadata)
                {
                    HandlePlayerMetadataMsg(sender, e);
                }
            }
        }

        void HandlePlayerMetadataMsg(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                PlayerMetadataMsg msg = message.Deserialize<PlayerMetadataMsg>();
                PeerPlayer.Initialise(id: 1 - SelfPlayer.Id, name: msg.Name);
                m_PeerPlayerMetadataReceived = true;
            }
        }

        void SendPlayerMetadata()
        {
            ConnectionManager.Instance.SendMessage(() => PlayerMetadataMsg.CreateMessage(Settings.SelfPlayerName), SendMode.Reliable);
        }
    }
}
