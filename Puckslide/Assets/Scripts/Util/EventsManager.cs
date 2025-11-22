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
    public static readonly Evt<bool> OnBoardFlipState = new Evt<bool>();
}
