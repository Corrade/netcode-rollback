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

        bool m_IsInIntermission = false;
        bool m_PeerPlayerMetadataReceived = false;
        const float m_IntermissionDurationSec = 0.8f;

        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        int[] m_DebugSimulatedWithoutPrediction = new int[TickService.MaxTick];
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
        }

        void Start()
        {
            // Debug singleplayer
            if (false)
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
            Clock.Instance.TickUpdated -= GameLoop;
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
            RollbackManager.ResetForMatch();

            SelfPlayer.ResetForMatch();
            PeerPlayer.ResetForMatch();

            ResetForRound();

            Clock.Instance.Begin();
        }

        void ResetForRound()
        {
            SelfPlayer.ResetForRound();
            PeerPlayer.ResetForRound();

            SpawnManager.TeleportToSpawn(SelfPlayer);
            SpawnManager.TeleportToSpawn(PeerPlayer);

            RollbackManager.SaveRollbackState(
                Clock.Instance.Paused
                    ? TickService.Add(Clock.Instance.CurrentTick, 1)
                    : Clock.Instance.CurrentTick
            );

            Clock.Instance.TickUpdated += GameLoop;
            Clock.Instance.ResumeIncrementing();
        }

        void StopRound()
        {
            Clock.Instance.PauseIncrementing();
            Clock.Instance.TickUpdated -= GameLoop;
        }

        void GameLoop(ushort currentTick)
        {
            SetSpritesVisible(visible: false);

            SelfPlayer.WriteInput(currentTick);
            SelfPlayer.SendUnackedInputs(untilTickExclusive: TickService.Add(currentTick, 1));

            // Rollback to the gamestate and tick saved by the latest call
            // to SaveRollbackState()
            ushort t = RollbackManager.Rollback();

            // t <= currentTick
            Assert.IsTrue(TickService.IsBeforeOrEqual(t, currentTick));

            // Simulate while both players' inputs are present, starting from
            // and including t
            for (; PeerPlayer.HasInput(t) && TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                SelfPlayer.Simulate(t);
                PeerPlayer.Simulate(t);
                RunSimulation(isPredicting: false, tick: t);
            }

            AssertSimulatedWithoutPredictionExactlyOnceUpTo(tickExclusive: t);

            DebugUI.ShowGhost("selfghost", SelfPlayer.Position);
            DebugUI.ShowGhost("peerghost", PeerPlayer.Position);

            // t <= currentTick+1
            Assert.IsTrue(TickService.IsBeforeOrEqual(t, TickService.Add(currentTick, 1)));

            // By now, t represents the next tick that needs to be simulated
            RollbackManager.SaveRollbackState(t);

            // From the guard of the previous loop
            Assert.IsTrue(!PeerPlayer.HasInput(t) || TickService.IsAfter(t, currentTick));

            // Finish the simulation if needed by performing prediction and
            // extrapolation
            for (; TickService.IsBeforeOrEqual(t, currentTick); t = TickService.Add(t, 1))
            {
                Assert.IsTrue(!PeerPlayer.HasInput(t));

                SelfPlayer.Simulate(t);
                PeerPlayer.SimulateWithExtrapolation();
                RunSimulation(isPredicting: true, tick: t);
            }

            SetSpritesVisible(visible: true);
        }

        void SetSpritesVisible(bool visible)
        {
            SelfPlayer.SetSpriteVisible(visible);
            PeerPlayer.SetSpriteVisible(visible);
        }

        void RunSimulation(bool isPredicting, ushort tick)
        {
            SelfPlayer.IsPredicting = isPredicting;
            SimulationManager.Instance.Simulate(tick);

            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!isPredicting)
            {
                m_DebugSimulatedWithoutPrediction[tick]++;
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

            StopRound();

            if (matchIsOver)
            {
                MatchEnded?.Invoke();
            }
            else
            {
                RoundEnded?.Invoke();
            }

            yield return new WaitForSecondsRealtime(m_IntermissionDurationSec);

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

        // Assert that all ticks up to tickExclusive have been simulated
        // without prediction exactly once.
        // For the sake of simplicity and since this is only for debugging,
        // we assume that tickExclusive is after or equal to t (by TickService
        // standards).
        void AssertSimulatedWithoutPredictionExactlyOnceUpTo(ushort tickExclusive)
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            for (ushort t = 0; TickService.IsBefore(t, tickExclusive); t = TickService.Add(t, 1))
            {
                Assert.IsTrue(m_DebugSimulatedWithoutPrediction[t] == 1);
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
