using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
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

    private Collider2D m_BoardBounds;
    // Entry lines for both sides of the board.
    private float m_BottomEntryY;
    private float m_TopEntryY;

    private Vector3 m_StartPosition;
    private bool m_HasReachedBoard;

    private void Awake()
    {
        m_Camera = Camera.main;
        m_Rigidbody.freezeRotation = true;
        if (m_TrajectoryRenderer == null)
        {
            m_TrajectoryRenderer = GetComponent<LineRenderer>();
        }
        if (m_TrajectoryRenderer == null)
        {
            Debug.LogWarning("PuckController requires a LineRenderer component.", this);
        }
        m_PuckFriction = GetComponent<PuckFriction>();
        m_Collider = GetComponent<CircleCollider2D>();

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

            Texture2D tex = new Texture2D(2, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.SetPixel(1, 0, Color.clear);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = tex;
            mat.mainTextureScale = new Vector2(10f, 1f);
            m_TrajectoryRenderer.material = mat;
        }

        m_StartPosition = transform.position;
    }

    private void Start()
    {
        UpdateBoardEntryLines();
    }

    // Recalculate the entry lines at the top and bottom of the board.
    public void UpdateBoardEntryLines()
    {
        Transform board = BoardFlipper.GetBoardTransform();
        if (board == null)
        {
            m_BoardBounds = null;
            return;
        }
        m_BoardBounds = board.GetComponentInChildren<Collider2D>();
        Tile[] tiles = board.GetComponentsInChildren<Tile>();
        if (tiles.Length == 0)
        {
            Debug.LogWarning("PuckController could not locate any board tiles to determine board entry.", this);
            m_BottomEntryY = 0f;
            m_TopEntryY = 0f;
            return;
        }

        float minY = tiles[0].transform.position.y;
        float maxY = minY;
        float halfHeight = 0f;
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
    }

    private void OnEnable()
    {
        EventsManager.OnDeletePucks.AddListener(OnDelete);
        EventsManager.OnTurnChanged.AddListener(OnTurnChanged, true);
        BoardFlipper.OnBoardSet += UpdateBoardEntryLines;
        EventsManager.OnPuckSpawned.Invoke(m_Rigidbody);
    }

    private void OnDisable()
    {
        EventsManager.OnDeletePucks.RemoveListener(OnDelete);
        EventsManager.OnTurnChanged.RemoveListener(OnTurnChanged);
        BoardFlipper.OnBoardSet -= UpdateBoardEntryLines;
        EventsManager.OnPuckDespawned.Invoke(m_Rigidbody);
        if (s_ActivePuck == this)
        {
            s_ActivePuck = null;
        }
    }

    private void OnTurnChanged(bool _)
    {
        UpdateBoardEntryLines();
        Transform board = BoardFlipper.GetBoardTransform();
        if (board != null)
        {
            m_BoardBounds = board.GetComponentInChildren<Collider2D>();
        }
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

        bool reachedBoard = m_BoardBounds != null && m_BoardBounds.bounds.Contains(m_Rigidbody.worldCenterOfMass);

        if (reachedBoard)
        {

            m_HasReachedBoard = true;

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
