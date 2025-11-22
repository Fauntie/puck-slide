using System;
using System.Collections.Generic;
using UnityEngine;

namespace Puckslide.Networking
{
    public static class NetworkEvents
    {
        public static readonly NetworkEvt<bool> OnDeletePucks = new NetworkEvt<bool>();
        public static readonly NetworkEvt<PieceSetupMessage> OnPieceSetupData = new NetworkEvt<PieceSetupMessage>();
        public static readonly NetworkEvt<Dictionary<Vector2Int, ChessPiece>> OnBoardLayout = new NetworkEvt<Dictionary<Vector2Int, ChessPiece>>();
        public static readonly NetworkEvt<Rigidbody2D> OnPuckSpawned = new NetworkEvt<Rigidbody2D>();
        public static readonly NetworkEvt<Rigidbody2D> OnPuckDespawned = new NetworkEvt<Rigidbody2D>();
        public static readonly NetworkEvt<TurnChangeMessage> OnTurnChanged = new NetworkEvt<TurnChangeMessage>(new TurnChangeMessage
        {
            IsWhiteTurn = true,
            TurnNumber = 0,
            ServerTime = 0d
        });
        public static readonly NetworkEvt<bool> OnBoardFlipState = new NetworkEvt<bool>();
        public static readonly NetworkEvt<NetworkLobbySnapshot> OnLobbySnapshot = new NetworkEvt<NetworkLobbySnapshot>();
        public static readonly NetworkEvt<PuckSpawnMessage> OnNetworkPuckSpawned = new NetworkEvt<PuckSpawnMessage>();
        public static readonly NetworkEvt<PuckDespawnMessage> OnNetworkPuckDespawned = new NetworkEvt<PuckDespawnMessage>();
        public static readonly NetworkEvt<ShotLaunchMessage> OnShotLaunched = new NetworkEvt<ShotLaunchMessage>();
        public static readonly NetworkEvt<PlayerCommandMessage> OnPlayerCommandSubmitted = new NetworkEvt<PlayerCommandMessage>();
        public static readonly NetworkEvt<PuckStateSnapshotMessage> OnPuckSnapshot = new NetworkEvt<PuckStateSnapshotMessage>();
        public static readonly NetworkEvt<TurnDeterminismMessage> OnTurnDeterminism = new NetworkEvt<TurnDeterminismMessage>();
    }

    public class NetworkEvt<T>
    {
        private event Action<T> m_Action = delegate { };
        private T m_LastValue;

        public NetworkEvt(T defaultValue = default)
        {
            m_LastValue = defaultValue;
        }

        public void Invoke(T param)
        {
            m_LastValue = param;
            m_Action.Invoke(param);
        }

        public void AddListener(Action<T> listener, bool receiveLastValue = false)
        {
            m_Action += listener;
            if (receiveLastValue)
            {
                listener(m_LastValue);
            }
        }

        public void RemoveListener(Action<T> listener)
        {
            m_Action -= listener;
        }
    }
}
