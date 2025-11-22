using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField]
    private int m_Row;

    [SerializeField]
    private SpriteRenderer m_HighlightRenderer;

    [SerializeField]
    private Color m_DefaultHighlightColor = Color.white;

    private Piece m_CurrentPiece;

    private void Awake()
    {
        HideHighlight();
    }

    public bool HasPiece()
    {
        return m_CurrentPiece != null;
    }

    public void SetPiece(Piece piece)
    {
        m_CurrentPiece = piece;
    }

    public void ClearTile()
    {
        m_CurrentPiece = null;
    }

    public Piece GetCurrentPiece()
    {
        return m_CurrentPiece;
    }

    public int GetRow()
    {
        return m_Row;
    }

    public void ShowHighlight()
    {
        ShowHighlight(m_DefaultHighlightColor);
    }

    public void ShowHighlight(Color highlightColor)
    {
        if (m_HighlightRenderer != null)
        {
            m_HighlightRenderer.color = highlightColor;
            m_HighlightRenderer.enabled = true;
        }
    }

    public void HideHighlight()
    {
        if (m_HighlightRenderer != null)
        {
            m_HighlightRenderer.enabled = false;
        }
    }
}
