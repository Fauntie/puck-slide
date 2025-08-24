using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RowData
{
    public Tile[] m_Row;
}

public class BoardController : MonoBehaviour
{
    [SerializeField]
    private GameObject m_PiecePrefab;
    [SerializeField]
    private GameObject m_PiecePrefabUI;

    [SerializeField]
    private Transform m_CapturedPiecesWhiteTransform;
    [SerializeField]
    private Transform m_CapturedPiecesBlackTransform;
    
    [SerializeField]
    private RowData[] m_Grid;

    // The piece we are currently dragging, if any
    private Piece m_SelectedPiece;
    private Vector3 m_Offset;
    private Tile m_OriginalTile;
    // Piece that was under the mouse on the initial press. Used to
    // re-select a piece if the user begins dragging after deselecting it.
    private Piece m_MouseDownPiece;
    private bool? m_LastMoveWasWhite = null;

    private readonly List<Tile> m_HighlightedTiles = new List<Tile>();
    private readonly List<Piece> m_SpawnedPieces = new List<Piece>();
    private bool m_IsDragging = false;
    private bool m_MouseDownOnPiece = false;
    private Vector3 m_MouseDownPos;
    private Tile m_WhiteCheckTile;
    private Tile m_BlackCheckTile;
    private IEventBus m_EventBus;
    private IInputSource m_InputSource;

    private struct BoardMove
    {
        public Tile From;
        public Tile To;
        public BoardMove(Tile from, Tile to)
        {
            From = from;
            To = to;
        }
    }

    private void Awake()
    {
        m_EventBus = FindObjectOfType<EventBusBootstrap>()?.Bus;
        m_InputSource = InputSourceBootstrapper.Current;
    }

    private void OnEnable()
    {
        for (int i = m_CapturedPiecesWhiteTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = m_CapturedPiecesWhiteTransform.GetChild(i);
            Destroy(child.gameObject);
        }
        
        for (int i = m_CapturedPiecesBlackTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = m_CapturedPiecesBlackTransform.GetChild(i);
            Destroy(child.gameObject);
        }

        m_EventBus?.Subscribe<bool>(EventBusEvents.TurnChanged, OnTurnChanged, true);
        m_EventBus?.Subscribe<BoardLayoutMessage>(EventBusEvents.BoardLayout, OnBoardLayout, true);
        m_LastMoveWasWhite = null;
        BuildFromState();
    }

    private void OnDisable()
    {
        m_EventBus?.Unsubscribe<bool>(EventBusEvents.TurnChanged, OnTurnChanged);
        m_EventBus?.Unsubscribe<BoardLayoutMessage>(EventBusEvents.BoardLayout, OnBoardLayout);

        foreach (Piece piece in m_SpawnedPieces)
        {
            if (piece != null)
            {
                Destroy(piece.gameObject);
            }
        }
        m_SpawnedPieces.Clear();
    }

    private void OnTurnChanged(bool _)
    {
        ClearHighlights();
        if (m_SelectedPiece != null)
        {
            HighlightMoves(m_SelectedPiece);
        }
    }

    private void OnBoardLayout(BoardLayoutMessage message)
    {
        GameState.Instance.ApplyBoardLayoutMessage(message);

        foreach (Piece piece in m_SpawnedPieces)
        {
            if (piece != null)
            {
                Destroy(piece.gameObject);
            }
        }
        m_SpawnedPieces.Clear();

        for (int i = m_CapturedPiecesWhiteTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(m_CapturedPiecesWhiteTransform.GetChild(i).gameObject);
        }

        for (int i = m_CapturedPiecesBlackTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(m_CapturedPiecesBlackTransform.GetChild(i).gameObject);
        }

        BuildFromState();
    }

    private void BuildFromState()
    {
        var layout = GameState.Instance.GetLayout();
        foreach (KeyValuePair<Position, ChessPiece> entry in layout)
        {
            Position pos = entry.Key;
            ChessPiece pieceType = entry.Value;

            if (pos.Y >= 0 && pos.Y < m_Grid.Length &&
                pos.X >= 0 && pos.X < m_Grid[pos.Y].m_Row.Length)
            {
                Tile tile = m_Grid[pos.Y].m_Row[pos.X];
                if (tile == null)
                {
                    Debug.LogWarning($"Tile at ({pos.X}, {pos.Y}) is null!");
                    continue;
                }

                GameObject pieceObj = Instantiate(m_PiecePrefab, tile.transform.position, Quaternion.identity, tile.transform);

                Piece pieceScript = pieceObj.GetComponent<Piece>();
                if (pieceScript != null)
                {
                    pieceScript.SetupPiece(pieceType);
                    tile.SetPiece(pieceScript);
                    pieceScript.SetTile(tile);
                    pieceScript.transform.SetParent(tile.transform);
                    m_SpawnedPieces.Add(pieceScript);
                }
                else
                {
                    Debug.LogError("Your m_PiecePrefab is missing a Piece script!");
                }
            }
            else
            {
                Debug.LogWarning($"Coordinates ({pos.X}, {pos.Y}) are out of range!");
            }
        }
    }

