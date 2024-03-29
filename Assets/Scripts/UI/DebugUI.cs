using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/*
ZERO OVERHEAD AND ACHIEVING CONDITIONAL COMPILATION

(The following discussion is based on testing done in the editor. Behaviour
might be different/nicer with a standalone development or release build. Still,
adapting code for the editor is worthwhile because the editor is so
convenient.)

Profiling results find that this class, when enabled, is responsible for the
vast majority of the game loop's CPU footprint.

We should make sure we're not paying for anything here when we're not
debugging.

Ultimately, the solution is to left to the caller, who should wrap calls to
this class in a compile-time condition:

if (DebugFlags.IsDebugging) // where IsDebugging is a const variable*
    DebugUI.Func(...)

Because the conditional variable is const, the condition will be evaluated
at compile-time. If it's found to be false (debugging is disabled), the whole
block will be scrapped (citation needed) and we'll pay nothing during runtime.
In other words, the whole function call will be subject to conditional
compilation.

*The condition just has to be something that can be resolved at compile-time.
A simple const variable is nice because it'll signpost itself with an
"unreachable code" editor warning when the condition is false. Something a bit
more elaborate, like a function returning a const variable, will also work,
but it just won't produce the same warning!

Q: This is tedious and involves lots of code duplication. Why not move the
condition inside the functions here? In other words, return immediately /
no-op if we're debugging.
A: That wouldn't eliminate all overhead. Supposing that debugging is disabled
and the condition is false, the compiler is NOT smart enough to then erase
all calls to the resulting no-ops. The functions will still be called. Sure,
they'll just be no-ops so we'll avoid lots of work, but we'll still incur the
cost of constructing their arguments (and actually calling them). This retains
a major performance penalty as arguments to popular utilities like Write()
usually involve lots of data fetching and string formatting - expensive!
For zero overhead, we need to ensure these functions are never called, and
moving our conditions inside them can't accomplish that.

Q: Why not use Unity's #defines instead of a custom debugging flag?
A: For brevity and, more importantly, flexibility. Unity's related #defines are
coupled with launcher settings and the editor context.
*/

namespace Rollback
{
    public class DebugUI : MonoBehaviour
    {
        public static DebugUI Instance { get; private set; }

        [SerializeField]
        TMP_Text DebugDialogue;

        [SerializeField]
        GameObject DebugGhostPrefab;

        [SerializeField]
        GameObject DebugGhostPrefabAlternate;

        Dictionary<string, string> m_DebugText = new Dictionary<string, string>();
        Dictionary<string, GameObject> m_DebugGhosts = new Dictionary<string, GameObject>();

        int Sequence;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            Assert.IsTrue(DebugDialogue != null);

            if (!DebugFlags.IsDebugging)
                DebugDialogue.text = "";
        }

        void Update()
        {
            if (!DebugFlags.IsDebugging)
                return;

            DebugDialogue.text = String.Join("\n", m_DebugText.Values.ToList().OrderBy(s => s));
        }

        // Writes a string into the given channel, replacing everything in the
        // same channel.
        // Every frame, written strings from all channels are displayed in
        // alphabetical order of the strings.
        public static void Write(DebugGroup debugGroup, string channel, string value)
        {
            if (!DebugFlags.IsDebugging)
                return;

            if (!IsDebugGroupEnabled(debugGroup))
                return;

            Instance.m_DebugText[channel] = value;
            Instance.Update();
        }

        // As for Write(), but prepends a global sequence number in front of
        // the given string. Strings written this way will therefore be
        // ordered by sequence number when displayed.
        public static void WriteSequenced(DebugGroup debugGroup, string channel, string value)
        {
            if (!DebugFlags.IsDebugging)
                return;

            if (!IsDebugGroupEnabled(debugGroup))
                return;

            Write(debugGroup, channel, $"Seq={Instance.Sequence} {value}");
            Instance.Sequence++;
        }

        // Updates the position of the ghost in the given channel
        public static void ShowGhost(DebugGroup debugGroup, string channel, Vector2 position, bool useAlternateSprite = false)
        {

            if (Instance.m_DebugGhosts.ContainsKey(channel))
            {
                Instance.m_DebugGhosts[channel].transform.position = position;
                Instance.m_DebugGhosts[channel].SetActive(true);
            }
            else
            {
                Instance.m_DebugGhosts[channel] = Instantiate(
                    useAlternateSprite ? Instance.DebugGhostPrefabAlternate : Instance.DebugGhostPrefab,
                    position: position,
                    rotation: Quaternion.identity
                );
                Instance.m_DebugGhosts[channel].SetActive(true);
            }
        }

        // Hides the ghost in the given channel
        public static void HideGhost(string channel)
        {
            if (!DebugFlags.IsDebugging)
                return;

            if (Instance.m_DebugGhosts.ContainsKey(channel))
            {
                Instance.m_DebugGhosts[channel].SetActive(false);
            }
        }

        static bool IsDebugGroupEnabled(DebugGroup debugGroup)
        {
            return (DebugFlags.EnabledDebugGroups & debugGroup) != 0;
        }
    }
}
