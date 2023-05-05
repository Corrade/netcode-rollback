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
        const int m_StartCatchingUpThresholdTick = 6;
        const int m_StopCatchingUpThresholdTick = 2;
        const float m_CatchupSpeed = 2f;

        const int m_MaxRTTsCount = 5;
        LinkedList<float> m_RTTsSec = new LinkedList<float>();
        float m_SumRTTsSec;

        ushort m_LatestPingSentTick;
        float m_LatestPingSentTimestampSec;

        ushort m_LatestPeerPingTick;
        float m_LatestPeerPingTimestampSec;

        bool m_IsCatchingUp = false;
        
        void Start()
        {
            ConnectionManager.Instance.SetupComplete += OnConnectionSetupComplete;
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);
        }

        void Update()
        {
            if (DebugFlags.IsDebuggingSingleplayer)
            {
                return;
            }

            ushort peerCurrentTick = EstimatePeerCurrentTick();
            ushort behindByTick = TickService.IsAfter(peerCurrentTick, Clock.Instance.CurrentTick)
                ? TickService.Subtract(peerCurrentTick, Clock.Instance.CurrentTick)
                : (ushort)0;

            DebugUI.Write(
                DebugGroup.Networking,
                "RTT",
                $"RTT={EstimateRTTSec()}, peer current tick={peerCurrentTick}, behindByTick={behindByTick}"
            );

            if (m_IsCatchingUp)
            {
                if (HasCaughtUp(behindByTick))
                {
                    StopCatchingUp();
                }
            }
            // Not catching up and:
            else if (NeedsToCatchUp(behindByTick))
            {
                StartCatchingUp();
            }
        }

        ushort EstimatePeerCurrentTick()
        {
            float latestPeerPingSentTimestampSec = m_LatestPeerPingTimestampSec - (0.5f * EstimateRTTSec());
            float timeSinceLatestPeerPingSentSec = Time.time - latestPeerPingSentTimestampSec;
            Assert.IsTrue(timeSinceLatestPeerPingSentSec >= 0);
            int timeSinceLatestPeerPingSentTick = (int)(timeSinceLatestPeerPingSentSec * (float)TickService.Tickrate);
            return TickService.Add(m_LatestPeerPingTick, timeSinceLatestPeerPingSentTick);
        }

        float EstimateRTTSec()
        {
            /*
            RTT IS A MULTIPLE OF FRAME DURATION

            In general, although network latencies are actually continuous/
            highly granular, packets are processed in the update loop, which
            discretises their latencies into steps of frame duration (1/FPS)
            length.
            */

            return m_SumRTTsSec / m_RTTsSec.Count;
        }

        bool NeedsToCatchUp(ushort behindByTick)
        {
            return behindByTick >= m_StartCatchingUpThresholdTick;
        }

        bool HasCaughtUp(ushort behindByTick)
        {
            return behindByTick <= m_StopCatchingUpThresholdTick;
        }

        void StartCatchingUp()
        {
            m_IsCatchingUp = true;
            Clock.Instance.SetSpeedMultiplier(m_CatchupSpeed);
        }

        void StopCatchingUp()
        {
            Clock.Instance.ResetSpeedMultiplier();
            m_IsCatchingUp = false;
        }

        void OnConnectionSetupComplete()
        {
            SendPing();
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
                m_LatestPeerPingTimestampSec = Time.time;

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

                AddRTTSec(RTTSec: Time.time - m_LatestPingSentTimestampSec);

                // Continually circulate the ping
                SendPing();
            }
        }

        void AddRTTSec(float RTTSec)
        {
            m_SumRTTsSec += RTTSec;
            m_RTTsSec.AddLast(RTTSec);

            if (m_RTTsSec.Count > m_MaxRTTsCount)
            {
                m_SumRTTsSec -= m_RTTsSec.First.Value;
                m_RTTsSec.RemoveFirst();
            }
        }

        void SendPing()
        {
            m_LatestPingSentTick = Clock.Instance.CurrentTick;
            m_LatestPingSentTimestampSec = Time.time;

            ConnectionManager.Instance.SendMessage(() => PingMsg.CreateMessage(currentTick: m_LatestPingSentTick), SendMode.Reliable);
        }

        void SendPingAck(ushort receivedTick)
        {
            ConnectionManager.Instance.SendMessage(() => PingAckMsg.CreateMessage(receivedTick: receivedTick), SendMode.Reliable);
        }
    }
}
