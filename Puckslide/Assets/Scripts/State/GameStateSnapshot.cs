using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PuckState
{
    public int InstanceId;
    public ChessPiece Piece;
    public bool IsWhite;
    public bool IsSticky;
    public Vector2 Position;
    public Vector2 Velocity;
    public float AngularVelocity;
    public float RotationZ;
    public Vector2Int GridPosition;

    public static PuckState FromPuck(PuckController puck)
    {
        return new PuckState
        {
            InstanceId = puck.GetInstanceID(),
            Piece = puck.ChessPiece,
            IsWhite = puck.IsWhitePiece,
            IsSticky = puck.IsSticky,
            Position = puck.transform.position,
            Velocity = puck.Velocity,
            AngularVelocity = puck.AngularVelocity,
            RotationZ = puck.transform.rotation.eulerAngles.z,
            GridPosition = puck.CurrentGridPosition
        };
    }
}

[Serializable]
public class BoardPieceState
{
    public Vector2Int Coordinates;
    public ChessPiece Piece;
}

[Serializable]
public class GameStateSnapshot
{
    public bool IsWhiteTurn;
    public bool IsPhase2Active;
    public List<BoardPieceState> BoardPieces = new List<BoardPieceState>();
    public List<PuckState> Pucks = new List<PuckState>();

    public static GameStateSnapshot Capture(GridManager gridManager)
    {
        GameStateSnapshot snapshot = new GameStateSnapshot
        {
            IsWhiteTurn = PuckController.IsWhiteTurn,
            IsPhase2Active = Phase2Manager.IsPhase2Active
        };

        if (gridManager == null)
        {
            gridManager = GameObject.FindObjectOfType<GridManager>();
        }

        if (gridManager != null)
        {
            Dictionary<Vector2Int, ChessPiece> layout = gridManager.GetLayoutCopy();
            foreach (KeyValuePair<Vector2Int, ChessPiece> entry in layout)
            {
                snapshot.BoardPieces.Add(new BoardPieceState
                {
                    Coordinates = entry.Key,
                    Piece = entry.Value
                });
            }
        }
        else
        {
            Debug.LogWarning("No GridManager found to capture board layout.");
        }

        foreach (PuckController puck in GameObject.FindObjectsOfType<PuckController>())
        {
            snapshot.Pucks.Add(PuckState.FromPuck(puck));
        }

        return snapshot;
    }
}
