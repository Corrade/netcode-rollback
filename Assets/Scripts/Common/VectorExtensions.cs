using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rollback
{
    static class VectorExtensions
    {
        // Returns the vector going right along the surface with the given normal
        public static Vector2 VectorAlongSurface(Vector2 normal)
        {
            return PerpendicularClockwise(normal).normalized;
        }

        // Returns the vector going down along the surface with the given normal
        public static Vector2 VectorDownSurface(Vector2 normal)
        {
            // Surface is like /
            if (normal.x <= 0)
            {
                return PerpendicularAntiClockwise(normal).normalized;
            }
            // Surface is like \
            else
            {
                return PerpendicularClockwise(normal).normalized;
            }
        }

        public static Vector2 PerpendicularAntiClockwise(Vector2 vec)
        {
            return new Vector2(-vec.y, vec.x);
        }

        public static Vector2 PerpendicularClockwise(Vector2 vec)
        {
            return new Vector2(vec.y, -vec.x);
        }
    }
}
