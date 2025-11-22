using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    private Piece m_PressedPiece;
    private Piece m_DraggedPiece;
    private Tile m_DragOriginTile;
    private Piece m_CurrentSelection;
    private Tile m_CurrentSelectionTile;
    private readonly List<Tile> m_HighlightedTiles = new List<Tile>();
    private Tile m_PointerDownTile;
    private bool? m_LastMoveWasWhite = null;

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
        m_LastMoveWasWhite = null;
    }
    
    private void OnDisable()
    {
        EventsManager.OnBoardLayout.RemoveListener(OnBoardLayout);

        ClearSelection();
        m_DraggedPiece = null;
        m_DragOriginTile = null;
        m_PressedPiece = null;

        Piece[] gamePieces = FindObjectsOfType<Piece>();
        foreach (Piece piece in gamePieces)
        {
            Destroy(piece.gameObject);
        }
    }

    private void OnBoardLayout(Dictionary<Vector2Int, ChessPiece> layout)
    {
        ClearSelection();

        ClearBoardPieces();

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

    private void ClearBoardPieces()
    {
        foreach (RowData rowData in m_Grid)
        {
            foreach (Tile tile in rowData.m_Row)
            {
                if (tile == null)
                {
                    continue;
                }

                Piece existingPiece = tile.GetCurrentPiece();
                if (existingPiece != null)
                {
                    Destroy(existingPiece.gameObject);
                    tile.ClearTile();
                }
            }
        }
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

            Piece topmostPiece = null;
            int topSortingOrder = int.MinValue;
            Tile firstTile = null;

            foreach (RaycastHit2D hit in hits)
            {
                if (firstTile == null)
                {
                    Tile tileHit = hit.collider.GetComponent<Tile>();
                    if (tileHit != null)
                    {
                        firstTile = tileHit;
                    }
                }

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

            m_PointerDownTile = firstTile;

            if (topmostPiece != null && (m_LastMoveWasWhite == null || topmostPiece.IsWhite() != m_LastMoveWasWhite.Value))
            {
                Tile originTile = topmostPiece.GetCurrentTile();
                if (originTile != null)
                {
                    SetSelection(topmostPiece, originTile);
                    m_PressedPiece = topmostPiece;
                    m_DragOriginTile = originTile;
                }
            }
        }

        if (Input.GetMouseButton(0) && m_PressedPiece != null)
        {
            if (m_DraggedPiece == null)
            {
                m_DraggedPiece = m_PressedPiece;
                m_PressedPiece = null;

                if (m_DragOriginTile != null && m_DragOriginTile.GetCurrentPiece() == m_DraggedPiece)
                {
                    m_DragOriginTile.ClearTile();
                }

                m_DraggedPiece.transform.SetParent(null);
            }

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            m_DraggedPiece.transform.position = new Vector3(mouseWorldPos.x,
                mouseWorldPos.y,
                -0.01f);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

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

            if (m_DraggedPiece != null)
            {
                HandleDragRelease(tileBelow);
            }
            else
            {
                HandleClickRelease(tileBelow);
            }

            m_PointerDownTile = null;
            m_PressedPiece = null;
        }
    }

    private void HandleDragRelease(Tile tileBelow)
    {
        Piece piece = m_DraggedPiece;
        Tile originTile = m_DragOriginTile;

        if (piece == null || originTile == null)
        {
            m_DraggedPiece = null;
            m_DragOriginTile = null;
            return;
        }

        bool releasedOnOrigin = tileBelow == originTile;

        if (tileBelow != null && IsLegalMove(piece, originTile, tileBelow) && !releasedOnOrigin)
        {
            ExecuteMove(piece, originTile, tileBelow);
            ClearSelection();
        }
        else
        {
            AttachPieceToTile(piece, originTile);

            if (releasedOnOrigin && m_CurrentSelection == piece)
            {
                m_CurrentSelectionTile = originTile;
                RefreshHighlights(piece, originTile);
            }
            else
            {
                ClearSelection();
            }
        }

        m_DraggedPiece = null;
        m_DragOriginTile = null;
    }

    private void HandleClickRelease(Tile tileBelow)
    {
        if (m_CurrentSelection == null || m_CurrentSelectionTile == null)
        {
            return;
        }

        if (tileBelow == m_CurrentSelectionTile)
        {
            RefreshHighlights(m_CurrentSelection, m_CurrentSelectionTile);
            return;
        }

        if (tileBelow != null && tileBelow == m_PointerDownTile &&
            m_HighlightedTiles.Contains(tileBelow) &&
            IsLegalMove(m_CurrentSelection, m_CurrentSelectionTile, tileBelow))
        {
            ExecuteMove(m_CurrentSelection, m_CurrentSelectionTile, tileBelow);
            ClearSelection();
            return;
        }

        ClearSelection();
    }

    private void SetSelection(Piece piece, Tile originTile)
    {
        if (piece == null || originTile == null)
        {
            ClearSelection();
            return;
        }

        if (m_CurrentSelection != piece)
        {
            ClearHighlights();
        }

        m_CurrentSelection = piece;
        m_CurrentSelectionTile = originTile;
        RefreshHighlights(piece, originTile);
    }

    private void RefreshHighlights(Piece piece, Tile originTile)
    {
        ClearHighlights();

        if (piece == null || originTile == null)
        {
            return;
        }

        foreach (RowData rowData in m_Grid)
        {
            foreach (Tile tile in rowData.m_Row)
            {
                if (tile == null || tile == originTile)
                {
                    continue;
                }

                if (IsLegalMove(piece, originTile, tile))
                {
                    tile.ShowHighlight();
                    m_HighlightedTiles.Add(tile);
                }
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (Tile tile in m_HighlightedTiles)
        {
            if (tile != null)
            {
                tile.HideHighlight();
            }
        }

        m_HighlightedTiles.Clear();
    }

    private void ClearSelection()
    {
        ClearHighlights();
        m_CurrentSelection = null;
        m_CurrentSelectionTile = null;
    }

    private void AttachPieceToTile(Piece piece, Tile tile)
    {
        if (tile == null || piece == null)
        {
            return;
        }

        tile.SetPiece(piece);
        piece.SetTile(tile);
        piece.transform.position = tile.transform.position;
        piece.transform.SetParent(tile.transform);
    }

    private void ExecuteMove(Piece piece, Tile fromTile, Tile toTile)
    {
        if (piece == null || toTile == null)
        {
            return;
        }

        if (fromTile != null && fromTile.GetCurrentPiece() == piece)
        {
            fromTile.ClearTile();
        }

        if (toTile.HasPiece())
        {
            Piece pieceToCapture = toTile.GetCurrentPiece();
            bool isWhitePiece = pieceToCapture.IsWhite();
            CapturedPieceUi capUi =
                Instantiate(m_PiecePrefabUI,
                        isWhitePiece ? m_CapturedPiecesWhiteTransform : m_CapturedPiecesBlackTransform)
                    .GetComponent<CapturedPieceUi>();
            capUi.SetupCapturedUiPiece(pieceToCapture.GetChessPiece());

            Destroy(pieceToCapture.gameObject);
            toTile.ClearTile();
        }

        AttachPieceToTile(piece, toTile);

        if (piece.IsPawn() && (toTile.GetRow() == 0 || toTile.GetRow() == 7))
        {
            PromotionPanel.Instance.ShowPanel(piece, toTile);
        }

        m_LastMoveWasWhite = piece.IsWhite();
        BoardFlipper.FlipCamera();
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
}
