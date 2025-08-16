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
    private SpriteRenderer m_SpriteRenderer;

    [SerializeField]
    private Sprite[] m_Sprites;

    private Vector3 m_DragStartPos;
    private Camera m_Camera;

    private bool m_IsSticky;

    private const float STOP_THRESHOLD = 0.05f;
    private static bool? s_LastMoveWasWhite = null;
    private bool m_IsSelected;

    private void Awake()
    {
        m_Camera = Camera.main;
        m_Rigidbody.freezeRotation = true;
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
            Vector3 start = transform.position;
            start.z = 0;
            m_LineRenderer.SetPosition(0, start);
            m_LineRenderer.SetPosition(1, start);
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
            m_LineRenderer.SetPosition(0, puckCenter);

            m_LineRenderer.SetPosition(1, dragPos);
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
        const float maxDragDistance = 3f;
        dragVector = Vector2.ClampMagnitude(dragVector, maxDragDistance);
        float power = 4f;
        m_Rigidbody.bodyType = RigidbodyType2D.Dynamic;
        m_Rigidbody.AddForce(dragVector * power, ForceMode2D.Impulse);

        if (m_LineRenderer != null)
        {
            m_LineRenderer.enabled = false;
        }

        s_LastMoveWasWhite = IsWhitePiece;
        m_IsSelected = false;

        StartCoroutine(WaitForPuckStopped());
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
