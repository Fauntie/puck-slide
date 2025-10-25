using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private float m_TileSize = 1f; // Match this to your tile size
    [SerializeField] private Vector2 m_GridOrigin = Vector2.zero; // Bottom-left of the grid
    [SerializeField] private GameObject m_Phase1Canvas;
    [SerializeField] private GameObject m_Phase1Environment;
    [SerializeField] private GameObject m_Phase2Environment;
    [SerializeField] private GameObject m_Phase2Canvas;
    [SerializeField] private float m_SnapDelayOnStartPhase2 = 0f;

    private readonly Dictionary<Vector2Int, ChessPiece> m_PieceLayout = new Dictionary<Vector2Int, ChessPiece>();
    private bool m_IsTransitioningToPhase2;

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

    public void StartPhase2()
    {
        if (!gameObject.activeInHierarchy || m_IsTransitioningToPhase2)
        {
            return;
        }

        StartCoroutine(StartPhase2Routine());
    }

    private IEnumerator StartPhase2Routine()
    {
        m_IsTransitioningToPhase2 = true;

        if (m_SnapDelayOnStartPhase2 > 0f)
        {
            yield return SnapPucksOneByOne(m_SnapDelayOnStartPhase2);
        }

        UpdatePieceLayout();

        if (m_Phase1Canvas != null)
        {
            m_Phase1Canvas.SetActive(false);
        }

        if (m_Phase1Environment != null)
        {
            m_Phase1Environment.SetActive(false);
        }

        if (m_Phase2Environment != null)
        {
            m_Phase2Environment.SetActive(true);
        }

        if (m_Phase2Canvas != null)
        {
            m_Phase2Canvas.SetActive(true);
        }

        EventsManager.OnTurnChanged.Invoke(PuckController.IsWhiteTurn);

        m_IsTransitioningToPhase2 = false;
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
