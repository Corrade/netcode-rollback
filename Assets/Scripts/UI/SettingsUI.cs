using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Lockstep
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField SelfPlayerIdTextInput;

        [SerializeField]
        TMP_InputField SelfPlayerNameTextInput;

        [SerializeField]
        TMP_InputField SelfPortTextInput;

        [SerializeField]
        TMP_InputField PeerAddressTextInput;
        
        [SerializeField]
        TMP_InputField PeerPortTextInput;

        [SerializeField]
        TMP_InputField InputDelayTicksTextInput;

        [SerializeField]
        TMP_InputField ArtificialLatencyMsTextInput;

        [SerializeField]
        TMP_InputField ArtificialPacketLossPcTextInput;

        void Awake()
        {
            Assert.IsTrue(SelfPlayerIdTextInput != null);
            Assert.IsTrue(SelfPlayerNameTextInput != null);
            Assert.IsTrue(SelfPortTextInput != null);
            Assert.IsTrue(PeerAddressTextInput != null);
            Assert.IsTrue(PeerPortTextInput != null);
            Assert.IsTrue(InputDelayTicksTextInput != null);
            Assert.IsTrue(ArtificialLatencyMsTextInput != null);
            Assert.IsTrue(ArtificialPacketLossPcTextInput != null);
        }

        void Start()
        {
            LoadSettings();
        }

        void LoadSettings()
        {
            SelfPlayerIdTextInput.text = Settings.SelfPlayerId.ToString();
            SelfPlayerNameTextInput.text = Settings.SelfPlayerName;
            SelfPortTextInput.text = Settings.SelfPort.ToString();
            PeerAddressTextInput.text = Settings.PeerAddress;
            PeerPortTextInput.text = Settings.PeerPort.ToString();
            InputDelayTicksTextInput.text = Settings.InputDelayTicks.ToString();
            ArtificialLatencyMsTextInput.text = Settings.ArtificialLatencyMs.ToString();
            ArtificialPacketLossPcTextInput.text = Settings.ArtificialPacketLossPc.ToString();
        }

        void SaveSettings()
        {
            Settings.SelfPlayerId = int.Parse(SelfPlayerIdTextInput.text);
            Settings.SelfPlayerName = SelfPlayerNameTextInput.text;
            Settings.SelfPort = int.Parse(SelfPortTextInput.text);
            Settings.PeerAddress = PeerAddressTextInput.text;
            Settings.PeerPort = int.Parse(PeerPortTextInput.text);
            Settings.InputDelayTicks = int.Parse(InputDelayTicksTextInput.text);
            Settings.ArtificialLatencyMs = int.Parse(ArtificialLatencyMsTextInput.text);
            Settings.ArtificialPacketLossPc = float.Parse(ArtificialPacketLossPcTextInput.text);
        }

        public void OnPlayPressed()
        {
            SaveSettings();
            SceneManager.LoadScene((int)SceneIDs.Game);
        }
    }
}
