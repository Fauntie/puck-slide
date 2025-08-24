using System.Collections.Generic;
using UnityEngine;

public class PieceRegistry : MonoBehaviour
{
    public static PieceRegistry Instance { get; private set; }

    private readonly List<Piece> m_Pieces = new List<Piece>();
    private IEventBus m_EventBus;

    public Piece[] GetPieces()
    {
        return m_Pieces.ToArray();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        m_EventBus = FindObjectOfType<EventBusBootstrap>()?.Bus;
    }

    private void OnEnable()
    {
        m_EventBus?.Subscribe<Piece>(EventBusEvents.PieceSpawned, OnPieceSpawned);
        m_EventBus?.Subscribe<Piece>(EventBusEvents.PieceDespawned, OnPieceDespawned);
    }

    private void OnDisable()
    {
        m_EventBus?.Unsubscribe<Piece>(EventBusEvents.PieceSpawned, OnPieceSpawned);
        m_EventBus?.Unsubscribe<Piece>(EventBusEvents.PieceDespawned, OnPieceDespawned);
        m_Pieces.Clear();
    }

    private void OnPieceSpawned(Piece piece)
    {
        if (piece != null && !m_Pieces.Contains(piece))
        {
            m_Pieces.Add(piece);
        }
    }

    private void OnPieceDespawned(Piece piece)
    {
        m_Pieces.Remove(piece);
    }
}
