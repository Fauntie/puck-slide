using UnityEngine;
using UnityEngine.EventSystems;

public class LocalInputRouter : MonoBehaviour
{
    [SerializeField]
    private int m_LocalPlayerId;
    [SerializeField]
    private PlayerCommandDispatcher m_Dispatcher;
    [SerializeField]
    private Camera m_Camera;

    private bool m_IsPointerActive;
    private int m_ActivePointerId = -1;
    private PlayerCommandTarget m_ActiveTarget = PlayerCommandTarget.Board;
    private int m_ActiveInstanceId = -1;

    private void Awake()
    {
        if (m_Dispatcher == null)
        {
            m_Dispatcher = PlayerCommandDispatcher.Instance ?? FindObjectOfType<PlayerCommandDispatcher>();
        }

        if (m_Camera == null)
        {
            m_Camera = Camera.main;
        }
    }

    private void Update()
    {
        if (m_Dispatcher == null || m_Camera == null)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (TryGetPointerDown(out Vector3 pointerDownPos, out int pointerId))
        {
            PlayerCommand command = BuildPointerCommand(pointerDownPos, pointerId, PlayerCommandType.PointerDown);
            m_Dispatcher.Enqueue(command);
            m_IsPointerActive = true;
            m_ActivePointerId = pointerId;
        }

        if (m_IsPointerActive && TryGetPointerPosition(out Vector3 pointerPos))
        {
            PlayerCommand command = BuildPointerCommand(pointerPos, m_ActivePointerId, PlayerCommandType.PointerDrag);
            m_Dispatcher.Enqueue(command);
        }

        if (m_IsPointerActive && TryGetPointerUp(out Vector3 pointerUpPos))
        {
            PlayerCommand command = BuildPointerCommand(pointerUpPos, m_ActivePointerId, PlayerCommandType.PointerUp);
            m_Dispatcher.Enqueue(command);
            m_IsPointerActive = false;
            m_ActivePointerId = -1;
            m_ActiveInstanceId = -1;
            m_ActiveTarget = PlayerCommandTarget.Board;
        }
    }

    private PlayerCommand BuildPointerCommand(Vector3 worldPos, int pointerId, PlayerCommandType type)
    {
        if (type == PlayerCommandType.PointerDown)
        {
            IdentifyTarget(worldPos);
        }

        return PlayerCommand.CreatePointerCommand(
            m_LocalPlayerId,
            pointerId,
            type,
            m_ActiveTarget,
            m_ActiveInstanceId,
            worldPos);
    }

    private void IdentifyTarget(Vector3 worldPos)
    {
        m_ActiveTarget = PlayerCommandTarget.Board;
        m_ActiveInstanceId = -1;

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
        foreach (RaycastHit2D hit in hits)
        {
            PuckController puck = hit.collider.GetComponent<PuckController>();
            if (puck != null)
            {
                m_ActiveTarget = PlayerCommandTarget.Puck;
                m_ActiveInstanceId = puck.GetInstanceID();
                return;
            }
        }
    }

    private bool TryGetPointerDown(out Vector3 worldPos, out int pointerId)
    {
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    pointerId = touch.fingerId;
                    worldPos = m_Camera.ScreenToWorldPoint(touch.position);
                    return true;
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            pointerId = -1;
            worldPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
            return true;
        }

        worldPos = Vector3.zero;
        pointerId = -1;
        return false;
    }

    private bool TryGetPointerPosition(out Vector3 worldPos)
    {
        if (Input.touchCount > 0 && m_ActivePointerId >= 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == m_ActivePointerId && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    worldPos = m_Camera.ScreenToWorldPoint(touch.position);
                    return true;
                }
            }
        }
        else if (m_ActivePointerId == -1 && Input.GetMouseButton(0))
        {
            worldPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
            return true;
        }

        worldPos = Vector3.zero;
        return false;
    }

    private bool TryGetPointerUp(out Vector3 worldPos)
    {
        if (Input.touchCount > 0 && m_ActivePointerId >= 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == m_ActivePointerId && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    worldPos = m_Camera.ScreenToWorldPoint(touch.position);
                    return true;
                }
            }
        }
        else if (m_ActivePointerId == -1 && Input.GetMouseButtonUp(0))
        {
            worldPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
            return true;
        }

        worldPos = Vector3.zero;
        return false;
    }
}
