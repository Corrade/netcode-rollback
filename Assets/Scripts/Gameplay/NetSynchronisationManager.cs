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
        const int m_StartCatchingUpThresholdTick = 3;
        const int m_StopCatchingUpThresholdTick = 1;
        const float m_CatchupSpeed = 2f;

        const int m_MaxRTTsCount = 5;
        LinkedList<float> m_RTTsSec = new LinkedList<float>();
        float m_SumRTTsSec;

        ushort m_LatestPingSentTick;
        float m_LatestPingSentTimestampSec;

        ushort m_LatestPeerPingTick;
        float m_ReceivedLatestPeerPingAtSec;

        bool m_IsCatchingUp = false;
        bool m_IsSyncEnabled = false;

        void Start()
        {
            ConnectionManager.Instance.SetupComplete += OnConnectionSetupComplete;
            ConnectionManager.Instance.AddOnMessageReceived(OnMessageReceived);
        }

        void Update()
        {
            if (DebugFlags.IsDebuggingSingleplayer)
                return;
            
            if (!m_IsSyncEnabled)
                return;

            ushort peerCurrentTick = EstimatePeerCurrentTick();
            ushort behindByTick = TickService.IsAfter(peerCurrentTick, Clock.Instance.CurrentTick)
                ? TickService.Subtract(peerCurrentTick, Clock.Instance.CurrentTick)
                : (ushort)0;

            if (DebugFlags.IsDebugging)
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
            // If this function is called immediately after receiving a peer
            // ping (which is the most intuitive example), then
            // m_ReceivedLatestPeerPingAtSec == Time.time, which simplifies
            // everything to (m_LatestPeerPingTick + #ticks in 0.5*RTT)
            float peerSentPingAtSec = m_ReceivedLatestPeerPingAtSec - (0.5f * EstimateRTTSec());
            float timeSincePeerSentPingSec = Time.time - peerSentPingAtSec;
            Assert.IsTrue(timeSincePeerSentPingSec >= 0);
            int timeSincePeerSentPingTick = (int)(timeSincePeerSentPingSec * (float)TickService.Tickrate);
            return TickService.Add(m_LatestPeerPingTick, timeSincePeerSentPingTick);
        }

        // Dampen net jitter by taking an average of recent RTT measurements
        float EstimateRTTSec()
        {
            /*
            RTT IS A MULTIPLE OF FRAME DURATION

            In general, although network latencies are actually continuous/
            highly granular, packets are processed in the update loop, which
            discretises their latencies into steps of frame-duration length*
            (1/FPS). This causes rounding.

            For example, if a packet arrives at t=0.0023, one might actually
            see it at t=1/60 given a 60 updates/sec loop.

            This can impact both incoming and outgoing packets by deferring
            the reading-from or writing-to of a network buffer.

            *Assuming traffic is handled at a consistent location in the update
            loop and the update loop is in itself consistent. Both are probably
            true for non-taxing projects like this one.
            */

            return m_RTTsSec.Count == 0
                ? 0
                : m_SumRTTsSec / m_RTTsSec.Count;
        }

        // We accept tick differences within a threshold to prevent clients from
        // continuously flip-flopping between catching up and resting in a
        // futile pursuit of perfect synchronisation
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
            m_IsSyncEnabled = true;
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
                m_ReceivedLatestPeerPingAtSec = Time.time;

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
