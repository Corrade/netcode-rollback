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
        ConnectionManager ConnectionManager;

        [SerializeField]
        SpawnManager SpawnManager;

        [SerializeField]
        public SelfPlayer SelfPlayer;

        [SerializeField]
        public PeerPlayer PeerPlayer;

        bool m_PeerPlayerMetadataReceived = false;

        void Awake()
        {
            Assert.IsTrue(ConnectionManager != null);
            Assert.IsTrue(SelfPlayer != null);
            Assert.IsTrue(PeerPlayer != null);

            // Physics.autoSimulation = false;
        }

        void Start()
        {
            // TODO yeah i had an instance where i started a thing in the editor first. then off-editor. off-editor receive metadata first. then only after a long wait the editor received the off-editor's metadata. i presume that the FIRST-OPENED instance may miss the second-opened instance's metadata being sent the first time
            // nah try again i had error pause on
            // Possible race condition if the peer's metadata message is sent too early, i.e. before this is called on the self client
            ConnectionManager.AddOnMessageReceived(OnMessageReceived);

            StartCoroutine(PreGame());
        }

        void OnDestroy()
        {
            Clock.Instance.TickUpdated -= OnTickUpdated;
        }

        IEnumerator PreGame()
        {
            SelfPlayer.Initialise(id: Settings.SelfPlayerId, name: Settings.SelfPlayerName, connectionManager: ConnectionManager);

            // Setup all connections
            yield return ConnectionManager.Setup();

            // Send self player metadata to peer
            using (Message msg = PlayerMetadataMsg.CreateMessage(Settings.SelfPlayerName))
            {
                ConnectionManager.SendMessage(msg, SendMode.Reliable);
            }

            // Receive peer player metadata
            yield return new WaitUntil(() => m_PeerPlayerMetadataReceived);

            StartGame();
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
            ushort simulationTick = TickService.SubtractTick(currentTick, Settings.InputDelayTicks);

            SelfPlayer.SendUnackedInputs(currentTick);

            if (!PeerPlayer.HasInput(simulationTick))
            {
                Clock.Instance.PauseIncrementing();
                return;
            }

            Clock.Instance.ResumeIncrementing();

            SelfPlayer.WriteInput(currentTick);

            SelfPlayer.Simulate(simulationTick);
            PeerPlayer.Simulate(simulationTick);
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
                PeerPlayer.Initialise(id: 1 - SelfPlayer.Id, name: msg.Name, connectionManager: ConnectionManager);
                m_PeerPlayerMetadataReceived = true;
            }
        }
    }
}
