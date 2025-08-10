using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase2Manager : MonoBehaviour
{
    private void OnEnable()
    {
        // Clear any lingering pucks from the board
        EventsManager.OnDeletePucks.Invoke(true);

        // Also remove chess pieces left over from previous games
        foreach (Piece piece in FindObjectsOfType<Piece>())
        {
            Tile tile = piece.GetCurrentTile();
            if (tile != null)
            {
                tile.ClearTile();
            }
            Destroy(piece.gameObject);
        }
    }
}