    private void Update()
    {
        // Mouse down: select piece or handle deselection
        if (m_InputSource != null && m_InputSource.GetPointerDown())
        {
            m_MouseDownPos = Camera.main.ScreenToWorldPoint(m_InputSource.GetPointerPosition());
            m_MouseDownOnPiece = false;

            Piece clickedPiece = null;
            foreach (RaycastHit2D hit in Physics2D.RaycastAll(m_MouseDownPos, Vector2.zero))
            {
                Piece piece = hit.collider.GetComponent<Piece>();
                if (piece != null &&
                    (m_LastMoveWasWhite == null || piece.IsWhite() != m_LastMoveWasWhite.Value))
                {
                    clickedPiece = piece;
                    break;
                }
            }

            if (clickedPiece != null)
            {
                m_MouseDownOnPiece = true;
                m_MouseDownPiece = clickedPiece;

                if (m_SelectedPiece == clickedPiece)
                {
                    m_SelectedPiece = null;
                    ClearHighlights();
                }
                else
                {
                    m_SelectedPiece = clickedPiece;
                    m_OriginalTile = m_SelectedPiece.GetCurrentTile();
                    HighlightMoves(m_SelectedPiece);
                }
            }
        }

        // Mouse drag
        if (m_InputSource != null && m_InputSource.GetPointer() && m_MouseDownOnPiece)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(m_InputSource.GetPointerPosition());

            if (!m_IsDragging && Vector3.Distance(mouseWorldPos, m_MouseDownPos) > 0.1f)
            {
                m_IsDragging = true;
                // If the user begins dragging after deselecting the piece in the
                // mouse-down phase, treat the drag as a fresh selection.
                if (m_SelectedPiece == null && m_MouseDownPiece != null)
                {
                    m_SelectedPiece = m_MouseDownPiece;
                    m_OriginalTile = m_SelectedPiece.GetCurrentTile();
                    HighlightMoves(m_SelectedPiece);
                }

                if (m_SelectedPiece != null && m_OriginalTile != null)
                {
                    m_SelectedPiece.transform.SetParent(null);
                    m_OriginalTile.ClearTile();
                }
            }

            if (m_IsDragging && m_SelectedPiece != null)
            {
                m_SelectedPiece.transform.position = new Vector3(mouseWorldPos.x,
                    mouseWorldPos.y,
                    -0.01f);
            }
        }

        // Mouse up
        if (m_InputSource != null && m_InputSource.GetPointerUp())
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(m_InputSource.GetPointerPosition());
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
            m_MouseDownOnPiece = false;

            Tile tileBelow = null;
            foreach (RaycastHit2D hit in hits)
            {
                Tile t = hit.collider.GetComponent<Tile>();
                if (t != null)
                {
                    tileBelow = t;
                    break;
                }
            }

