using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;

namespace Rollback
{
    public class PingMsg : IDarkRiftSerializable
    {
        public ushort CurrentTick { get; private set; }

        public PingMsg() {}

        public PingMsg(ushort currentTick)
        {
            CurrentTick = currentTick;
        }

        public static Message CreateMessage(ushort currentTick)
        {
            return Message.Create(
                Tags.Ping,
                new PingMsg(currentTick)
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            CurrentTick = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(CurrentTick);
        }
    }
}
