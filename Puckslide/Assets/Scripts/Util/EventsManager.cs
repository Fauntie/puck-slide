using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventsManager
{
    public static readonly Evt<bool> OnDeletePucks = new Evt<bool>();
    public static readonly Evt<PieceSetupData[]> OnPieceSetupData = new Evt<PieceSetupData[]>();

    public static readonly Evt<Dictionary<Vector2Int, ChessPiece>> OnBoardLayout = new Evt<Dictionary<Vector2Int, ChessPiece>>();
    public static readonly Evt<Rigidbody2D> OnPuckSpawned = new Evt<Rigidbody2D>();
    public static readonly Evt<Rigidbody2D> OnPuckDespawned = new Evt<Rigidbody2D>();
    public static readonly Evt<bool> OnTurnChanged = new Evt<bool>(true);
    public static readonly Evt<string> OnGameState = new Evt<string>();
}


public class Evt<T>
{
    private event Action<T> m_Action = delegate { };
    private T m_LastValue;

    public Evt(T defaultValue = default)
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
