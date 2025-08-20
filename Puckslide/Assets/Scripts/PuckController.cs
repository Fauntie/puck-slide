using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuckController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D m_Rigidbody;
    [SerializeField]
    private LineRenderer m_LineRenderer;

    [SerializeField]
    private LineRenderer m_DragLimitRenderer;

    [SerializeField]
    private LineRenderer m_TrajectoryRenderer;

    [SerializeField]
    private SpriteRenderer m_SpriteRenderer;

    [SerializeField]
    private Sprite[] m_Sprites;

    [SerializeField]
    private float m_MaxDragMultiplier = 4f; // allowed pull distance in puck diameters

    private Vector3 m_DragStartPos;
    private Camera m_Camera;

    private PuckFriction m_PuckFriction;
    private CircleCollider2D m_Collider;
    private float m_MaxDragDistance;

    private bool m_IsSticky;

    private const float STOP_THRESHOLD = 0.05f;
    [SerializeField]
    // Further reduced max shoot force by 5% to limit shot strength
    private float m_MaxShootForce = 9.12f;

    [SerializeField]
    private float m_MinLineWidth = 0.05f;

    [SerializeField]
    private float m_MaxLineWidth = 0.3f;
    private static bool s_IsWhiteTurn = true;
    private static PuckController s_ActivePuck;
    private bool m_IsSelected;

    // Entry lines for both sides of the board.
    private float m_BottomEntryY;
    private float m_TopEntryY;
    private float m_HalfBoardY;

    private Vector3 m_StartPosition;
    private bool m_HasReachedBoard;

    private GridManager m_GridManager;

    // Cached board data used for highlighting legal moves.
    private static readonly List<Tile> s_HighlightedTiles = new List<Tile>();
    private static Dictionary<Vector2Int, Tile> s_TileMap;
    private static float s_TileSize;
    private static Vector2 s_BoardOrigin;

    private void Awake()
    {
        m_Camera = Camera.main;
        m_Rigidbody.freezeRotation = true;
        m_TrajectoryRenderer ??= GetComponent<LineRenderer>();
        if (m_TrajectoryRenderer == null)
        {
            Debug.LogWarning("PuckController requires a LineRenderer component.", this);
        }
        m_PuckFriction = GetComponent<PuckFriction>();
        m_Collider = GetComponent<CircleCollider2D>();
        m_GridManager = FindObjectOfType<GridManager>();

        if (m_Collider != null)
        {
            float diameter = m_Collider.radius * 2f * transform.localScale.x;
            m_MaxDragDistance = diameter * m_MaxDragMultiplier;
        }

        if (m_TrajectoryRenderer != null)
        {
            m_TrajectoryRenderer.enabled = false;
            m_TrajectoryRenderer.startColor = Color.red;
            m_TrajectoryRenderer.endColor = Color.red;
            m_TrajectoryRenderer.textureMode = LineTextureMode.Tile;
            m_TrajectoryRenderer.positionCount = 0;
            m_TrajectoryRenderer.startWidth = m_MinLineWidth;
            m_TrajectoryRenderer.endWidth = m_MinLineWidth;

            // Create a small two-pixel texture where one pixel is opaque and
            // the next is transparent. When tiled along the line this produces
            // a densely dotted pattern instead of widely spaced dots.
            var dotTex = new Texture2D(2, 1, TextureFormat.ARGB32, false);
            var pixels = new[]
            {
                Color.white,
                new Color(1f, 1f, 1f, 0f)
            };
            dotTex.SetPixels(pixels);
            dotTex.filterMode = FilterMode.Point;
            dotTex.wrapMode = TextureWrapMode.Repeat;
            dotTex.Apply();

            var dottedMat = new Material(Shader.Find("Sprites/Default"));
            dottedMat.mainTexture = dotTex;
            m_TrajectoryRenderer.material = dottedMat;
            // Increase the texture tiling so the two-pixel pattern repeats
            // more often, giving the appearance of many closely spaced dots
            // instead of longer dashes.
            m_TrajectoryRenderer.material.mainTextureScale = new Vector2(4f, 1f);
        }


        UpdateBoardEntryLines();
        m_StartPosition = transform.position;
    }

    // Recalculate the entry lines at the top and bottom of the board.
    public void UpdateBoardEntryLines()
    {
        Transform board = BoardFlipper.GetBoardTransform();
        if (board == null)
        {
            m_BottomEntryY = 0f;
            m_TopEntryY = 0f;
            m_HalfBoardY = 0f;
            return;
        }

        // Prefer the BoardTrigger collider to avoid including launch-area tiles
        // when calculating the board bounds.
        Transform trigger = board.Find("BoardTrigger");
        if (trigger != null && trigger.TryGetComponent(out BoxCollider2D box))
        {
            Bounds bounds = box.bounds;
            m_BottomEntryY = bounds.min.y;
            m_TopEntryY = bounds.max.y;
            m_HalfBoardY = (m_TopEntryY + m_BottomEntryY) * 0.5f;
            return;
        }

        // Fallback to scanning tile positions.
        Tile[] tiles = board.GetComponentsInChildren<Tile>();
        if (tiles.Length == 0)
        {
            Debug.LogWarning("PuckController could not locate any board tiles to determine board entry.", this);
            m_BottomEntryY = 0f;
            m_TopEntryY = 0f;
            m_HalfBoardY = 0f;
            return;
        }

        float minY = tiles[0].transform.position.y;
        float maxY = minY;
        float halfHeight;
        SpriteRenderer sr = tiles[0].GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            halfHeight = sr.bounds.extents.y;
        }
        else
        {
            halfHeight = tiles[0].transform.localScale.y * 0.5f;
        }

        for (int i = 1; i < tiles.Length; i++)
        {
            float y = tiles[i].transform.position.y;
            if (y < minY)
            {
                minY = y;
            }
            if (y > maxY)
            {
                maxY = y;
            }
        }

        m_BottomEntryY = minY - halfHeight;
        m_TopEntryY = maxY + halfHeight;
        m_HalfBoardY = (m_TopEntryY + m_BottomEntryY) * 0.5f;
    }

    private void OnEnable()
    {
        EventsManager.OnDeletePucks.AddListener(OnDelete);
        EventsManager.OnTurnChanged.AddListener(OnTurnChanged, true);
        EventsManager.OnPuckSpawned.Invoke(m_Rigidbody);
    }

    private void OnDisable()
    {
        EventsManager.OnDeletePucks.RemoveListener(OnDelete);
        EventsManager.OnTurnChanged.RemoveListener(OnTurnChanged);
        EventsManager.OnPuckDespawned.Invoke(m_Rigidbody);
        if (s_ActivePuck == this)
        {
            s_ActivePuck = null;
        }
    }

    private void OnTurnChanged(bool _)
    {
        UpdateBoardEntryLines();
    }

    private void OnDelete(bool delete)
    {
        if (delete)
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        if (m_IsSticky && m_Rigidbody.bodyType == RigidbodyType2D.Dynamic)
        {
            if (m_Rigidbody.velocity.magnitude <= STOP_THRESHOLD)
            {
                m_Rigidbody.bodyType = RigidbodyType2D.Static;
            }
        }
    }

    private void FixedUpdate()
    {

        // Keep pawns on their starting side during phase 1 once they reach the board.
        if (m_HasReachedBoard && !Phase2Manager.IsPhase2Active &&

            (ChessPiece == ChessPiece.W_Pawn || ChessPiece == ChessPiece.B_Pawn))
        {
            if (m_Rigidbody.position.y > m_HalfBoardY && m_Rigidbody.velocity.y > 0f)
            {
                Vector2 pos = m_Rigidbody.position;
                pos.y = m_HalfBoardY;
                m_Rigidbody.position = pos;
                Vector2 vel = m_Rigidbody.velocity;
                vel.y = -vel.y;      // mirror far-wall bounce
                m_Rigidbody.velocity = vel;
            }
        }
    }


    public void Init(ChessPieceType chessPieceType, bool isSticky, bool isWhite)
    {
        switch (chessPieceType)
        {
            case ChessPieceType.Pawn:
                ChessPiece = isWhite ? ChessPiece.W_Pawn : ChessPiece.B_Pawn;
                m_SpriteRenderer.sprite = isWhite ? m_Sprites[9] : m_Sprites[3];
                break;
            case ChessPieceType.Knight:
                ChessPiece = isWhite ? ChessPiece.W_Knight : ChessPiece.B_Knight;
                m_SpriteRenderer.sprite = isWhite ? m_Sprites[8] : m_Sprites[2];
                break;
            case ChessPieceType.Bishop:
                ChessPiece = isWhite ? ChessPiece.W_Bishop : ChessPiece.B_Bishop;
                m_SpriteRenderer.sprite = isWhite ? m_Sprites[6] : m_Sprites[0];
                break;
            case ChessPieceType.Rook:
                ChessPiece = isWhite ? ChessPiece.W_Rook : ChessPiece.B_Rook;
                m_SpriteRenderer.sprite = isWhite ? m_Sprites[11] : m_Sprites[5];
                break;
            case ChessPieceType.Queen:
                ChessPiece = isWhite ? ChessPiece.W_Queen : ChessPiece.B_Queen;
                m_SpriteRenderer.sprite = isWhite ? m_Sprites[10] : m_Sprites[4];
                break;
            case ChessPieceType.King:
                ChessPiece = isWhite ? ChessPiece.W_King : ChessPiece.B_King;
                m_SpriteRenderer.sprite = isWhite ? m_Sprites[7] : m_Sprites[1];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(chessPieceType), chessPieceType, null);
        }

        m_IsSticky = isSticky;
        m_Rigidbody.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnMouseDown()
    {

        if (IsWhitePiece != s_IsWhiteTurn || (s_ActivePuck != null && s_ActivePuck != this) || m_HasReachedBoard || m_Rigidbody.velocity.magnitude > STOP_THRESHOLD)

        {
            m_IsSelected = false;
            return;
        }

        s_ActivePuck = this;
        m_IsSelected = true;
        m_StartPosition = transform.position;
        if (m_IsSticky && m_Rigidbody.bodyType == RigidbodyType2D.Static)
        {
            m_Rigidbody.bodyType = RigidbodyType2D.Dynamic;
        }

        m_DragStartPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        
        if (m_LineRenderer != null)
        {
            m_LineRenderer.enabled = true;
            m_LineRenderer.positionCount = 2;
            Vector3 start = transform.position;
            start.z = 0;
            m_LineRenderer.SetPosition(0, start);
            m_LineRenderer.SetPosition(1, start);
            m_LineRenderer.startColor = Color.green;
            m_LineRenderer.endColor = Color.green;
            m_LineRenderer.startWidth = m_MinLineWidth;
            m_LineRenderer.endWidth = m_MinLineWidth;
            m_LineRenderer.numCapVertices = 8;

            DrawDragLimitCircle(start);
        }

        if (m_TrajectoryRenderer != null)
        {
            m_TrajectoryRenderer.enabled = true;
            m_TrajectoryRenderer.positionCount = 0;
            m_TrajectoryRenderer.startWidth = m_MinLineWidth;
            m_TrajectoryRenderer.endWidth = m_MinLineWidth;
        }
        m_GridManager?.UpdatePieceLayout();
        HighlightLegalMoves();
    }

    private void OnMouseDrag()
    {
        if (!m_IsSelected)
        {
            return;
        }

        Vector3 dragPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        dragPos.z = 0;

        Vector3 puckCenter = transform.position;
        puckCenter.z = 0;

        Vector3 offset = dragPos - puckCenter;
        Vector3 clampedOffset = Vector3.ClampMagnitude(offset, m_MaxDragDistance);
        Vector3 endPos = puckCenter + clampedOffset;

        float powerRatio = clampedOffset.magnitude / m_MaxDragDistance;
        Vector3 direction = (puckCenter - endPos).normalized;
        Vector3 dragVector = direction * (powerRatio * m_MaxShootForce);

        if (m_TrajectoryRenderer != null && m_TrajectoryRenderer.enabled)
        {
            UpdateTrajectory(dragVector);
        }

        if (m_LineRenderer != null && m_LineRenderer.enabled)
        {
            Color powerColor = Color.Lerp(Color.green, Color.red, powerRatio);
            m_LineRenderer.startColor = powerColor;
            m_LineRenderer.endColor = powerColor;

            m_LineRenderer.SetPosition(0, puckCenter);
            m_LineRenderer.SetPosition(1, endPos);

            float endWidth = Mathf.Lerp(m_MinLineWidth, m_MaxLineWidth, powerRatio);
            m_LineRenderer.startWidth = m_MinLineWidth;
            m_LineRenderer.endWidth = endWidth;

            DrawDragLimitCircle(puckCenter);
        }
    }

    private void OnMouseUp()
    {
        if (!m_IsSelected)
        {
            return;
        }

        Vector3 dragEndPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dragVector = (m_DragStartPos - dragEndPos);
        float dragDistance = Mathf.Min(dragVector.magnitude, m_MaxDragDistance);
        Vector2 dragDirection = dragVector.normalized;
        float powerRatio = dragDistance / m_MaxDragDistance;
        float force = powerRatio * m_MaxShootForce;

        m_Rigidbody.bodyType = RigidbodyType2D.Dynamic;
        m_Rigidbody.AddForce(dragDirection * force, ForceMode2D.Impulse);

        if (m_LineRenderer != null)
        {
            m_LineRenderer.enabled = false;
        }

        if (m_DragLimitRenderer != null)
        {
            m_DragLimitRenderer.enabled = false;
        }

        if (m_TrajectoryRenderer != null)
        {
            m_TrajectoryRenderer.enabled = false;
        }
        m_IsSelected = false;

        ClearHighlights();

        StartCoroutine(WaitForPuckStopped());
    }

    private void UpdateTrajectory(Vector2 force)
    {
        if (m_TrajectoryRenderer == null)
        {
            return;
        }

        int steps = 30;
        float timeStep = Time.fixedDeltaTime;
        float friction = m_PuckFriction != null ? m_PuckFriction.Friction : 0.98f;
        Vector2 position = m_Rigidbody.position;
        Vector2 velocity = force / m_Rigidbody.mass;

        m_TrajectoryRenderer.positionCount = steps + 1;
        m_TrajectoryRenderer.SetPosition(0, new Vector3(position.x, position.y, 0f));
        for (int i = 1; i <= steps; i++)
        {
            position += velocity * timeStep;
            m_TrajectoryRenderer.SetPosition(i, new Vector3(position.x, position.y, 0f));
            velocity *= friction;
        }
    }

    private void DrawDragLimitCircle(Vector3 center)
    {
        if (m_DragLimitRenderer == null)
        {
            return;
        }

        int segments = 32;
        m_DragLimitRenderer.loop = true;
        m_DragLimitRenderer.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * m_MaxDragDistance;
            m_DragLimitRenderer.SetPosition(i, pos);
        }
        m_DragLimitRenderer.enabled = true;
    }

    private IEnumerator WaitForPuckStopped()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitUntil(() => m_Rigidbody.velocity.magnitude <= STOP_THRESHOLD);

        if (m_HasReachedBoard)
        {
            s_ActivePuck = null;
            s_IsWhiteTurn = !s_IsWhiteTurn;
            if (Phase2Manager.IsPhase2Active)
            {
                BoardFlipper.FlipCamera();
            }
            else
            {
                BoardFlipper.Flip();
            }
            EventsManager.OnTurnChanged.Invoke(s_IsWhiteTurn);
        }
        else
        {
            // Shot stopped before reaching the board—reset for another try
            m_Rigidbody.position = m_StartPosition;
            transform.position = m_StartPosition;
            m_Rigidbody.velocity = Vector2.zero;
            m_Rigidbody.angularVelocity = 0f;
            transform.rotation = Quaternion.identity;
            m_HasReachedBoard = false;
        }

        m_GridManager?.UpdatePieceLayout();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name == "BoardTrigger")
        {
            m_HasReachedBoard = true;
        }
    }

    public Vector2Int CurrentGridPosition { get; private set; } // Store the grid position of this puck
    public ChessPiece ChessPiece { get; private set; }

    public bool IsWhitePiece => (int)ChessPiece >= 6;

    public static bool IsWhiteTurn => s_IsWhiteTurn;

    public static void ResetTurnOrder()
    {
        s_IsWhiteTurn = true; // Start with white's turn
        EventsManager.OnTurnChanged.Invoke(s_IsWhiteTurn);
    }

    private static void EnsureTileMap()
    {
        if (s_TileMap != null)
        {
            return;
        }

        s_TileMap = new Dictionary<Vector2Int, Tile>();
        Transform board = BoardFlipper.GetBoardTransform();
        if (board == null)
        {
            return;
        }

        Tile[] tiles = board.GetComponentsInChildren<Tile>();
        if (tiles.Length == 0)
        {
            return;
        }

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float step = float.PositiveInfinity;

        for (int i = 0; i < tiles.Length; i++)
        {
            Vector3 posI = tiles[i].transform.position;
            minX = Mathf.Min(minX, posI.x);
            minY = Mathf.Min(minY, posI.y);
            for (int j = i + 1; j < tiles.Length; j++)
            {
                float dx = Mathf.Abs(posI.x - tiles[j].transform.position.x);
                float dy = Mathf.Abs(posI.y - tiles[j].transform.position.y);
                if (dx > 0.001f && dx < step)
                {
                    step = dx;
                }
                if (dy > 0.001f && dy < step)
                {
                    step = dy;
                }
            }
        }

        if (float.IsInfinity(step) || step <= 0f)
        {
            step = 1f;
        }

        s_TileSize = step;
        s_BoardOrigin = new Vector2(minX, minY);

        foreach (Tile tile in tiles)
        {
            Vector3 pos = tile.transform.position;
            int gx = Mathf.RoundToInt((pos.x - minX) / step);
            int gy = Mathf.RoundToInt((pos.y - minY) / step);
            Vector2Int coords = new Vector2Int(gx, gy);
            if (!s_TileMap.ContainsKey(coords))
            {
                s_TileMap[coords] = tile;
            }
        }
    }

    private static bool IsWhite(ChessPiece piece)
    {
        return (int)piece >= 6;
    }

    private static void ClearHighlights()
    {
        foreach (Tile tile in s_HighlightedTiles)
        {
            tile.ClearHighlight();
        }
        s_HighlightedTiles.Clear();
    }

    private void HighlightLegalMoves()
    {
        EnsureTileMap();
        ClearHighlights();
        if (CurrentGridPosition.x < 0)
        {
            return;
        }

        // Build current board layout from pucks.
        Dictionary<Vector2Int, ChessPiece> layout = new Dictionary<Vector2Int, ChessPiece>();
        foreach (PuckController puck in FindObjectsOfType<PuckController>())
        {
            if (puck.CurrentGridPosition.x >= 0)
            {
                layout[puck.CurrentGridPosition] = puck.ChessPiece;
            }
        }

        List<Vector2Int> moves = GetLegalMoves(ChessPiece, CurrentGridPosition, layout);
        foreach (Vector2Int move in moves)
        {
            if (s_TileMap != null && s_TileMap.TryGetValue(move, out Tile tile))
            {
                tile.Highlight(Color.yellow);
                s_HighlightedTiles.Add(tile);
            }
        }
    }

    private static List<Vector2Int> GetLegalMoves(ChessPiece piece, Vector2Int start, Dictionary<Vector2Int, ChessPiece> layout)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        bool white = IsWhite(piece);

        switch (piece)
        {
            case ChessPiece.W_Pawn:
            case ChessPiece.B_Pawn:
            {
                int dir = white ? 1 : -1;
                Vector2Int forward = new Vector2Int(start.x, start.y + dir);
                if (!layout.ContainsKey(forward) && InBounds(forward))
                {
                    moves.Add(forward);
                    Vector2Int doubleStep = new Vector2Int(start.x, start.y + 2 * dir);
                    int startRow = white ? 1 : 6;
                    if (start.y == startRow && !layout.ContainsKey(doubleStep) && InBounds(doubleStep))
                    {
                        moves.Add(doubleStep);
                    }
                }
                Vector2Int left = new Vector2Int(start.x - 1, start.y + dir);
                Vector2Int right = new Vector2Int(start.x + 1, start.y + dir);
                if (layout.TryGetValue(left, out ChessPiece lp) && IsWhite(lp) != white)
                {
                    moves.Add(left);
                }
                if (layout.TryGetValue(right, out ChessPiece rp) && IsWhite(rp) != white)
                {
                    moves.Add(right);
                }
                break;
            }
            case ChessPiece.W_Rook:
            case ChessPiece.B_Rook:
                AddLinearMoves(moves, start, layout, white, new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1));
                break;
            case ChessPiece.W_Bishop:
            case ChessPiece.B_Bishop:
                AddLinearMoves(moves, start, layout, white, new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1));
                break;
            case ChessPiece.W_Queen:
            case ChessPiece.B_Queen:
                AddLinearMoves(moves, start, layout, white,
                    new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
                    new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1));
                break;
            case ChessPiece.W_Knight:
            case ChessPiece.B_Knight:
            {
                Vector2Int[] offsets =
                {
                    new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(-1, 2), new Vector2Int(-2, 1),
                    new Vector2Int(1, -2), new Vector2Int(2, -1), new Vector2Int(-1, -2), new Vector2Int(-2, -1)
                };
                foreach (Vector2Int off in offsets)
                {
                    Vector2Int dest = start + off;
                    if (!InBounds(dest))
                    {
                        continue;
                    }
                    if (layout.TryGetValue(dest, out ChessPiece p))
                    {
                        if (IsWhite(p) != white)
                        {
                            moves.Add(dest);
                        }
                    }
                    else
                    {
                        moves.Add(dest);
                    }
                }
                break;
            }
            case ChessPiece.W_King:
            case ChessPiece.B_King:
            {
                Vector2Int[] offsets =
                {
                    new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
                    new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
                };
                foreach (Vector2Int off in offsets)
                {
                    Vector2Int dest = start + off;
                    if (!InBounds(dest))
                    {
                        continue;
                    }
                    if (layout.TryGetValue(dest, out ChessPiece p))
                    {
                        if (IsWhite(p) != white)
                        {
                            moves.Add(dest);
                        }
                    }
                    else
                    {
                        moves.Add(dest);
                    }
                }
                break;
            }
        }

        return moves;
    }

    private static void AddLinearMoves(List<Vector2Int> moves, Vector2Int start, Dictionary<Vector2Int, ChessPiece> layout, bool white, params Vector2Int[] directions)
    {
        foreach (Vector2Int dir in directions)
        {
            Vector2Int pos = start + dir;
            while (InBounds(pos))
            {
                if (layout.TryGetValue(pos, out ChessPiece p))
                {
                    if (IsWhite(p) != white)
                    {
                        moves.Add(pos);
                    }
                    break;
                }
                moves.Add(pos);
                pos += dir;
            }
        }
    }

    private static bool InBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }

    public void UpdateGridPosition(float tileSize, Vector2 gridOrigin)
    {
        // Calculate the current grid position based on the puck's position
        Vector2 worldPosition = transform.position;

        int gridX = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / tileSize);
        int gridY = Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / tileSize);

        if (gridX < 0 || gridX > 7 || gridY < 0 || gridY > 7)
        {
            CurrentGridPosition = new Vector2Int(-1,-1);
        }
        else
        {
            CurrentGridPosition = new Vector2Int(gridX, gridY);
        }
    }

    public void SnapToGrid(float tileSize, Vector2 gridOrigin)
    {
        Vector2 worldPosition = transform.position;
        
        int gridX = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / tileSize);
        int gridY = Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / tileSize);
        
        // The center of tile (gridX, gridY):
        float centerX = gridOrigin.x + (gridX + 0.5f) * tileSize;
        float centerY = gridOrigin.y + (gridY + 0.5f) * tileSize;
        transform.position = new Vector2(centerX, centerY);

        m_Rigidbody.velocity = Vector2.zero;
    }
}
