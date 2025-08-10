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
                GameObject pieceObj = Instantiate(m_PiecePrefab, tile.transform.position, Quaternion.identity);

                // 3) Get the piece script and call SetupPiece
                Piece pieceScript = pieceObj.GetComponent<Piece>();
                if (pieceScript != null)
                {
                    pieceScript.SetupPiece(pieceType);
                    tile.SetPiece(pieceScript);
                    pieceScript.SetTile(tile);
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

            // If we found a piece, "pick it up"
            if (topmostPiece != null)
            {
                m_SelectedPiece = topmostPiece;
        
                if (m_SelectedPiece.GetCurrentTile() != null)
                {
                    m_SelectedPiece.GetCurrentTile().ClearTile();
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

            // If we dropped on a tile, place the piece there
            if (tileBelow != null)
            {
                // If there's already a piece, "capture" it (destroy it)
                if (tileBelow.HasPiece())
                {
                    // Fill out UI
                    Piece piece = tileBelow.GetCurrentPiece();
                    bool isWhitePiece = piece.IsWhite();
                    CapturedPieceUi capUi =
                        Instantiate(m_PiecePrefabUI,
                                isWhitePiece ? m_CapturedPiecesWhiteTransform : m_CapturedPiecesBlackTransform)
                            .GetComponent<CapturedPieceUi>();
                    
                    capUi.SetupCapturedUiPiece(piece.GetChessPiece());
                    
                    Destroy(tileBelow.GetCurrentPiece().gameObject);
                    tileBelow.ClearTile();
                }

                // Place the selected piece on the new tile
                tileBelow.SetPiece(m_SelectedPiece);
                m_SelectedPiece.SetTile(tileBelow);
                m_SelectedPiece.transform.position = tileBelow.transform.position;
                
                // Promotion check
                // If the piece is a Pawn AND it's on the top/bottom row
                if (m_SelectedPiece.IsPawn() && (tileBelow.GetRow() == 0 || tileBelow.GetRow() == 7))
                {
                    // Show the promotion UI
                    PromotionPanel.Instance.ShowPanel(m_SelectedPiece, tileBelow);
                }

                BoardFlipper.Flip();
            }
            else
            {
                // Otherwise, revert to original tile if it existed
                Tile oldTile = m_SelectedPiece.GetCurrentTile();
                if (oldTile != null)
                {
                    m_SelectedPiece.transform.position = oldTile.transform.position;
                    oldTile.SetPiece(m_SelectedPiece);
                }
            }

            m_SelectedPiece = null;
        }
    }
}
