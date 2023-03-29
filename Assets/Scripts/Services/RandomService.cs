using System;
using System.Diagnostics;
using UnityEngine.Assertions;

using Rollback;

namespace Rollback
{
    public static class RandomService
    {
        static Random m_Rand;

        static RandomService()
        {
            m_Rand = new Random();
        }

        // [x, y)
        public static int GetRandomBetween(int x, int y)
        {
            return m_Rand.Next(x, y);
        }

        public static bool ReturnTrueWithProbability(float p)
        {
            Assert.IsTrue(p >= 0f && p <= 1f);
            return m_Rand.NextDouble() < p;
        }
    }
}
