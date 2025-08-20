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

    private struct Move
    {
        public Tile From;
        public Tile To;
        public Move(Tile from, Tile to)
        {
            From = from;
            To = to;
        }
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
        
        EventsManager.OnBoardLayout.AddListener(OnBoardLayout, true);
    }
    
    private void OnDisable()
    {
        EventsManager.OnBoardLayout.RemoveListener(OnBoardLayout);
        
        Piece[] gamePieces = FindObjectsOfType<Piece>();
        foreach (Piece piece in gamePieces)
        {
            Destroy(piece.gameObject);
        }
    }

    private void OnBoardLayout(Dictionary<Vector2Int, ChessPiece> layout)
    {
        if (layout == null)
        {
            return;
        }
        
        foreach (KeyValuePair<Vector2Int, ChessPiece> entry in layout)
        {
            Vector2Int coords = entry.Key;      // e.g. (x=0, y=1)
            ChessPiece pieceType = entry.Value; // e.g. ChessPiece.B_Pawn

            // Safety check: make sure coords are in range
            if (coords.y >= 0 && coords.y < m_Grid.Length &&
                coords.x >= 0 && coords.x < m_Grid[coords.y].m_Row.Length)
            {
                // 1) Get the tile at (x,y)
                Tile tile = m_Grid[coords.y].m_Row[coords.x];
                if (tile == null)
                {
                    Debug.LogWarning($"Tile at {coords} is null!");
                    continue;
                }

                // 2) Instantiate the piece prefab at the tile's position
                GameObject pieceObj = Instantiate(m_PiecePrefab, tile.transform.position, Quaternion.identity, tile.transform);

                // 3) Get the piece script and call SetupPiece
                Piece pieceScript = pieceObj.GetComponent<Piece>();
                if (pieceScript != null)
                {
                    pieceScript.SetupPiece(pieceType);
                    tile.SetPiece(pieceScript);
                    pieceScript.SetTile(tile);
                    pieceScript.transform.SetParent(tile.transform);
                }
                else
                {
                    Debug.LogError("Your m_PiecePrefab is missing a Piece script!");
                }
            }
            else
            {
                Debug.LogWarning($"Coordinates {coords} are out of range!");
            }
        }
    }

    private void Update()
    {
        // 1) Mouse down
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Collect everything under the mouse
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

            Piece topmostPiece = null;
            int topSortingOrder = int.MinValue;

            foreach (RaycastHit2D hit in hits)
            {
                Piece piece = hit.collider.GetComponent<Piece>();
                if (piece != null)
                {
                    int order = piece.GetComponent<SpriteRenderer>().sortingOrder;
                    if (order > topSortingOrder)
                    {
                        topmostPiece = piece;
                        topSortingOrder = order;
                    }
                }
            }

            // If we found a piece, check turn and "pick it up"
            if (topmostPiece != null && topmostPiece.IsWhite() == EventsManager.IsWhiteTurn)
            {
                m_SelectedPiece = topmostPiece;
                m_OriginalTile = m_SelectedPiece.GetCurrentTile();

                if (m_OriginalTile != null)
                {
                    m_SelectedPiece.transform.SetParent(null);
                    m_OriginalTile.ClearTile();
                }
            }
        }

        // 2) Mouse drag
        if (Input.GetMouseButton(0) && m_SelectedPiece != null)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    
            // Just place the piece directly at the cursor (plus a tiny Z offset).
            m_SelectedPiece.transform.position = new Vector3(mouseWorldPos.x,
                mouseWorldPos.y,
                -0.01f);
        }

        // 3) Mouse up: try to drop onto a tile
        if (Input.GetMouseButtonUp(0) && m_SelectedPiece != null)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

            Tile tileBelow = null;

            // Look for any tile collider under the mouse
            foreach (RaycastHit2D hit in hits)
            {
                Tile t = hit.collider.GetComponent<Tile>();
                if (t != null)
                {
                    tileBelow = t;
                    break; // we'll just pick the first tile we find
                }
            }

            bool moveMade = false;
            if (tileBelow != null && IsLegalMove(m_SelectedPiece, m_OriginalTile, tileBelow))
            {
                // Simulate the move
                Piece capturedPiece = tileBelow.GetCurrentPiece();
                tileBelow.SetPiece(m_SelectedPiece);
                m_SelectedPiece.SetTile(tileBelow);

                bool kingInCheck = IsKingInCheck(m_SelectedPiece.IsWhite());

                if (kingInCheck)
                {
                    // Revert illegal move
                    tileBelow.SetPiece(capturedPiece);
                    if (capturedPiece != null)
                    {
                        capturedPiece.SetTile(tileBelow);
                    }
                    m_OriginalTile.SetPiece(m_SelectedPiece);
                    m_SelectedPiece.SetTile(m_OriginalTile);
                }
                else
                {
                    // Finalize capture if any
                    if (capturedPiece != null)
                    {
                        bool isWhitePiece = capturedPiece.IsWhite();
                        CapturedPieceUi capUi =
                            Instantiate(m_PiecePrefabUI,
                                    isWhitePiece ? m_CapturedPiecesWhiteTransform : m_CapturedPiecesBlackTransform)
                                .GetComponent<CapturedPieceUi>();
                        capUi.SetupCapturedUiPiece(capturedPiece.GetChessPiece());
                        Destroy(capturedPiece.gameObject);
                    }

                    // Place piece visually
                    m_SelectedPiece.transform.position = tileBelow.transform.position;
                    m_SelectedPiece.transform.SetParent(tileBelow.transform);

                    if (m_SelectedPiece.IsPawn() && (tileBelow.GetRow() == 0 || tileBelow.GetRow() == 7))
                    {
                        PromotionPanel.Instance.ShowPanel(m_SelectedPiece, tileBelow);
                    }

                    EventsManager.ToggleTurn();
                    moveMade = true;

                    // Evaluate opponent's state
                    EvaluateGameState(EventsManager.IsWhiteTurn);
                }
            }

            if (!moveMade && m_OriginalTile != null)
            {
                m_SelectedPiece.transform.position = m_OriginalTile.transform.position;
                m_OriginalTile.SetPiece(m_SelectedPiece);
                m_SelectedPiece.SetTile(m_OriginalTile);
                m_SelectedPiece.transform.SetParent(m_OriginalTile.transform);
            }

            m_SelectedPiece = null;
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

    private List<Move> GetLegalMoves(bool isWhite)
    {
        List<Move> moves = new List<Move>();
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
                            moves.Add(new Move(from, to));
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
        List<Move> legalMoves = GetLegalMoves(isWhiteTurn);

        if (legalMoves.Count == 0)
        {
            if (inCheck)
            {
                EventsManager.OnGameState.Invoke("checkmate");
            }
            else
            {
                EventsManager.OnGameState.Invoke("draw");
            }
        }
        else if (inCheck)
        {
            EventsManager.OnGameState.Invoke("check");
        }
    }
}

