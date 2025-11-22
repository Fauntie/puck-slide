using System;
using System.Collections.Generic;
using System.Linq;

public class PieceSetupState
{
    public const int MaxPiecesPerColor = 16;

    private readonly Dictionary<ChessPieceType, PieceSetupData> m_PieceMap;
    private readonly List<PieceSetupData> m_DefaultSetup;

    public PieceSetupState(IEnumerable<PieceSetupData> initialSetup)
    {
        if (initialSetup == null)
        {
            throw new ArgumentNullException(nameof(initialSetup));
        }

        m_DefaultSetup = CloneSetup(initialSetup);
        m_PieceMap = m_DefaultSetup.ToDictionary(piece => piece.Type, piece => piece.Clone());
    }

    public IReadOnlyList<PieceSetupData> GetSnapshot()
    {
        return CloneSetup(m_PieceMap.Values);
    }

    public void Reset()
    {
        m_PieceMap.Clear();
        foreach (PieceSetupData data in m_DefaultSetup)
        {
            m_PieceMap[data.Type] = data.Clone();
        }
    }

    public bool IncreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        if (!m_PieceMap.TryGetValue(pieceType, out PieceSetupData setupData))
        {
            return false;
        }

        if (!CanIncrease(pieceType, isWhite))
        {
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

        return true;
    }

    public bool DecreaseCount(ChessPieceType pieceType, bool isWhite)
    {
        if (!m_PieceMap.TryGetValue(pieceType, out PieceSetupData setupData))
        {
            return false;
        }

        if (!CanDecrease(pieceType, isWhite))
        {
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

        return true;
    }

    public bool CanIncrease(ChessPieceType pieceType, bool isWhite)
    {
        if (!m_PieceMap.TryGetValue(pieceType, out PieceSetupData setupData))
        {
            return false;
        }

        int currentCount = isWhite ? setupData.WhiteCount : setupData.BlackCount;
        if (currentCount >= MaxPiecesPerColor)
        {
            return false;
        }

        int totalCount = GetTotalCount(isWhite);
        return totalCount + 1 <= MaxPiecesPerColor;
    }

    public bool CanDecrease(ChessPieceType pieceType, bool isWhite)
    {
        if (!m_PieceMap.TryGetValue(pieceType, out PieceSetupData setupData))
        {
            return false;
        }

        int currentCount = isWhite ? setupData.WhiteCount : setupData.BlackCount;
        return currentCount > 0;
    }

    public int GetCount(ChessPieceType pieceType, bool isWhite)
    {
        if (!m_PieceMap.TryGetValue(pieceType, out PieceSetupData setupData))
        {
            return 0;
        }

        return isWhite ? setupData.WhiteCount : setupData.BlackCount;
    }

    public bool GetSticky(ChessPieceType pieceType)
    {
        if (!m_PieceMap.TryGetValue(pieceType, out PieceSetupData setupData))
        {
            return false;
        }

        return setupData.Sticky;
    }

    public bool ToggleSticky(ChessPieceType pieceType, bool isSticky)
    {
        if (!m_PieceMap.TryGetValue(pieceType, out PieceSetupData setupData))
        {
            return false;
        }

        setupData.Sticky = isSticky;
        return true;
    }

    public int GetTotalCount(bool isWhite)
    {
        return m_PieceMap.Values.Sum(piece => isWhite ? piece.WhiteCount : piece.BlackCount);
    }

    public static List<PieceSetupData> CreateDefaultSetup()
    {
        return new List<PieceSetupData>
        {
            new PieceSetupData { Type = ChessPieceType.Pawn,   WhiteCount=4, BlackCount=4, Sticky=true },
            new PieceSetupData { Type = ChessPieceType.Knight, WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.Bishop, WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.Rook,   WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.Queen,  WhiteCount=1, BlackCount=1, Sticky=false },
            new PieceSetupData { Type = ChessPieceType.King,   WhiteCount=1, BlackCount=1, Sticky=true }
        };
    }

    private static List<PieceSetupData> CloneSetup(IEnumerable<PieceSetupData> source)
    {
        return source.Select(piece => piece.Clone()).ToList();
    }
}
