using System;
using UnityEngine;
using UnityEngine.UI;

public class GameSetupManager : MonoBehaviour
{
    public event Action CountsChanged;

    [SerializeField]
    private PieceSetupData[] m_DefaultPieceSetup = PieceSetupState.CreateDefaultSetup().ToArray();

    [SerializeField]
    private GameObject m_Phase1Canvas;
    [SerializeField]
    private GameObject Phase1Environment;

    private PieceSetupState m_PieceSetupState;


    private void OnEnable()
    {
        EventsManager.OnDeletePucks.Invoke(true);
        PuckController.ResetTurnOrder();

        if (m_DefaultPieceSetup == null || m_DefaultPieceSetup.Length == 0)
        {
            m_DefaultPieceSetup = PieceSetupState.CreateDefaultSetup().ToArray();
        }

        m_PieceSetupState = new PieceSetupState(m_DefaultPieceSetup);
    }

    public void StartButton()
    {
        EventsManager.OnPieceSetupData.Invoke(m_PieceSetupState.GetSnapshot().ToArray());
        m_Phase1Canvas.SetActive(true);
        Phase1Environment.SetActive(true);
        gameObject.SetActive(false);
    }


    public bool IncreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        bool changed = m_PieceSetupState.IncreaseCount(pieceType, isWhite);
        NotifyCountsChanged();
        return changed;
    }

    public bool DecreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        bool changed = m_PieceSetupState.DecreaseCount(pieceType, isWhite);
        NotifyCountsChanged();
        return changed;
    }

    public void ToggleSticky(ChessPieceType pieceType, bool isSticky)
    {
        m_PieceSetupState.ToggleSticky(pieceType, isSticky);
    }

    public int GetCount(ChessPieceType pieceType, bool isWhite)
    {
        return m_PieceSetupState.GetCount(pieceType, isWhite);
    }

    public bool GetSticky(ChessPieceType pieceType)
    {
        return m_PieceSetupState.GetSticky(pieceType);
    }


    public bool WithinWhiteCount()
    {
        return m_PieceSetupState.GetTotalCount(true) < PieceSetupState.MaxPiecesPerColor;
    }

    public bool WithinBlackCount()
    {
        return m_PieceSetupState.GetTotalCount(false) < PieceSetupState.MaxPiecesPerColor;
    }

    public bool CanIncrease(ChessPieceType pieceType, bool isWhite)
    {
        return m_PieceSetupState.CanIncrease(pieceType, isWhite);
    }

    public bool CanDecrease(ChessPieceType pieceType, bool isWhite)
    {
        return m_PieceSetupState.CanDecrease(pieceType, isWhite);
    }

    private void NotifyCountsChanged()
    {
        CountsChanged?.Invoke();
    }
}
