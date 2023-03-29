using System;
using System.Collections;
using System.Collections.Generic;

namespace Rollback
{
    static class MathExtensions
    {
        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
