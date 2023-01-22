using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lockstep
{
    static class Settings
    {
        public static int SelfPlayerId = 0;

        public static string SelfPlayerName = "placeholder123";

        public static int SelfPort = 9000;

        // Warning: "localhost" doesn't always work whereas 127.0.0.1 is consistent
        public static string PeerAddress = "127.0.0.1";

        public static int PeerPort = 9001;

        public static int InputDelayTicks = 6;

        public static int ArtificialLatencyMs = 25;

        public static float ArtificialPacketLossPc = 0.05f;

        public static Dictionary<ushort, KeyCode> Binding => Bindings[SelfPlayerId];

        public static Dictionary<ushort, KeyCode>[] Bindings = new Dictionary<ushort, KeyCode>[2]{
            new Dictionary<ushort, KeyCode>{
                { InputMasks.MoveLeft, KeyCode.A },
                { InputMasks.MoveRight, KeyCode.D },
                { InputMasks.Dive, KeyCode.W },
                { InputMasks.Kick, KeyCode.S }
            },
            new Dictionary<ushort, KeyCode>{
                { InputMasks.MoveLeft, KeyCode.LeftArrow },
                { InputMasks.MoveRight, KeyCode.RightArrow },
                { InputMasks.Dive, KeyCode.UpArrow },
                { InputMasks.Kick, KeyCode.DownArrow }
            }
        };
    }
}
