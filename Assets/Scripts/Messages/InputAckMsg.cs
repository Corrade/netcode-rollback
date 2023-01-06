using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;

namespace Lockstep
{
    public class InputAckMsg : IDarkRiftSerializable
    {
        public ushort LatestTickReceived { get; private set; }

        public InputAckMsg() {}

        public InputAckMsg(ushort latestTickReceived)
        {
            LatestTickReceived = latestTickReceived;
        }

        public static Message CreateMessage(ushort latestTickReceived)
        {
            return Message.Create(
                Tags.InputAck,
                new InputAckMsg(latestTickReceived)
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            LatestTickReceived = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(LatestTickReceived);
        }
    }
}
