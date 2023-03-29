using System.Diagnostics;

using Rollback;

namespace Rollback
{
    public static class TimeService
    {
        static Stopwatch m_Stopwatch;

        static TimeService()
        {
            m_Stopwatch = new Stopwatch();
            m_Stopwatch.Start();
        }

        public static long GetElapsedTime()
        {
            return m_Stopwatch.ElapsedMilliseconds;
        }
    }
}
