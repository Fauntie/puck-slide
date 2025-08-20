using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private float m_TileSize = 1f; // Match this to your tile size
    [SerializeField] private Vector2 m_GridOrigin = Vector2.zero; // Bottom-left of the grid
    private Dictionary<Vector2Int, ChessPiece> m_PieceLayout = new Dictionary<Vector2Int, ChessPiece>();

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
        PuckController[] pucks = FindObjectsOfType<PuckController>();

        foreach (PuckController puck in pucks)
        {
            puck.SnapToGrid(m_TileSize, m_GridOrigin);
            yield return new WaitForSeconds(delay);
        }
    }

    public void UpdatePieceLayout()
    {
        m_PieceLayout.Clear(); // Clear the layout before recalculating

        // Find all pucks in the scene
        PuckController[] pucks = FindObjectsOfType<PuckController>();

        foreach (PuckController puck in pucks)
        {
            puck.SnapToGrid(m_TileSize, m_GridOrigin);
        }
        
        foreach (PuckController puck in pucks)
        {
            // Update each puck's grid position
            puck.UpdateGridPosition(m_TileSize, m_GridOrigin);

            if (puck.CurrentGridPosition != new Vector2Int(-1, -1))
            {
                if (m_PieceLayout.ContainsKey(puck.CurrentGridPosition))
                {
                    Debug.LogWarning($"Duplicate piece at {puck.CurrentGridPosition} replaced.");
                }
                m_PieceLayout[puck.CurrentGridPosition] = puck.ChessPiece;
            }
        }
        
        EventsManager.OnBoardLayout.Invoke(m_PieceLayout);
    }

    // Rebuild the board layout without altering puck positions.
    public void UpdatePieceLayoutWithoutSnap()
    {
        m_PieceLayout.Clear();

        // Find all pucks in the scene
        PuckController[] pucks = FindObjectsOfType<PuckController>();

        foreach (PuckController puck in pucks)
        {
            // Update each puck's grid position based on its current location
            puck.UpdateGridPosition(m_TileSize, m_GridOrigin);

            if (puck.CurrentGridPosition != new Vector2Int(-1, -1))
            {
                if (m_PieceLayout.ContainsKey(puck.CurrentGridPosition))
                {
                    Debug.LogWarning($"Duplicate piece at {puck.CurrentGridPosition} replaced.");
                }
                m_PieceLayout[puck.CurrentGridPosition] = puck.ChessPiece;
            }
        }

        EventsManager.OnBoardLayout.Invoke(m_PieceLayout);
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
