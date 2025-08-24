using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase2Manager : MonoBehaviour
{
    [SerializeField]
    private Transform m_BoardTransform;
    [SerializeField]
    private int m_GridSize = 8;
    [SerializeField]
    private float m_TileSize = 0.383f;

    /// <summary>
    /// Indicates whether Phase 2 gameplay is currently active.
    /// </summary>
    public static bool IsPhase2Active { get; private set; }

    private void OnEnable()
    {
        IsPhase2Active = true;
        // Record the current puck layout before we remove the physical pucks
        // from the board.
        CollectPucks();

        // Destroy any existing pucks so only Phase 2 pieces remain when the
        // expanded board becomes active.
        foreach (PuckController puck in FindObjectsOfType<PuckController>(true))
        {
            Destroy(puck.gameObject);
        }

        // Rebuild the board with Phase 2 chess pieces based on the recorded
        // layout.
        BoardController board = FindObjectOfType<BoardController>();
        if (board != null)
        {
            board.enabled = false;
            board.enabled = true;
        }

        // Ensure the BoardFlipper knows about the Phase 2 board so pieces
        // remain aligned when the board is rotated.
        if (m_BoardTransform != null)
        {
            BoardFlipper.SetBoard(m_BoardTransform, m_GridSize, m_TileSize);
            BoardFlipper.SetFlipOffset(new Vector3(0f, -1f, 0f));
        }
    }

    /// <summary>
    /// Gathers all puck positions on the board and writes them to the
    /// GameState so Phase 2 can rebuild the layout with chess pieces.
    /// </summary>
    private void CollectPucks()
    {
        GameState state = GameState.Instance;
        state.Clear();
        GridManager gridManager = FindObjectOfType<GridManager>();
        foreach (PuckController puck in FindObjectsOfType<PuckController>())
        {
            // Update each puck's grid position and record it if valid.
            puck.UpdateGridPosition(m_TileSize, gridManager.GridOrigin);
            Vector2Int pos = puck.CurrentGridPosition;
            if (pos.x >= 0 && pos.y >= 0)
            {
                state.SetPiece(new Position(pos.x, pos.y), puck.ChessPiece);
            }
        }
    }

    private void OnDisable()
    {
        IsPhase2Active = false;
    }

    public void EndGame()
    {
        // Remove all pieces from the board when the player chooses to end
        // the game.
        foreach (Piece piece in FindObjectsOfType<Piece>())
        {
            Destroy(piece.gameObject);
        }
    }
}
