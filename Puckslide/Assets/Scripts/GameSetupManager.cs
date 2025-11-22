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

    public event Action CountsChanged;

    [SerializeField]
    private bool m_IsLocalHost = true;

    [SerializeField]
    private bool m_HostIsWhite = true;

    [SerializeField]
    private Toggle m_ColorToggle;
    
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

    public bool IsLocalHost => m_IsLocalHost;

    public bool HostIsWhite => m_HostIsWhite;


    private void OnEnable()
    {
        LobbyState.SetLocalHost(m_IsLocalHost);
        EventsManager.OnDeletePucks.Invoke(true);
        PuckController.ResetTurnOrder();

        EventsManager.OnLobbySnapshot.AddListener(OnLobbySnapshotReceived, true);

        m_PieceSetup = LobbySnapshot.ClonePieceSetup(m_PieceSetup);

        if (LobbyState.LatestSnapshot == null)
        {
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

        if (m_ColorToggle != null)
        {
            m_ColorToggle.isOn = m_HostIsWhite;
            m_ColorToggle.interactable = m_IsLocalHost;
        }
        NotifyCountsChanged();
    }

    private void OnDisable()
    {
        EventsManager.OnLobbySnapshot.RemoveListener(OnLobbySnapshotReceived);
    }

    public void StartButton()
    {
        if (!m_IsLocalHost)
        {
            return;
        }

        LobbySnapshot snapshot = LobbySnapshot.Create(m_PieceSetup, m_HostIsWhite);
        LobbyState.ApplySnapshot(snapshot);

        EventsManager.OnPieceSetupData.Invoke(m_PieceSetup);
        m_Phase1Canvas.SetActive(true);
        Phase1Environment.SetActive(true);
        gameObject.SetActive(false);
    }


    public bool IncreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        if (!m_IsLocalHost)
        {
            return false;
        }

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
        }
        else
        {
            setupData.BlackCount++;
        }

        NotifyCountsChanged();
        return true;
    }

    public bool DecreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        if (!m_IsLocalHost)
        {
            return false;
        }

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
        }
        else
        {
            setupData.BlackCount--;
        }

        NotifyCountsChanged();
        return true;
    }

    public void ToggleSticky(ChessPieceType pieceType, bool isSticky)
    {
        if (!m_IsLocalHost)
        {
            return;
        }

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

    public void ToggleHostColor(bool wantsWhite)
    {
        if (!m_IsLocalHost)
        {
            return;
        }

        m_HostIsWhite = wantsWhite;
        if (m_ColorToggle != null)
        {
            m_ColorToggle.isOn = m_HostIsWhite;
        }
    }


    public bool WithinWhiteCount()
    {
        return GetTotalCount(true) < MAX_PIECES_PER_COLOR;
    }

    public bool WithinBlackCount()
    {
        return GetTotalCount(false) < MAX_PIECES_PER_COLOR;
    }

    private void OnLobbySnapshotReceived(LobbySnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        m_PieceSetup = LobbySnapshot.ClonePieceSetup(snapshot.PieceSetup);
        m_HostIsWhite = snapshot.HostIsWhite;
        if (m_ColorToggle != null)
        {
            m_ColorToggle.isOn = m_HostIsWhite;
            m_ColorToggle.interactable = m_IsLocalHost;
        }

        NotifyCountsChanged();
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
        return m_PieceSetup.Sum(piece => isWhite ? piece.WhiteCount : piece.BlackCount);
    }

    private void NotifyCountsChanged()
    {
        CountsChanged?.Invoke();
    }
}
