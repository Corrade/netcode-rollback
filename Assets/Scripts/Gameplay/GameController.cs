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

        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        ushort m_DebugRoundStartTick;
        int[] m_DebugTimesSimulatedOfficially = new int[TickService.MaxTick];
        #endif

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

            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_DebugRoundStartTick = roundStartTick;
            #endif
        }

        void StopRound()
        {
            m_IsFirstRound = false;
        }

        void ExecuteGameLoop(ushort currentTick)
        {
            DebugUI.WriteSequenced(DebugGroup.Core, "GameLoop() start", $"GameLoop() start: currentTick={currentTick}");

            // Bandaid workaround: ideally, rendering should be decoupled from simulation
            SetSpritesVisible(visible: false);
            m_GameLoop(currentTick);
            SetSpritesVisible(visible: true);

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
            // to SaveRollbackState()
            ushort t = RollbackManager.Rollback();

            DebugUI.WriteSequenced(DebugGroup.Core, "Rolled back", $"Rolled back: tick={t}");

            // t <= currentTick
            Assert.IsTrue(TickService.IsBeforeOrEqual(t, currentTick));

            DebugUI.WriteSequenced(DebugGroup.Core, "Official simulation start", $"Official simulation start: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");

            // Simulate while both players' inputs are present, starting from
            // and including t
            for (; PeerPlayer.HasInput(t) && TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                SelfPlayer.Simulate(t);
                PeerPlayer.Simulate(t);
                RunSimulation(isSimulatingOfficially: true, tick: t);
            }

            DebugUI.WriteSequenced(DebugGroup.Core, "Official simulation end", $"Official simulation end: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");

            AssertSimulatedOfficiallyExactlyOnceUpTo(tickExclusive: t);

            DebugUI.ShowGhost(DebugGroup.Core, "Self ghost", SelfPlayer.Position);
            DebugUI.ShowGhost(DebugGroup.Core, "Self kick collider", SelfPlayer.KickColliderPosition);
            DebugUI.ShowGhost(DebugGroup.Core, "Peer ghost", PeerPlayer.Position);
            DebugUI.ShowGhost(DebugGroup.Core, "Peer kick collider", PeerPlayer.KickColliderPosition);

            // t <= currentTick+1
            Assert.IsTrue(TickService.IsBeforeOrEqual(t, TickService.Add(currentTick, 1)));

            // By now, t represents the next tick that needs to be simulated
            RollbackManager.SaveRollbackState(t);

            // From the guard of the previous loop
            Assert.IsTrue(!PeerPlayer.HasInput(t) || TickService.IsAfter(t, currentTick));

            DebugUI.WriteSequenced(DebugGroup.Core, "Unofficial simulation start", $"Unofficial simulation start: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");

            // Finish the simulation if needed by performing prediction and
            // extrapolation
            for (; TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                Assert.IsTrue(!PeerPlayer.HasInput(t));

                SelfPlayer.Simulate(t);
                PeerPlayer.SimulateWithExtrapolation();
                RunSimulation(isSimulatingOfficially: false, tick: t);
            }

            DebugUI.WriteSequenced(DebugGroup.Core, "Unofficial simulation end", $"Unofficial simulation end: t={t}, self={SelfPlayer.Position}, peer={PeerPlayer.Position}");
        }

        void DebugSingleplayerGameLoop(ushort currentTick)
        {
            // TODO temporary logs
            Debug.Log("begin anim name=" + SelfPlayer.CurrentAnimationName);
            SelfPlayer.WriteInput(currentTick);

            // We still rollback to be able to test rollback-related
            // mechanics, e.g. animation rollback
            ushort t = RollbackManager.Rollback();

            // Never simulate up to more than 8 ticks behind the current tick
            // so that we force rollback to occur
            for (; TickService.IsBeforeOrEqual(t, TickService.Subtract(currentTick, 8)); t = TickService.Add(t, 1))
            {
                SelfPlayer.Simulate(t);
                RunSimulation(isSimulatingOfficially: true, tick: t);
            }

            Debug.Log("just before save rb state name=" + SelfPlayer.CurrentAnimationName);
            RollbackManager.SaveRollbackState(t);
            Debug.Log("just after save rb state name=" + SelfPlayer.CurrentAnimationName);

            for (; TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                SelfPlayer.Simulate(t);
                RunSimulation(isSimulatingOfficially: false, tick: t);
            }
            Debug.Log("end anim name=" + SelfPlayer.CurrentAnimationName);
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

            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (isSimulatingOfficially)
            {
                m_DebugTimesSimulatedOfficially[tick]++;
            }
            #endif
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
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            for (ushort t = m_DebugRoundStartTick; TickService.IsBefore(t, tickExclusive); t = TickService.Add(t, 1))
            {
                Assert.IsTrue(m_DebugTimesSimulatedOfficially[t] > 0);
                Assert.IsTrue(m_DebugTimesSimulatedOfficially[t] < 2);
            }
            #endif
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
