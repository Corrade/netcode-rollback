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
        ushort m_LatestAckReceived;

        // Refactor
        Dictionary<ushort, KeyCode> m_Binds = new Dictionary<ushort, KeyCode>{
            { InputMasks.MoveLeft, KeyCode.A },
            { InputMasks.MoveRight, KeyCode.D },
            { InputMasks.Dive, KeyCode.Space },
            { InputMasks.Kick, KeyCode.LeftShift }
        };

        protected override void Awake()
        {
            base.Awake();
            AssertInputsBound();
        }

        public override void Initialise(ConnectionManager connectionManager)
        {
            base.Initialise(connectionManager);
            connectionManager.AddOnMessageReceived(OnMessageReceived);
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

        public void WriteInput(ushort currentTick)
        {
            m_InputHistory[currentTick] = (ushort)(m_KeysCurrentlyPressed | m_KeysJustPressed);
            m_KeysJustPressed = 0;
        }

        public void SendUnackedInputs(ushort currentTick)
        {
            // TODO: for m_LatestAckReceived to current tick inclusive etc.
            // if nothing has been acked yet then it's t he start of the game and the peer is waiting on currentTick-InputDelay
            // deal with invalid input vs no input
            // should never send an invalid input
            // there may be a problem in invalidating input once the ticks wrap around -
            // perhaps change the check in game loop from "peer.hasinput(t)" to "peer.latestinputreceived >= t"
            // yeah perhaps remove or rewrite hasinput to be like the above
            ;
        }

        void AssertInputsBound()
        {
            foreach (ushort inputMask in InputMasks.AllMasks)
            {
                Assert.IsTrue(m_Binds.ContainsKey(inputMask));
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

                // Update the latest ack received
                if (TickService.IsTickAfter(msg.LatestTickReceived, m_LatestAckReceived))
                {
                    m_LatestAckReceived = msg.LatestTickReceived;
                }
            }
        }
    }
}
