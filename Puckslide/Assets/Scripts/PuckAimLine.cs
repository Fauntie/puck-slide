using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PuckAimLine : MonoBehaviour
{
    public LineRenderer line;
    public float maxAimLength = 10f;
    public LayerMask collisionMask;
    public float lineStartWidth = 0.05f;
    public float lineEndWidth = 0.05f;
    public float forceMultiplier = 9.12f;
    public float minDragToShow = 0.05f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Camera cam;
    private bool dragging;
    private Vector2 dragStart;
    private const float MOVE_THRESHOLD = 0.05f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        cam = Camera.main;

        if (line == null)
        {
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
            }
        }
        SetupLine();
    }

    private void SetupLine()
    {
        if (line == null) return;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = lineStartWidth;
        line.endWidth = lineEndWidth;
        line.numCapVertices = 4;
        if (line.material == null)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
        }
        line.enabled = false;
    }

    private void OnValidate()
    {
        if (line != null)
        {
            SetupLine();
        }
    }

    private void Update()
    {
        Vector2 pointerWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            if (rb.velocity.magnitude > MOVE_THRESHOLD) return;
            if (col != null && col.OverlapPoint(pointerWorld))
            {
                dragging = true;
                dragStart = pointerWorld;
                line.enabled = false;
            }
        }

        if (dragging && Input.GetMouseButton(0))
        {
            Vector2 dragVec = dragStart - pointerWorld;
            if (dragVec.magnitude < minDragToShow)
            {
                line.enabled = false;
            }
            else
            {
                line.enabled = true;
                Vector2 dir = dragVec.normalized;
                Vector2 puckPos = rb.position;
                RaycastHit2D hit = Physics2D.Raycast(puckPos, dir, maxAimLength, collisionMask);
                Vector3 end = hit ? (Vector3)hit.point : (Vector3)(puckPos + dir * maxAimLength);
                line.SetPosition(0, puckPos);
                line.SetPosition(1, end);
            }
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            Vector2 dragVec = dragStart - pointerWorld;
            rb.AddForce(dragVec * forceMultiplier, ForceMode2D.Impulse);
            dragging = false;
            line.enabled = false;
        }

        if (!Input.GetMouseButton(0) && !dragging)
        {
            line.enabled = false;
        }
    }
}
