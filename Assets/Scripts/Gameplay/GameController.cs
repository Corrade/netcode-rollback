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

        bool m_PeerPlayerMetadataReceived = false;

        void Awake()
        {
            Assert.IsTrue(SelfPlayer != null);
            Assert.IsTrue(PeerPlayer != null);

            // Physics.autoSimulation = false;
        }

        void Start()
        {
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);

            StartCoroutine(PreGame());
        }

        void OnDestroy()
        {
            Clock.Instance.TickUpdated -= OnTickUpdated;
        }

        IEnumerator PreGame()
        {
            SelfPlayer.Initialise(id: Settings.SelfPlayerId, name: Settings.SelfPlayerName);

            // Setup all connections
            yield return ConnectionManager.Instance.Setup();

            // Send client player metadata to peer and wait for theirs
            yield return PlayerMetadataSync();

            // Send self player metadata to peer
            StartGame();
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

        void StartGame()
        {
            SpawnManager.TeleportToSpawn(SelfPlayer);
            SpawnManager.TeleportToSpawn(PeerPlayer);
            SelfPlayer.ResetLives();
            PeerPlayer.ResetLives();

            Clock.Instance.TickUpdated += OnTickUpdated;
            Clock.Instance.Begin();
        }

        void OnTickUpdated(ushort currentTick)
        {
            ushort simulationTick = TickService.Subtract(currentTick, Settings.InputDelayTicks);

            SelfPlayer.SendUnackedInputs(untilTickExclusive: currentTick);

            if (!PeerPlayer.HasInput(simulationTick))
            {
                // Debug.Log($"tickupdated => peer player doesn't hvae input for sim={simulationTick}");
                Clock.Instance.PauseIncrementing();
                return;
            }

            Clock.Instance.ResumeIncrementing();

            SelfPlayer.WriteInput(currentTick);

            // Debug.LogError($"trying to simulate {simulationTick} for the self");
            SelfPlayer.Simulate(simulationTick);
            // Debug.LogError($"trying to simulate {simulationTick} for the peer");
            PeerPlayer.Simulate(simulationTick);

            SelfPlayer.DisposeInputs(simulationTick);
            PeerPlayer.DisposeInputs(simulationTick);
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
