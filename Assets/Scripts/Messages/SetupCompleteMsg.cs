using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DarkRift;

namespace Lockstep
{
    public class SetupCompleteMsg : IDarkRiftSerializable
    {
        public SetupCompleteMsg() {}

        public static Message CreateMessage()
        {
            return Message.Create(
                Tags.SetupComplete,
                new SetupCompleteMsg()
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            ;
        }

        public void Serialize(SerializeEvent e)
        {
            ;
        }
    }
}
