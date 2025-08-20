using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum ChessPieceType
{
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King
}

[System.Serializable]
public class PieceSetupData
{
    public ChessPieceType Type;
    public int WhiteCount;
    public int BlackCount;
    public bool Sticky;
}

public class GameSetupManager : MonoBehaviour
{
    private const int MAX_PIECES_PER_COLOR = 16;
    
    [SerializeField]
    private PieceSetupData[] m_PieceSetup = new PieceSetupData[]
    {
        new PieceSetupData { Type = ChessPieceType.Pawn,   WhiteCount=4, BlackCount=4, Sticky=true },
        new PieceSetupData { Type = ChessPieceType.Knight, WhiteCount=1, BlackCount=1, Sticky=false },
        new PieceSetupData { Type = ChessPieceType.Bishop, WhiteCount=1, BlackCount=1, Sticky=false },
        new PieceSetupData { Type = ChessPieceType.Rook,   WhiteCount=1, BlackCount=1, Sticky=false },
        new PieceSetupData { Type = ChessPieceType.Queen,  WhiteCount=1, BlackCount=1, Sticky=false },
        new PieceSetupData { Type = ChessPieceType.King,   WhiteCount=1, BlackCount=1, Sticky=true },
    };

    [SerializeField]
    private GameObject m_Phase1Canvas;
    [SerializeField]
    private GameObject Phase1Environment;
    

    private void OnEnable()
    {
        EventsManager.OnDeletePucks.Invoke(true);
        EventsManager.ResetTurn();

        m_PieceSetup = new PieceSetupData[]
        {
            new PieceSetupData { Type = ChessPieceType.Pawn,   WhiteCount=4, BlackCount=4, Sticky=true },
            new PieceSetupData { Type = ChessPieceType.Knight, WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.Bishop, WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.Rook,   WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.Queen,  WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.King,   WhiteCount=1, BlackCount=1, Sticky=true },
        };
    }

    public void StartButton()
    {
        EventsManager.OnPieceSetupData.Invoke(m_PieceSetup);
        m_Phase1Canvas.SetActive(true);
        Phase1Environment.SetActive(true);
        gameObject.SetActive(false);
    }


    public void IncreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        for (int i = 0; i < m_PieceSetup.Length; i++)
        {
            if (m_PieceSetup[i].Type == pieceType)
            {
                if (isWhite)
                {
                    m_PieceSetup[i].WhiteCount++;
                }
                else
                {
                    m_PieceSetup[i].BlackCount++;
                }
                return;
            }
        }

        Debug.LogWarning($"No PieceSetupData found for {pieceType}");
    }
    
    public void DecreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        for (int i = 0; i < m_PieceSetup.Length; i++)
        {
            if (m_PieceSetup[i].Type == pieceType)
            {
                if (isWhite)
                {
                    m_PieceSetup[i].WhiteCount--;
                }
                else
                {
                    m_PieceSetup[i].BlackCount--;
                }
                return;
            }
        }

        Debug.LogWarning($"No PieceSetupData found for {pieceType}");
    }

    public void ToggleSticky(ChessPieceType pieceType, bool isSticky)
    {
        for (int i = 0; i < m_PieceSetup.Length; i++)
        {
            if (m_PieceSetup[i].Type == pieceType)
            {
                m_PieceSetup[i].Sticky = isSticky;
                return;
            }
        }
    }

    public int GetCount(ChessPieceType pieceType, bool isWhite)
    {
        for (int i = 0; i < m_PieceSetup.Length; i++)
        {
            if (m_PieceSetup[i].Type == pieceType)
            {
                return isWhite ? m_PieceSetup[i].WhiteCount : m_PieceSetup[i].BlackCount;
            }
        }

        Debug.LogWarning($"No PieceSetupData found for {pieceType}");
        return 0;
    }

    public bool GetSticky(ChessPieceType pieceType)
    {
        for (int i = 0; i < m_PieceSetup.Length; i++)
        {
            if (m_PieceSetup[i].Type == pieceType)
            {
                return m_PieceSetup[i].Sticky;
            }
        }

        Debug.LogWarning($"No PieceSetupData found for {pieceType}");
        return false;
    }


    public bool WithinWhiteCount()
    {
        return m_PieceSetup.Sum(piece => piece.WhiteCount) < MAX_PIECES_PER_COLOR;
    }

    public bool WithinBlackCount()
    {
        return m_PieceSetup.Sum(piece => piece.BlackCount) < MAX_PIECES_PER_COLOR;
    }
}
