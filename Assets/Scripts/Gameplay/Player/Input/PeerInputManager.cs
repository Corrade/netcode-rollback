using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Lockstep;

namespace Lockstep
{
    public class PeerInputManager : InputManager
    {
        public override void Initialise()
        {
            ConnectionManager.Instance.RemoveOnMessageReceived(OnMessageReceived);
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);

            ushort tickBeforeStartTick = TickService.Subtract(TickService.StartTick, 1);

            m_InputBuffer.Initialise(
                // Start with blank input in the buffer to enable immediate extrapolation
                startInclusive: TickService.Subtract(TickService.StartTick, 5),
                endExclusive: tickBeforeStartTick
            );
        }

        void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.Input)
                {
                    HandleInputMsg(sender, e);
                }
            }
        }

        void HandleInputMsg(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                InputMsg msg = message.Deserialize<InputMsg>();

                if (TickService.IsAfter(msg.EndTickExclusive, m_InputBuffer.EndExclusive))
                {
                    ushort tick = msg.StartTick;

                    foreach (ushort input in msg.Inputs)
                    {
                        // Don't overwrite inputs
                        if (!m_InputBuffer.HasInput(tick))
                        {
                            m_InputBuffer.WriteInput(tick, input);
                        }

                        tick = TickService.Add(tick, 1);
                    }

                    m_InputBuffer.EndExclusive = msg.EndTickExclusive;
                }

                SendInputAck(msg.EndTickExclusive);
            }
        }

        void SendInputAck(ushort receivedUntilTickExclusive)
        {
            ConnectionManager.Instance.SendMessage(() => InputAckMsg.CreateMessage(receivedUntilTickExclusive), SendMode.Unreliable);
        }
    }
}
