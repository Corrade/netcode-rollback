using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using Rollback;

namespace Rollback
{
    public class PeerInputManager : InputManager
    {
        public override void Initialise()
        {
            ConnectionManager.Instance.RemoveOnMessageReceived(OnMessageReceived);
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);

            ResetForRound(TickService.StartTick);
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

                SendInputAck(receivedUntilTickExclusive: msg.EndTickExclusive);
            }
        }

        void SendInputAck(ushort receivedUntilTickExclusive)
        {
            ConnectionManager.Instance.SendMessage(() => InputAckMsg.CreateMessage(receivedUntilTickExclusive), SendMode.Unreliable);
        }
    }
}
