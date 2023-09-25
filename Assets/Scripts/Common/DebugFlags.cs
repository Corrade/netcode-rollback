using System;
using System.Collections;
using System.Collections.Generic;

namespace Rollback
{
    static class DebugFlags
    {
        public const bool IsDebugging = m_IsDebugging;
        public const bool IsDebuggingSingleplayer = IsDebugging && m_IsDebuggingSingleplayer;
        public const DebugGroup EnabledDebugGroups = IsDebugging ? m_EnabledDebugGroups : new DebugGroup();

        const bool m_IsDebugging = false;
        const bool m_IsDebuggingSingleplayer = false;
        const DebugGroup m_EnabledDebugGroups = DebugGroup.Core | DebugGroup.Animation | DebugGroup.Networking;
    }
}
