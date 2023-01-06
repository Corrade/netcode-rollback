using System.Diagnostics;

using Lockstep;

namespace Lockstep
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
