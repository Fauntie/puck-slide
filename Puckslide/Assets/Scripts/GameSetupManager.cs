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
    

    private void OnEnable()
    {
        EventsManager.OnDeletePucks.Invoke(true);
        PuckController.ResetTurnOrder();
        if ((m_PieceSetup == null || m_PieceSetup.Length == 0) && m_DefaultSetupConfig != null)
        {
            ResetToDefaultSetup();
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
        return IncreaseCount(pieceType, isWhite, out _);
    }

    public bool IncreaseCount(ChessPieceType pieceType, bool isWhite, out string errorMessage)
    {
        bool changed = TryAdjustCount(pieceType, isWhite, 1, out errorMessage);
        NotifyCountsChanged();
        return changed;
    }

    public bool DecreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        return DecreaseCount(pieceType, isWhite, out _);
    }

    public bool DecreaseCount(ChessPieceType pieceType, bool isWhite, out string errorMessage)
    {
        bool changed = TryAdjustCount(pieceType, isWhite, -1, out errorMessage);
        NotifyCountsChanged();
        return changed;
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
        return GetTotalCount(true) <= MAX_PIECES_PER_COLOR;
    }

    public bool WithinBlackCount()
    {
        return GetTotalCount(false) <= MAX_PIECES_PER_COLOR;
    }

    public bool CanIncrease(ChessPieceType pieceType, bool isWhite)
    {
        PieceSetupData setupData = FindPieceSetupData(pieceType);
        if (setupData == null)
        {
            return false;
        }

        return IsAdjustmentAllowed(setupData, pieceType, isWhite, 1, out _);
    }

    public bool CanDecrease(ChessPieceType pieceType, bool isWhite)
    {
        PieceSetupData setupData = FindPieceSetupData(pieceType);
        if (setupData == null)
        {
            return false;
        }

        return IsAdjustmentAllowed(setupData, pieceType, isWhite, -1, out _);
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

    private bool TryAdjustCount(ChessPieceType pieceType, bool isWhite, int delta, out string errorMessage)
    {
        PieceSetupData setupData = FindPieceSetupData(pieceType);
        if (setupData == null)
        {
            errorMessage = $"No PieceSetupData found for {pieceType}";
            Debug.LogWarning(errorMessage);
            return false;
        }

        if (!IsAdjustmentAllowed(setupData, pieceType, isWhite, delta, out errorMessage))
        {
            return false;
        }

        if (isWhite)
        {
            setupData.WhiteCount += delta;
        }
        else
        {
            setupData.BlackCount += delta;
        }

        return true;
    }

    private bool IsAdjustmentAllowed(PieceSetupData setupData, ChessPieceType pieceType, bool isWhite, int delta, out string errorMessage)
    {
        int currentCount = isWhite ? setupData.WhiteCount : setupData.BlackCount;
        int proposedCount = currentCount + delta;

        if (proposedCount < 0)
        {
            errorMessage = $"Cannot assign fewer than 0 {GetColorLabel(isWhite)} {pieceType} pieces.";
            return false;
        }

        if (proposedCount > MAX_PIECES_PER_COLOR)
        {
            errorMessage = $"Cannot assign more than {MAX_PIECES_PER_COLOR} {GetColorLabel(isWhite)} {pieceType} pieces.";
            return false;
        }

        int totalCount = GetTotalCount(isWhite);
        int proposedTotal = totalCount + delta;

        if (proposedTotal < 0)
        {
            errorMessage = $"Cannot have fewer than 0 total {GetColorLabel(isWhite)} pieces.";
            return false;
        }

        if (proposedTotal > MAX_PIECES_PER_COLOR)
        {
            errorMessage = $"Cannot exceed {MAX_PIECES_PER_COLOR} total {GetColorLabel(isWhite)} pieces.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static string GetColorLabel(bool isWhite)
    {
        return isWhite ? "white" : "black";
    }

    private int GetTotalCount(bool isWhite)
    {
        return m_PieceSetup.Sum(piece => isWhite ? piece.WhiteCount : piece.BlackCount);
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
    }
}
