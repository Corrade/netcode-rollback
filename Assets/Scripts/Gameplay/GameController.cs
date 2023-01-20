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
        public SelfPlayer SelfPlayer;

        [SerializeField]
        public PeerPlayer PeerPlayer;

        public event Action MatchStarted;
        public event Action MatchEnded;
        public event Action RoundStarted;
        public event Action RoundEnded;

        bool m_PeerPlayerMetadataReceived = false;
        const float m_IntermissionDurationSec = 0.8f;

        void Awake()
        {
            Assert.IsTrue(SelfPlayer != null);
            Assert.IsTrue(PeerPlayer != null);

            // Physics.autoSimulation = false;
        }

        void Start()
        {
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);

            SelfPlayer.LifeLost += OnLifeLost;
            PeerPlayer.LifeLost += OnLifeLost;

            /*
            // Debug
            {
                SelfPlayer.Initialise(id: Settings.SelfPlayerId, name: "self");
                PeerPlayer.Initialise(id: 1 - SelfPlayer.Id, name: "peer");
                StartMatch();
                return;
            }
            */

            StartCoroutine(PreGame());
        }

        void OnDestroy()
        {
            Clock.Instance.TickUpdated -= GameLoop;
        }

        IEnumerator PreGame()
        {
            SelfPlayer.Initialise(id: Settings.SelfPlayerId, name: Settings.SelfPlayerName);

            // Setup all connections
            yield return ConnectionManager.Instance.Setup();

            // Send client player metadata to peer and wait for theirs
            yield return PlayerMetadataSync();

            StartMatch();
        }

        IEnumerator PlayerMetadataSync()
        {
            SendPlayerMetadata();

            yield return new WaitUntil(() => m_PeerPlayerMetadataReceived);
        }

        void SendPlayerMetadata()
        {
            using (Message msg = PlayerMetadataMsg.CreateMessage(Settings.SelfPlayerName))
            {
                ConnectionManager.Instance.SendMessage(msg, SendMode.Reliable);
            }
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

            Clock.Instance.TickUpdated += GameLoop;
            Clock.Instance.ResumeIncrementing();
        }

        void StopRound()
        {
            Clock.Instance.TickUpdated -= GameLoop;
            Clock.Instance.PauseIncrementing();
        }

        void GameLoop(ushort currentTick)
        { 
            /*
            // Debug
            {
                ushort s = TickService.Subtract(currentTick, Settings.InputDelayTicks);
                SelfPlayer.WriteInput(currentTick);
                SelfPlayer.Simulate(s);
                return;
            }
            */

            ushort simulationTick = TickService.Subtract(currentTick, Settings.InputDelayTicks);

            SelfPlayer.SendUnackedInputs(untilTickExclusive: currentTick);

            if (!PeerPlayer.HasInput(simulationTick))
            {
                Clock.Instance.PauseIncrementing();
                return;
            }

            Clock.Instance.ResumeIncrementing();

            SelfPlayer.WriteInput(currentTick);

            SelfPlayer.Simulate(simulationTick);
            PeerPlayer.Simulate(simulationTick);

            SelfPlayer.DisposeInputs(tickJustSimulated: simulationTick);
            PeerPlayer.DisposeInputs(tickJustSimulated: simulationTick);
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            StartCoroutine(Intermission(matchIsOver: (metadataManager.IsDefeated)));
        }

        IEnumerator Intermission(bool matchIsOver)
        {
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
    }
}
