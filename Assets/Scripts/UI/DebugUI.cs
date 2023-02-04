using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Lockstep
{
    public class DebugUI : MonoBehaviour
    {
        public static DebugUI Instance { get; private set; }

        [SerializeField]
        TMP_Text DebugDialogue;

        [SerializeField]
        GameObject DebugGhostPrefab;

        Dictionary<string, string> m_DebugText = new Dictionary<string, string>();
        Dictionary<string, GameObject> m_DebugGhosts = new Dictionary<string, GameObject>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            Assert.IsTrue(DebugDialogue != null);
        }

        void Update()
        {
            DebugDialogue.text = String.Join("\n", m_DebugText.Values.ToList().ToArray());
        }

        // Writes a string into the given channel, replacing everything in the
        // same channel
        public static void Write(string channel, string value)
        {
            Instance.m_DebugText[channel] = value;
        }

        // Updates the position of the ghost in the given channel
        public static void ShowGhost(string channel, Vector2 position)
        {
            if (Instance.m_DebugGhosts.ContainsKey(channel))
            {
                Instance.m_DebugGhosts[channel].transform.position = position;
                Instance.m_DebugGhosts[channel].SetActive(true);
            }
            else
            {
                Instance.m_DebugGhosts[channel] = Instantiate(Instance.DebugGhostPrefab, position: position, rotation: Quaternion.identity);
                Instance.m_DebugGhosts[channel].SetActive(true);
            }
        }

        // Hides the ghost in the given channel
        public static void HideGhost(string channel)
        {
            if (Instance.m_DebugGhosts.ContainsKey(channel))
            {
                Instance.m_DebugGhosts[channel].SetActive(false);
            }
        }
    }
}
