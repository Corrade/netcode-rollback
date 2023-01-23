using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Lockstep
{
    public class HudUI : MonoBehaviour
    {
        [Serializable]
        public struct PlayerHud
        {
            public TMP_Text Name;
            public TMP_Text Lives;
        }

        [SerializeField]
        GameController GameController;

        [SerializeField]
        PlayerHud[] PlayerHuds = new PlayerHud[2]; // Indexed by player ID

        [SerializeField]
        TMP_Text RoundEndDialogue;

        [SerializeField]
        TMP_Text MatchEndDialogue;

        void Awake()
        {
            foreach (PlayerHud playerHud in PlayerHuds)
            {
                Assert.IsTrue(playerHud.Name != null);
                Assert.IsTrue(playerHud.Lives != null);
            }

            Assert.IsTrue(GameController != null);
            Assert.IsTrue(RoundEndDialogue != null);
            Assert.IsTrue(MatchEndDialogue != null);
        }

        void Start()
        {
            RoundEndDialogue.gameObject.SetActive(false);
            MatchEndDialogue.gameObject.SetActive(false);

            GameController.SelfPlayer.MetadataUpdated += OnMetadataUpdated;
            GameController.PeerPlayer.MetadataUpdated += OnMetadataUpdated;

            // The metadata may have updated beforehand
            OnMetadataUpdated(GameController.SelfPlayer.MetadataManager);
            OnMetadataUpdated(GameController.PeerPlayer.MetadataManager);

            GameController.SelfPlayer.LifeLost += OnLifeLost;
            GameController.PeerPlayer.LifeLost += OnLifeLost;

            GameController.RoundStarted += OnRoundStarted;
            GameController.MatchEnded += OnMatchEnded;

            Clock.Instance.PauseChanged += OnPauseChanged;

            DebugUI.Write("netcode", $"Input delay = {Settings.InputDelayTicks} ticks\nArtificial latency = {Settings.ArtificialLatencyMs} ms\nArtificial packet loss = {100 * Settings.ArtificialPacketLossPc}%");
        }

        void OnMetadataUpdated(MetadataManager metadataManager)
        {
            PlayerHuds[metadataManager.Id].Name.text = metadataManager.Name;
            PlayerHuds[metadataManager.Id].Lives.text = $"{metadataManager.Lives}/{MetadataManager.MaxLives} Lives";
        }

        void OnLifeLost(MetadataManager metadataManager)
        {
            if (metadataManager.IsDefeated)
            {
                return;
            }

            RoundEndDialogue.gameObject.SetActive(true);

            if (metadataManager.Id == GameController.SelfPlayer.Id)
            {
                RoundEndDialogue.text = "-1";
            }
            else
            {
                RoundEndDialogue.text = "Hit!";
            }
        }

        void OnRoundStarted()
        {
            RoundEndDialogue.gameObject.SetActive(false);
        }

        void OnMatchEnded()
        {
            MatchEndDialogue.gameObject.SetActive(true);

            if (GameController.SelfPlayer.IsDefeated)
            {
                MatchEndDialogue.text = "Defeat";
            }
            else
            {
                MatchEndDialogue.text = "Victory!";
            }
        }

        void OnPauseChanged()
        {
            DebugUI.Write("pause", Clock.Instance.Paused ? "Paused" : "Running");
        }
    }
}
