using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;

namespace Lockstep
{
    public class PingAckMsg : IDarkRiftSerializable
    {
        public ushort ReceivedTick { get; private set; }

        public PingAckMsg() {}

        public PingAckMsg(ushort receivedTick)
        {
            ReceivedTick = receivedTick;
        }

        public static Message CreateMessage(ushort receivedTick)
        {
            return Message.Create(
                Tags.PingAck,
                new PingAckMsg(receivedTick)
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            ReceivedTick = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(ReceivedTick);
        }
    }
}
