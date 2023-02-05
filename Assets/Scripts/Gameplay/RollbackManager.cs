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
    public class RollbackManager : MonoBehaviour
    {
        [SerializeField]
        GameController GameController;

        ushort m_TickOfSavedState;

        void Awake()
        {
            Assert.IsTrue(GameController != null);
        }

        public void ResetForMatch()
        {
            m_TickOfSavedState = TickService.Subtract(TickService.StartTick, 1);
        }

        // Save the current gamestate for future rollback
        public void SaveRollbackState(ushort tick)
        {
            m_TickOfSavedState = tick;

            GameController.SelfPlayer.SaveRollbackState();
            GameController.PeerPlayer.SaveRollbackState();
        }

        // Rollback to the gamestate saved by the latest call to
        // SaveRollbackState() and return the tick of that state
        public ushort Rollback()
        {
            // We rollback to states in an increasing order, so we can
            // safely dispose of inputs for ticks prior to this state
            GameController.SelfPlayer.DisposeInputs(tickJustSimulated: TickService.Subtract(m_TickOfSavedState, 5));
            GameController.PeerPlayer.DisposeInputs(tickJustSimulated: TickService.Subtract(m_TickOfSavedState, 5));

            GameController.SelfPlayer.Rollback();
            GameController.PeerPlayer.Rollback();

            return m_TickOfSavedState;
        }
    }
}