using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Lockstep;

/*
Attach this class to any children of a player that need back-references to 
their player parent.

The player runs initialise on all of its child player reference components.
*/

namespace Lockstep
{
    public class PlayerReference : MonoBehaviour
    {
        public Player Player;

        public void Initialise(Player player)
        {
            Player = player;
        }
    }
}
