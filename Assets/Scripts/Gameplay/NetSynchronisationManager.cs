using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        const int m_MaxRTTsCount = 5;

        List<float> m_RTTs = new List<float>();

        float m_LatestPingSentTimestampMs;
        ushort m_LatestPingSentTick;

        // variable names are ass TODO
        ushort m_LatestPeerPingTick;
        float m_LatestPeerPingTimestampMs;
        
        void Start()
        {
            ConnectionManager.Instance.SetupComplete += OnConnectionSetupComplete;
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);
        }

        void OnConnectionSetupComplete()
        {
            // Continually circulate exactly one ping (that is, one ping originating from self - the peer will do the same)
            SendPing();

            // TODO
            // if our tick is slower, then speed up the clock
            // if our tick is faster, do nothing (the peer should speed up their clock by mirroring this procedure)
        }

        ushort EstimatePeerCurrentTick()
        {
            float latestPeerPingSentTimestampMs = m_LatestPeerPingTimestampMs - (0.5f * EstimateRTT());
            float timeSinceLatestPeerPingSentMs = TimeService.GetTimestampMs() - latestPeerPingSentTimestampMs;
            Assert.IsTrue(timeSinceLatestPeerPingSentMs >= 0);
            int timeSinceLatestPeerPingSentTick = (int)(timeSinceLatestPeerPingSentMs / (float)TickService.Tickrate);
            return TickService.Add(m_LatestPeerPingTick, timeSinceLatestPeerPingSentTick);
        }

        float EstimateRTT()
        {
            return m_RTTs.Average();
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

                m_LatestPeerPingTick = msg.CurrentTick;
                m_LatestPeerPingTimestampMs = TimeService.GetTimestampMs();

                SendPingAck(receivedTick: msg.CurrentTick);
            }
        }

        void HandlePingAckMsg(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                PingAckMsg msg = message.Deserialize<PingAckMsg>();

                // Peer acked the latest ping
                Assert.IsTrue(msg.ReceivedTick == m_LatestPingSentTick);

                float m_RTT = TimeService.GetTimestampMs() - m_LatestPingSentTimestampMs;

                m_RTTs.Add(m_RTT);
                if (m_RTTs.Count() > m_MaxRTTsCount)
                {
                    m_RTTs.RemoveAt(0);
                }

                // Keep pinging
                SendPing();
            }
        }

        void SendPing()
        {
            m_LatestPingSentTimestampMs = TimeService.GetTimestampMs();
            m_LatestPingSentTick = Clock.Instance.CurrentTick;

            ConnectionManager.Instance.SendMessage(() => PingMsg.CreateMessage(currentTick: m_LatestPingSentTick), SendMode.Reliable);
        }

        void SendPingAck(ushort receivedTick)
        {
            ConnectionManager.Instance.SendMessage(() => PingAckMsg.CreateMessage(receivedTick: receivedTick), SendMode.Reliable);
        }
    }
}
