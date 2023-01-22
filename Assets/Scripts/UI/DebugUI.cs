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

        Dictionary<string, string> m_DebugText = new Dictionary<string, string>();

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

        // Writes a string into a given channel, replacing everything in the
        // same channel
        public static void Write(string channel, string value)
        {
            Instance.m_DebugText[channel] = value;
        }
    }
}
