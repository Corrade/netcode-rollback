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
        public ushort ReceivedUntilTickExclusive { get; private set; }

        public InputAckMsg() {}

        public InputAckMsg(ushort receivedUntilTickExclusive)
        {
            ReceivedUntilTickExclusive = receivedUntilTickExclusive;
        }

        public static Message CreateMessage(ushort receivedUntilTickExclusive)
        {
            return Message.Create(
                Tags.InputAck,
                new InputAckMsg(receivedUntilTickExclusive)
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            ReceivedUntilTickExclusive = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(ReceivedUntilTickExclusive);
        }
    }
}
