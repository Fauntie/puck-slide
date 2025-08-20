using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField]
    private int m_Row;

    [SerializeField]
    private SpriteRenderer m_Renderer;

    private Color m_BaseColor;
    
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
        if (m_Renderer == null)
        {
            m_Renderer = GetComponent<SpriteRenderer>();
        }

        if (m_Renderer != null)
        {
            m_BaseColor = m_Renderer.color;
        }
    }

    public void Highlight(bool enable)
    {
        if (m_Renderer != null)
        {
            m_Renderer.color = enable ? Color.yellow : m_BaseColor;
        }
    }
}
