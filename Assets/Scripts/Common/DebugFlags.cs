using System;
using System.Collections;
using System.Collections.Generic;

namespace Rollback
{
    static class DebugFlags
    {
        public const bool IsDebuggingSingleplayer = false;
        public const DebugGroup EnabledDebugGroups = DebugGroup.Core | DebugGroup.Animation;
    }
}
