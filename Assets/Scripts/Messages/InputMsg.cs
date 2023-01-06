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
        public ushort NumTicks { get; private set; }
        public ushort EndTick => TickService.SubtractTick(TickService.AddTick(StartTick, NumTicks), 1);
        public ushort[] Inputs { get; private set; }

        public InputMsg() {}

        public InputMsg(ushort startTick, ushort numTicks, ushort[] inputs)
        {
            Assert.IsTrue(numTicks > 0);

            StartTick = startTick;
            NumTicks = numTicks;
            Inputs = inputs;
        }

        public static Message CreateMessage(ushort startTick, ushort numTicks, ushort[] inputs)
        {
            return Message.Create(
                Tags.Input,
                new InputMsg(startTick, numTicks, inputs)
            );
        }

        public void Deserialize(DeserializeEvent e)
        {
            StartTick = e.Reader.ReadUInt16();
            NumTicks = e.Reader.ReadUInt16();
            Inputs = e.Reader.ReadUInt16s();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(StartTick);
            e.Writer.Write(NumTicks);
            e.Writer.Write(Inputs);
        }
    }
}
