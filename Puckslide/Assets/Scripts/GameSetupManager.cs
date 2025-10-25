using System;
using System.Collections;
using System.Collections.Generic;
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

    public PieceSetupData Clone()
    {
        return new PieceSetupData
        {
            Type = Type,
            WhiteCount = WhiteCount,
            BlackCount = BlackCount,
            Sticky = Sticky
        };
    }
}

public class GameSetupManager : MonoBehaviour
{
    private const int MAX_PIECES_PER_COLOR = 16;

    public event Action CountsChanged;
    
    [SerializeField]
    private PieceSetupData[] m_PieceSetup;

    [SerializeField]
    private PieceSetupConfig m_DefaultSetupConfig;

    [SerializeField]
    private GameObject m_Phase1Canvas;
    [SerializeField]
    private GameObject Phase1Environment;

    private int m_TotalWhiteCount;
    private int m_TotalBlackCount;


    private void OnEnable()
    {
        EventsManager.OnDeletePucks.Invoke(true);
        // TODO: TurnManager initialises after this script in the new scene
        // startup order. The follow-up task tracked in Docs/turn-order-reset.md
        // should make this reset resilient so we do not lose the first turn.
        TurnManager.ResetTurnOrder();
        if ((m_PieceSetup == null || m_PieceSetup.Length == 0) && m_DefaultSetupConfig != null)
        {
            ResetToDefaultSetup();
        }
        else
        {
            RecalculateTotals();
        }
    }

    public void StartButton()
    {
        EventsManager.OnPieceSetupData.Invoke(m_PieceSetup);
        m_Phase1Canvas.SetActive(true);
        Phase1Environment.SetActive(true);
        gameObject.SetActive(false);
    }


    public bool IncreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        PieceSetupData setupData = FindPieceSetupData(pieceType);
        if (setupData == null)
        {
            Debug.LogWarning($"No PieceSetupData found for {pieceType}");
            NotifyCountsChanged();
            return false;
        }

        if (!CanIncrease(pieceType, isWhite))
        {
            NotifyCountsChanged();
            return false;
        }

        if (isWhite)
        {
            setupData.WhiteCount++;
            m_TotalWhiteCount++;
        }
        else
        {
            setupData.BlackCount++;
            m_TotalBlackCount++;
        }

        NotifyCountsChanged();
        return true;
    }

    public bool DecreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        PieceSetupData setupData = FindPieceSetupData(pieceType);
        if (setupData == null)
        {
            Debug.LogWarning($"No PieceSetupData found for {pieceType}");
            NotifyCountsChanged();
            return false;
        }

        if (!CanDecrease(pieceType, isWhite))
        {
            NotifyCountsChanged();
            return false;
        }

        if (isWhite)
        {
            setupData.WhiteCount--;
            m_TotalWhiteCount--;
        }
        else
        {
            setupData.BlackCount--;
            m_TotalBlackCount--;
        }

        NotifyCountsChanged();
        return true;
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
        return m_TotalWhiteCount < MAX_PIECES_PER_COLOR;
    }

    public bool WithinBlackCount()
    {
        return m_TotalBlackCount < MAX_PIECES_PER_COLOR;
    }

    public bool CanIncrease(ChessPieceType pieceType, bool isWhite)
    {
        PieceSetupData setupData = FindPieceSetupData(pieceType);
        if (setupData == null)
        {
            return false;
        }

        int currentCount = isWhite ? setupData.WhiteCount : setupData.BlackCount;
        if (currentCount >= MAX_PIECES_PER_COLOR)
        {
            return false;
        }

        int totalCount = GetTotalCount(isWhite);
        return totalCount + 1 <= MAX_PIECES_PER_COLOR;
    }

    public bool CanDecrease(ChessPieceType pieceType, bool isWhite)
    {
        PieceSetupData setupData = FindPieceSetupData(pieceType);
        if (setupData == null)
        {
            return false;
        }

        int currentCount = isWhite ? setupData.WhiteCount : setupData.BlackCount;
        return currentCount > 0;
    }

    private PieceSetupData FindPieceSetupData(ChessPieceType pieceType)
    {
        for (int i = 0; i < m_PieceSetup.Length; i++)
        {
            if (m_PieceSetup[i].Type == pieceType)
            {
                return m_PieceSetup[i];
            }
        }

        return null;
    }

    private int GetTotalCount(bool isWhite)
    {
        return isWhite ? m_TotalWhiteCount : m_TotalBlackCount;
    }

    private void NotifyCountsChanged()
    {
        CountsChanged?.Invoke();
    }

    [ContextMenu("Reset To Default Setup")]
    public void ResetToDefaultSetup()
    {
        if (m_DefaultSetupConfig == null)
        {
            Debug.LogWarning("Default setup config is not assigned.");
            return;
        }

        m_PieceSetup = m_DefaultSetupConfig.CreateSetup();
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        m_TotalWhiteCount = 0;
        m_TotalBlackCount = 0;

        if (m_PieceSetup == null)
        {
            return;
        }

        for (int i = 0; i < m_PieceSetup.Length; i++)
        {
            m_TotalWhiteCount += m_PieceSetup[i].WhiteCount;
            m_TotalBlackCount += m_PieceSetup[i].BlackCount;
        }
    }
}
