using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;

namespace Lockstep
{
    public class InputMsg : IDarkRiftSerializable
    {
        public ushort StartTick { get; private set; }
        public ushort[] Inputs { get; private set; }

        public int NumTicks => Inputs.Length;
        public ushort EndTickExclusive => TickService.Add(StartTick, NumTicks);

        public InputMsg() {}

        public InputMsg(ushort startTick, ushort[] inputs)
        {
            Assert.IsTrue(inputs.Length > 0);
            StartTick = startTick;
            Inputs = inputs;
        }

        public static Message CreateMessage(ushort startTick, ushort[] inputs)
        {
            return Message.Create(
                Tags.Input,
                new InputMsg(startTick, inputs)
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            StartTick = e.Reader.ReadUInt16();
            Inputs = e.Reader.ReadUInt16s();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(StartTick);
            e.Writer.Write(Inputs);
        }
    }
}
