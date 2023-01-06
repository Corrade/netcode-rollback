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

        void Start()
        {
            GameController.SelfPlayer.MetadataUpdated += OnMetadataUpdated;
            GameController.PeerPlayer.MetadataUpdated += OnMetadataUpdated;
        }

        void OnMetadataUpdated(MetadataManager metadataManager)
        {
            PlayerHuds[metadataManager.Id].Name.text = metadataManager.Name;
            PlayerHuds[metadataManager.Id].Lives.text = $"{metadataManager.Lives}/{MetadataManager.MaxLives} Lives";
        }
    }
}
