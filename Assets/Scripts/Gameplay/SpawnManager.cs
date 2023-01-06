using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DarkRift.Server;
using DarkRift.Server.Unity;

using Lockstep;

namespace Lockstep
{
    public class SpawnManager : MonoBehaviour
    {
        [Serializable]
        public struct Spawn
        {
            public Transform Point;
            public bool FaceLeft;

            public Vector2 Position => Point.position;
        }

        [SerializeField]
        Spawn[] PlayerSpawns = new Spawn[2]; // Indexed by player ID

        public void TeleportToSpawn(Player player)
        {
            player.Teleport(position: PlayerSpawns[player.Id].Position, faceLeft: PlayerSpawns[player.Id].FaceLeft);
        }
    }
}
