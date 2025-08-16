using System;
using System.Collections;
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
    private float m_MaxDragDistance = 1.5f;

    [SerializeField]
    private float m_ArrowHeadLength = 0.3f;

    [SerializeField]
    private float m_ArrowHeadWidth = 0.15f;

    private Vector3 m_DragStartPos;
    private Camera m_Camera;

    private PuckFriction m_PuckFriction;

    private bool m_IsSticky;

    private const float STOP_THRESHOLD = 0.05f;
    private const float SHOOT_POWER = 4f;
    private static bool? s_LastMoveWasWhite = null;
    private bool m_IsSelected;

    private void Awake()
    {
        m_Camera = Camera.main;
        m_Rigidbody.freezeRotation = true;
        m_PuckFriction = GetComponent<PuckFriction>();

        if (m_TrajectoryRenderer != null)
        {
            m_TrajectoryRenderer.enabled = false;
            m_TrajectoryRenderer.startColor = Color.red;
            m_TrajectoryRenderer.endColor = Color.red;
            m_TrajectoryRenderer.textureMode = LineTextureMode.Tile;
            m_TrajectoryRenderer.positionCount = 0;

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
    }

    private void OnEnable()
    {
        EventsManager.OnDeletePucks.AddListener(OnDelete);
        EventsManager.OnPuckSpawned.Invoke(m_Rigidbody);
    }

    private void OnDisable()
    {
        EventsManager.OnDeletePucks.RemoveListener(OnDelete);
        EventsManager.OnPuckDespawned.Invoke(m_Rigidbody);
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
        if (s_LastMoveWasWhite != null && IsWhitePiece == s_LastMoveWasWhite.Value)
        {
            m_IsSelected = false;
            return;
        }

        m_IsSelected = true;
        if (m_IsSticky && m_Rigidbody.bodyType == RigidbodyType2D.Static)
        {
            m_Rigidbody.bodyType = RigidbodyType2D.Dynamic;
        }

        m_DragStartPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        
        if (m_LineRenderer != null)
        {
            m_LineRenderer.enabled = true;
            m_LineRenderer.positionCount = 5;
            Vector3 start = transform.position;
            start.z = 0;
            for (int i = 0; i < m_LineRenderer.positionCount; i++)
            {
                m_LineRenderer.SetPosition(i, start);
            }
            m_LineRenderer.startColor = Color.green;
            m_LineRenderer.endColor = Color.green;

            DrawDragLimitCircle(start);
        }

        if (m_TrajectoryRenderer != null)
        {
            m_TrajectoryRenderer.enabled = true;
            m_TrajectoryRenderer.positionCount = 0;
        }
    }

    private void OnMouseDrag()
    {
        if (!m_IsSelected)
        {
            return;
        }

        if (m_LineRenderer != null && m_LineRenderer.enabled)
        {
            Vector3 dragPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
            dragPos.z = 0;

            Vector3 puckCenter = transform.position;
            puckCenter.z = 0;

            Vector3 offset = dragPos - puckCenter;
            Vector3 clampedOffset = Vector3.ClampMagnitude(offset, m_MaxDragDistance);
            Vector3 endPos = puckCenter + clampedOffset;

            m_LineRenderer.SetPosition(0, endPos);
            m_LineRenderer.SetPosition(1, puckCenter);

            float powerRatio = clampedOffset.magnitude / m_MaxDragDistance;
            Color powerColor = Color.Lerp(Color.green, Color.red, powerRatio);
            m_LineRenderer.startColor = powerColor;
            m_LineRenderer.endColor = powerColor;

            if (clampedOffset.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = (puckCenter - endPos).normalized;
                Vector3 perp = new Vector3(-direction.y, direction.x, 0f);
                Vector3 arrowBase = puckCenter - direction * m_ArrowHeadLength;
                Vector3 left = arrowBase + perp * m_ArrowHeadWidth * 0.5f;
                Vector3 right = arrowBase - perp * m_ArrowHeadWidth * 0.5f;

                m_LineRenderer.SetPosition(2, left);
                m_LineRenderer.SetPosition(3, puckCenter);
                m_LineRenderer.SetPosition(4, right);
            }

            Vector3 dragVector = -clampedOffset;
            if (m_TrajectoryRenderer != null && m_TrajectoryRenderer.enabled)
            {
                UpdateTrajectory(dragVector * SHOOT_POWER);
            }

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
        dragVector = Vector2.ClampMagnitude(dragVector, m_MaxDragDistance);

        m_Rigidbody.bodyType = RigidbodyType2D.Dynamic;
        m_Rigidbody.AddForce(dragVector * SHOOT_POWER, ForceMode2D.Impulse);

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

        s_LastMoveWasWhite = IsWhitePiece;
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
        if (Phase2Manager.IsPhase2Active)
        {
            BoardFlipper.FlipCamera();
        }
        else
        {
            BoardFlipper.Flip();
        }
    }

    public Vector2Int CurrentGridPosition { get; private set; } // Store the grid position of this puck
    public ChessPiece ChessPiece { get; private set; }

    public bool IsWhitePiece => (int)ChessPiece >= 6;

    public static void ResetTurnOrder()
    {
        s_LastMoveWasWhite = false; // Start with white's turn
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
