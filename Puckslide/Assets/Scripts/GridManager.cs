using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private float m_TileSize = 1f; // Match this to your tile size
    [SerializeField] private Vector2 m_GridOrigin = Vector2.zero; // Bottom-left of the grid
    private GameState m_GameState => GameState.Instance;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdatePieceLayout();
        }
    }

    public void SnapAllWithDelay(float delay = 0.1f)
    {
        StartCoroutine(SnapPucksOneByOne(delay));
    }

    private IEnumerator SnapPucksOneByOne(float delay)
    {
        PuckController[] pucks = GetComponentsInChildren<PuckController>();

        foreach (PuckController puck in pucks)
        {
            puck.SnapToGrid(m_TileSize, m_GridOrigin);
            yield return new WaitForSeconds(delay);
        }
    }

    public void UpdatePieceLayout()
    {
        EventsManager.OnBoardLayout.Invoke(ConvertLayout(m_GameState.GetLayout()));
    }

    // Rebuild the board layout without altering puck positions.
    public void UpdatePieceLayoutWithoutSnap()
    {
        EventsManager.OnBoardLayout.Invoke(ConvertLayout(m_GameState.GetLayout()));
    }

    private Dictionary<Vector2Int, ChessPiece> ConvertLayout(Dictionary<Position, ChessPiece> layout)
    {
        var dict = new Dictionary<Vector2Int, ChessPiece>();
        foreach (var kvp in layout)
        {
            dict[new Vector2Int(kvp.Key.X, kvp.Key.Y)] = kvp.Value;
        }
        return dict;
    }

    void OnDrawGizmos()
    {
        // Draw the grid in the scene view for debugging
        for (int x = 0; x < 8; x++) // Assuming an 8x8 board
        {
            for (int y = 0; y < 8; y++)
            {
                Vector3 position = new Vector3(m_GridOrigin.x + x * m_TileSize, m_GridOrigin.y + y * m_TileSize, 0);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(position + new Vector3(m_TileSize / 2, m_TileSize / 2, 0), new Vector3(m_TileSize, m_TileSize, 0));
            }
        }
    }
}
