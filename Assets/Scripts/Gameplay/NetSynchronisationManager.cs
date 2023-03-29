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
    public class NetSynchronisationManager : MonoBehaviour
    {
        void Start()
        {
            ConnectionManager.Instance.SetupComplete += OnConnectionSetupComplete;
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);
        }

        void OnConnectionSetupComplete()
        {
            ;

            // every now and then get ping
            // every now and then get tick of peer
            // estimate the peer's realtime tick using their last sent tick + ping
            // if our tick is slower, then speed up the clock
            // if our tick is faster, do nothing. the peer should speed up their clock by mirroring this procedure

            // TimeService.GetElapsedTime()
        }

        void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.Ping)
                {
                    HandlePingMsg(sender, e);
                }
                else if (message.Tag == Tags.PingAck)
                {
                    HandlePingAckMsg(sender, e);
                }
            }
        }

        void HandlePingMsg(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                PingMsg msg = message.Deserialize<PingMsg>();
                SendPingAck(msg.CurrentTick);
            }
        }

        void HandlePingAckMsg(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                PingAckMsg msg = message.Deserialize<PingAckMsg>();
            }
        }

        void SendPing()
        {
            ConnectionManager.Instance.SendMessage(() => PingMsg.CreateMessage(Clock.Instance.CurrentTick), SendMode.Reliable);
        }

        void SendPingAck(ushort tick)
        {
            ConnectionManager.Instance.SendMessage(() => PingAckMsg.CreateMessage(tick), SendMode.Unreliable);
        }
    }
}
