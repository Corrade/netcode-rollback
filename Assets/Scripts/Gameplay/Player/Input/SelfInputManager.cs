using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DarkRift.Server;
using DarkRift.Server.Unity;

using Lockstep;

/*

****  ***************       **
 1     2     3     4     5     6

Legend
    Time goes from left to right
    1, 2, ...  Ticks where input is processed
    *          Keypress

Problem: input is processed at a fixed rate (tickrate), but input is provided
at a different, unsynchronised rate (framerate). This is analogous to
discretisation.

Case of keypress sequence 1: The player holds the key from tick "0.5-1.5".
=> 1 tick worth of input @ tick 1

Case 2: The player holds the key from tick "1.9-4.1".
=> 3 ticks worth of input @ ticks 2, 3, 4

Case 3: The player holds the key from tick "5.5-5.6". This is only possible when
the framerate is higher than the tickrate and the player is agile enough to
rapidly press and release a key between ticks.
We must ensure that input isn't lost in these cases.
=> 1 tick worth of input @ tick 6

Cases 1 and 2 are easily handled by tracking what keys are currently
pressed and updating this based on framerate.

Case 3 is handled by a separate data structure. Each time we begin pressing a
key, we set its bit in m_KeysJustPressed. The clear this bit array after every
tick.
*/

namespace Lockstep
{
    public class SelfInputManager : InputManager
    {
        ushort m_KeysJustPressed;
        ushort m_KeysCurrentlyPressed;
        ushort m_NextTickToSend;

        // Refactor
        Dictionary<ushort, KeyCode> m_Binds = new Dictionary<ushort, KeyCode>{
            { InputMasks.MoveLeft, KeyCode.A },
            { InputMasks.MoveRight, KeyCode.D },
            { InputMasks.Dive, KeyCode.Space },
            { InputMasks.Kick, KeyCode.LeftShift }
        };

        void Awake()
        {
            AssertInputsBound();
        }

        public override void Initialise()
        {
            ConnectionManager.Instance.RemoveOnMessageReceived(OnMessageReceived);
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);

            m_KeysJustPressed = 0;
            m_KeysCurrentlyPressed = 0;

            // We need the tick before since simulation depends on prior ticks
            m_NextTickToSend = TickService.Subtract(TickService.StartSimulationTick, 1);

            m_InputBuffer.Initialise(
                startInclusive: m_NextTickToSend,
                endExclusive: TickService.StartTick
            );
        }

        void Update()
        {
            foreach (ushort inputMask in InputMasks.AllMasks)
            {
                if (Input.GetKey(m_Binds[inputMask]))
                {
                    // Set the input's bit
                    m_KeysCurrentlyPressed |= inputMask;
                    m_KeysJustPressed |= inputMask;
                }
                else
                {
                    // Clear the input's bit
                    m_KeysCurrentlyPressed &= (ushort)(~inputMask);
                }
            }
        }

        public override void DisposeInputs(ushort tickJustSimulated)
        {
            m_InputBuffer.StartInclusive = TickService.Min(tickJustSimulated, m_NextTickToSend);
        }

        public void WriteInput(ushort currentTick)
        {
            Assert.IsTrue(!m_InputBuffer.HasInput(currentTick));
            m_InputBuffer.WriteInput(currentTick, (ushort)(m_KeysCurrentlyPressed | m_KeysJustPressed));
            m_KeysJustPressed = 0;
            m_InputBuffer.EndExclusive = TickService.Add(currentTick, 1);
        }

        public void SendUnackedInputs(ushort untilTickExclusive)
        {
            /*
            DebugUI.Instance.Write("inputs", $"SendUnackedInputs from [{m_NextTickToSend}, {untilTickExclusive}) with buffer [{m_InputBuffer.StartInclusive}, {m_InputBuffer.EndExclusive})");
            */

            List<ushort> inputs = new List<ushort>();

            for (ushort t = m_NextTickToSend; TickService.IsBefore(t, untilTickExclusive); t = TickService.Add(t, 1))
            {
                inputs.Add(m_InputBuffer.GetRawInput(t));
            }

            if (inputs.Count == 0)
            {
                return;
            }

            SendInputs(startTick: m_NextTickToSend, inputs: inputs.ToArray());
        }

        void SendInputs(ushort startTick, ushort[] inputs)
        {
            using (Message msg = InputMsg.CreateMessage(startTick, inputs))
            {
                ConnectionManager.Instance.SendMessage(msg, SendMode.Unreliable);
            }
        }

        void OnMessageReceived(object sender, DarkRift.Client.MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.InputAck)
                {
                    HandleInputAckMsg(sender, e);
                }
            }
        }

        void HandleInputAckMsg(object sender, DarkRift.Client.MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                InputAckMsg msg = message.Deserialize<InputAckMsg>();

                if (TickService.IsAfter(msg.ReceivedUntilTickExclusive, m_NextTickToSend))
                {
                    m_NextTickToSend = msg.ReceivedUntilTickExclusive;
                }
            }
        }

        void AssertInputsBound()
        {
            foreach (ushort inputMask in InputMasks.AllMasks)
            {
                Assert.IsTrue(m_Binds.ContainsKey(inputMask));
            }
        }
    }
}
