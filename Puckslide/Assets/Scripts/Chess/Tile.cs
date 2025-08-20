using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField]
    private int m_Row;

    [SerializeField]
    private SpriteRenderer m_SpriteRenderer;

    private Color m_DefaultColor;
    
    private Piece m_CurrentPiece;

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

    private void Awake()
    {
        if (m_SpriteRenderer != null)
        {
            m_DefaultColor = m_SpriteRenderer.color;
        }
    }

    public void Highlight(Color color)
    {
        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.color = color;
        }
    }

    public void ClearHighlight()
    {
        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.color = m_DefaultColor;
        }
    }
}
