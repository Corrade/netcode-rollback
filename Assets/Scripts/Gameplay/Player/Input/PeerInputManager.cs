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
        public override void Initialise(ConnectionManager connectionManager)
        {
            base.Initialise(connectionManager);
            connectionManager.AddOnMessageReceived(OnMessageReceived);
        }

        void WriteInput(ushort tick, ushort input)
        {
            m_InputHistory[tick] = input;
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

                // TODO: for all inputs in this packet, add to the input history

                SendAck(msg.EndTick);
            }
        }

        void SendAck(ushort tick)
        {
            using (Message msg = InputAckMsg.CreateMessage(tick))
            {
                m_ConnectionManager.SendMessage(msg, SendMode.Unreliable);
            }
        }
    }
}