            if (m_IsDragging)
            {
                bool moveMade = false;
                if (m_SelectedPiece != null)
                {
                    moveMade = tileBelow != null && m_HighlightedTiles.Contains(tileBelow) &&
                               TryMovePiece(m_SelectedPiece, m_OriginalTile, tileBelow);
                    if (!moveMade && m_OriginalTile != null)
                    {
                        m_SelectedPiece.transform.position = m_OriginalTile.transform.position;
                        m_OriginalTile.SetPiece(m_SelectedPiece);
                        m_SelectedPiece.SetTile(m_OriginalTile);
                        m_SelectedPiece.transform.SetParent(m_OriginalTile.transform);
                    }
                }
                m_IsDragging = false;
                if (!moveMade)
                {
                    ClearHighlights();
                }
                m_SelectedPiece = null;
                m_MouseDownPiece = null;
            }
            else if (m_SelectedPiece != null && tileBelow != null && m_HighlightedTiles.Contains(tileBelow))
            {
                if (TryMovePiece(m_SelectedPiece, m_OriginalTile, tileBelow))
                {
                    m_SelectedPiece = null;
                }
                m_MouseDownPiece = null;
            }
        }
    }

    private Vector2Int GetCoords(Tile tile)
    {
        int y = tile.GetRow();
        Tile[] row = m_Grid[y].m_Row;
        for (int x = 0; x < row.Length; x++)
        {
            if (row[x] == tile)
            {
                return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }

    private bool IsPathClear(Vector2Int start, Vector2Int end)
    {
        int stepX = Math.Sign(end.x - start.x);
        int stepY = Math.Sign(end.y - start.y);
        int x = start.x + stepX;
        int y = start.y + stepY;
        while (x != end.x || y != end.y)
        {
            if (m_Grid[y].m_Row[x].HasPiece())
            {
                return false;
            }
            x += stepX;
            y += stepY;
        }
        return true;
    }

    private bool IsLegalMove(Piece piece, Tile from, Tile to)
    {
        if (from == null || to == null)
            return false;

        if (to.HasPiece() && to.GetCurrentPiece().IsWhite() == piece.IsWhite())
            return false;

        Vector2Int start = GetCoords(from);
        Vector2Int end = GetCoords(to);
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        bool isWhite = piece.IsWhite();

        switch (piece.GetChessPiece())
        {
            case ChessPiece.W_Pawn:
            case ChessPiece.B_Pawn:
                int dir = isWhite ? 1 : -1;
                int startRow = isWhite ? 1 : 6;
                if (Math.Abs(dx) == 1 && dy == dir && to.HasPiece() && to.GetCurrentPiece().IsWhite() != isWhite)
                    return true;
                if (dx == 0 && !to.HasPiece())
                {
                    if (dy == dir)
                        return true;
                    if (dy == 2 * dir && start.y == startRow && IsPathClear(start, end))
                        return true;
                }
                return false;
            case ChessPiece.W_Rook:
            case ChessPiece.B_Rook:
                if (dx == 0 || dy == 0)
                    return IsPathClear(start, end);
                return false;
            case ChessPiece.W_Bishop:
            case ChessPiece.B_Bishop:
                if (Math.Abs(dx) == Math.Abs(dy))
                    return IsPathClear(start, end);
                return false;
            case ChessPiece.W_Queen:
            case ChessPiece.B_Queen:
                if (dx == 0 || dy == 0 || Math.Abs(dx) == Math.Abs(dy))
                    return IsPathClear(start, end);
                return false;
            case ChessPiece.W_Knight:
            case ChessPiece.B_Knight:
                return (Math.Abs(dx) == 1 && Math.Abs(dy) == 2) || (Math.Abs(dx) == 2 && Math.Abs(dy) == 1);
            case ChessPiece.W_King:
            case ChessPiece.B_King:
                return Math.Max(Math.Abs(dx), Math.Abs(dy)) == 1;
        }

        return false;
    }

    private void HighlightMoves(Piece piece)
    {
        ClearHighlights();
        UpdateCheckHighlights();
        Tile from = piece.GetCurrentTile();
        foreach (RowData row in m_Grid)
        {
            foreach (Tile tile in row.m_Row)
            {
                if (IsLegalMove(piece, from, tile))
                {
                    tile.Highlight(Color.yellow);
                    m_HighlightedTiles.Add(tile);
                }
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (Tile t in m_HighlightedTiles)
        {
            t.ClearHighlight();
        }
        m_HighlightedTiles.Clear();
        m_WhiteCheckTile = null;
        m_BlackCheckTile = null;
    }

    private bool TryMovePiece(Piece piece, Tile from, Tile to)
    {
        if (!IsLegalMove(piece, from, to))
            return false;

        Vector2Int start = GetCoords(from);
        Vector2Int end = GetCoords(to);

        Piece capturedPiece = to.GetCurrentPiece();
        to.SetPiece(piece);
        piece.SetTile(to);

        bool kingInCheck = IsKingInCheck(piece.IsWhite());

        if (kingInCheck)
        {
            to.SetPiece(capturedPiece);
            if (capturedPiece != null)
                capturedPiece.SetTile(to);
            from.SetPiece(piece);
            piece.SetTile(from);
            return false;
        }

        GameState.Instance.ApplyMove(new Move(new Position(start.x, start.y), new Position(end.x, end.y)));

        if (capturedPiece != null)
        {
            bool isWhitePiece = capturedPiece.IsWhite();
            CapturedPieceUi capUi =
                Instantiate(m_PiecePrefabUI,
                        isWhitePiece ? m_CapturedPiecesWhiteTransform : m_CapturedPiecesBlackTransform)
                    .GetComponent<CapturedPieceUi>();
            capUi.SetupCapturedUiPiece(capturedPiece.GetChessPiece());
            m_SpawnedPieces.Remove(capturedPiece);
            Destroy(capturedPiece.gameObject);
        }

        piece.transform.position = to.transform.position;
        piece.transform.SetParent(to.transform);

        if (piece.IsPawn() && (to.GetRow() == 0 || to.GetRow() == 7))
        {
            PromotionPanel.Instance.ShowPanel(piece, to);
        }

        m_LastMoveWasWhite = piece.IsWhite();
        BoardFlipper.FlipCamera();
        m_EventBus?.Publish(EventBusEvents.TurnChanged, !m_LastMoveWasWhite.Value);

        // Highlight the king if it has been put in check by the latest move
        UpdateCheckHighlights();

        EvaluateGameState(!m_LastMoveWasWhite.Value);
        return true;
    }

    private void UpdateCheckHighlights()
    {
        Tile newWhite = IsKingInCheck(true) ? FindKingTile(true) : null;
        if (m_WhiteCheckTile != newWhite)
        {
            if (m_WhiteCheckTile != null)
            {
                m_WhiteCheckTile.ClearHighlight();
                m_HighlightedTiles.Remove(m_WhiteCheckTile);
            }
            if (newWhite != null)
            {
                newWhite.Highlight(Color.red);
                m_HighlightedTiles.Add(newWhite);
            }
            m_WhiteCheckTile = newWhite;
        }

        Tile newBlack = IsKingInCheck(false) ? FindKingTile(false) : null;
        if (m_BlackCheckTile != newBlack)
        {
            if (m_BlackCheckTile != null)
            {
                m_BlackCheckTile.ClearHighlight();
                m_HighlightedTiles.Remove(m_BlackCheckTile);
            }
            if (newBlack != null)
            {
                newBlack.Highlight(Color.red);
                m_HighlightedTiles.Add(newBlack);
            }
            m_BlackCheckTile = newBlack;
        }
    }

    private Tile FindKingTile(bool isWhite)
    {
        foreach (RowData row in m_Grid)
        {
            foreach (Tile tile in row.m_Row)
            {
                if (tile.HasPiece())
                {
                    Piece p = tile.GetCurrentPiece();
                    if (p.IsWhite() == isWhite &&
                        (p.GetChessPiece() == ChessPiece.W_King || p.GetChessPiece() == ChessPiece.B_King))
                    {
                        return tile;
                    }
                }
            }
        }
        return null;
    }

    private bool IsKingInCheck(bool isWhite)
    {
        Tile kingTile = FindKingTile(isWhite);
        if (kingTile == null)
            return false;

        foreach (RowData row in m_Grid)
        {
            foreach (Tile tile in row.m_Row)
            {
                if (tile.HasPiece())
                {
                    Piece piece = tile.GetCurrentPiece();
                    if (piece.IsWhite() != isWhite && IsLegalMove(piece, tile, kingTile))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private List<BoardMove> GetLegalMoves(bool isWhite)
    {
        List<BoardMove> moves = new List<BoardMove>();
        for (int y = 0; y < m_Grid.Length; y++)
        {
            Tile[] row = m_Grid[y].m_Row;
            for (int x = 0; x < row.Length; x++)
            {
                Tile from = row[x];
                if (!from.HasPiece())
                    continue;
                Piece piece = from.GetCurrentPiece();
                if (piece.IsWhite() != isWhite)
                    continue;

                for (int y2 = 0; y2 < m_Grid.Length; y2++)
                {
                    Tile[] row2 = m_Grid[y2].m_Row;
                    for (int x2 = 0; x2 < row2.Length; x2++)
                    {
                        Tile to = row2[x2];
                        if (!IsLegalMove(piece, from, to))
                            continue;

                        Piece captured = to.GetCurrentPiece();
                        to.SetPiece(piece);
                        piece.SetTile(to);
                        from.ClearTile();

                        bool inCheck = IsKingInCheck(isWhite);

                        from.SetPiece(piece);
                        piece.SetTile(from);
                        to.SetPiece(captured);
                        if (captured != null)
                            captured.SetTile(to);

                          if (!inCheck)
                          {
                              moves.Add(new BoardMove(from, to));
                          }
                    }
                }
            }
        }
        return moves;
    }

    private void EvaluateGameState(bool isWhiteTurn)
    {
        bool inCheck = IsKingInCheck(isWhiteTurn);
        List<BoardMove> legalMoves = GetLegalMoves(isWhiteTurn);

        if (legalMoves.Count == 0)
        {
            if (inCheck)
            {
                m_EventBus?.Publish(EventBusEvents.GameState, "checkmate");
            }
            else
            {
                m_EventBus?.Publish(EventBusEvents.GameState, "draw");
            }
        }
        else if (inCheck)
        {
            m_EventBus?.Publish(EventBusEvents.GameState, "check");
        }
    }
}

