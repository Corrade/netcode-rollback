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
        TMP_Text MatchEndDialogue;

        void Awake()
        {
            foreach (PlayerHud playerHud in PlayerHuds)
            {
                Assert.IsTrue(playerHud.Name != null);
                Assert.IsTrue(playerHud.Lives != null);
            }

            Assert.IsTrue(GameController != null);
            Assert.IsTrue(MatchEndDialogue != null);
        }

        void Start()
        {
            MatchEndDialogue.gameObject.SetActive(false);

            GameController.SelfPlayer.MetadataUpdated += OnMetadataUpdated;
            GameController.PeerPlayer.MetadataUpdated += OnMetadataUpdated;

            // The metadata may have updated beforehand
            OnMetadataUpdated(GameController.SelfPlayer.MetadataManager);
            OnMetadataUpdated(GameController.PeerPlayer.MetadataManager);

            GameController.MatchEnded += OnMatchEnded;
        }

        void OnMetadataUpdated(MetadataManager metadataManager)
        {
            PlayerHuds[metadataManager.Id].Name.text = metadataManager.Name;
            PlayerHuds[metadataManager.Id].Lives.text = $"{metadataManager.Lives}/{MetadataManager.MaxLives} Lives";
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
    }
}
