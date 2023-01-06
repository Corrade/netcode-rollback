using System;
using System.Collections;
using System.Collections.Generic;

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
    }
}
