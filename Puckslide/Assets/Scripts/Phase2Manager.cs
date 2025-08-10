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
        // Clear any lingering pucks from the board. Pieces will be spawned
        // from the last recorded layout by the BoardController, so avoid
        // destroying them here.
        EventsManager.OnDeletePucks.Invoke(true);

        // Ensure the BoardFlipper knows about the Phase 2 board so pieces
        // remain aligned when the board is rotated.
        if (m_BoardTransform != null)
        {
            BoardFlipper.SetBoard(m_BoardTransform, m_GridSize, m_TileSize);
            BoardFlipper.SetFlipOffset(new Vector3(0f, -1f, 0f));
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
