using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoardFlipper
{
    private static bool s_IsFlipped = false;
    private static Transform s_BoardTransform;

    private static int s_GridSize;
    private static float s_TileSize;
    private static Vector3 s_BoardCenter;

    private static Vector3 s_FlipOffset = Vector3.zero;

    private static BoardFlipAnimationRunner s_Animator;

    private const float FLIP_DURATION = 1.1f;
    private const float FLIP_LIFT_HEIGHT = 0.6f;


    public static void SetBoard(Transform board, int gridSize, float tileSize)
    {
        s_BoardTransform = board;

        s_GridSize = gridSize;
        s_TileSize = tileSize;

        EnsureBoardTrigger();

        RecalculateBoardCenter();
    }

    public static void SetFlipOffset(Vector3 offset)
    {
        s_FlipOffset = offset;
    }

    private static void EnsureBoardTrigger()
    {
        if (s_BoardTransform == null)
        {
            return;
        }

        const string triggerName = "BoardTrigger";
        Transform triggerTransform = s_BoardTransform.Find(triggerName);
        if (triggerTransform == null)
        {
            GameObject triggerObj = new GameObject(triggerName);
            triggerObj.transform.SetParent(s_BoardTransform, false);
            triggerTransform = triggerObj.transform;
        }

        triggerTransform.localPosition = new Vector3((s_GridSize - 1) * s_TileSize * 0.5f, (s_GridSize - 1) * s_TileSize * 0.5f, 0f);

        BoxCollider2D collider = triggerTransform.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = triggerTransform.gameObject.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
        collider.size = new Vector2(s_GridSize * s_TileSize, s_GridSize * s_TileSize);
    }

    private static void RecalculateBoardCenter()
    {
        if (s_BoardTransform == null)
        {
            s_BoardCenter = Vector3.zero;
            return;
        }

        // Prefer tile positions to avoid decorations such as scoreboards
        // skewing the board centre.
        Tile[] tiles = s_BoardTransform.GetComponentsInChildren<Tile>();
        if (tiles.Length > 0)
        {
            Vector3 min = tiles[0].transform.position;
            Vector3 max = min;
            foreach (Tile tile in tiles)
            {
                Vector3 p = tile.transform.position;
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            s_BoardCenter = (min + max) * 0.5f;
            return;
        }

        // Fallback to renderer bounds while ignoring pieces and pucks.
        Renderer[] renderers = s_BoardTransform.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        foreach (Renderer r in renderers)
        {

            if (r.GetComponentInParent<PuckController>() != null ||
                r.GetComponentInParent<Piece>() != null)
            {
                continue;
            }

            if (!boundsInitialized)
            {
                bounds = r.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }

        }

        if (boundsInitialized)
        {
            s_BoardCenter = bounds.center;
            return;
        }

        float halfSize = (s_GridSize - 1) * s_TileSize * 0.5f;
        s_BoardCenter = s_BoardTransform.TransformPoint(new Vector3(halfSize, halfSize, 0f));
    }

    public static Vector3 GetBoardCenter()
    {
        return s_BoardCenter;
    }

    public static Transform GetBoardTransform()
    {
        return s_BoardTransform;
    }

    public static bool IsFlipped()
    {
        return s_IsFlipped;
    }

    public static float GetFlipDuration()
    {
        return FLIP_DURATION;
    }

    public static IEnumerator Flip()
    {
        if (s_BoardTransform == null)
        {
            yield break;
        }

        EnsureAnimator();

        if (s_Animator == null)
        {
            yield break;
        }

        if (s_Animator.IsAnimating)
        {
            while (s_Animator.IsAnimating)
            {
                yield return null;
            }
            yield break;
        }

        bool targetFlippedState = !s_IsFlipped;

        // Ensure our cached centre is up to date before rotating.
        RecalculateBoardCenter();
        Vector3 boardCenterBefore = GetBoardCenter();
        s_BoardTransform.RotateAround(boardCenterBefore, Vector3.forward, 180f);

        // After rotation the board's bounds can shift if it has asymmetric
        // renderers (scoreboards, decorations, etc.).  Re‑center the board by
        // translating it so the centre matches its pre‑rotation position.
        RecalculateBoardCenter();
        Vector3 boardCenterAfter = GetBoardCenter();
        Vector3 boardOffset = boardCenterBefore - boardCenterAfter;
        Vector3 totalOffset = boardOffset + (targetFlippedState ? s_FlipOffset : -s_FlipOffset);

        Quaternion boardStartRotation = s_BoardTransform.rotation;
        Vector3 boardStartPosition = s_BoardTransform.position;
        Quaternion rotationDelta = Quaternion.AngleAxis(180f, Vector3.forward);

        Vector3 boardEndPosition = boardCenterBefore + rotationDelta * (boardStartPosition - boardCenterBefore) + totalOffset;
        Quaternion boardEndRotation = rotationDelta * boardStartRotation;

        // Revert to the original pose now that we've captured the targets.
        s_BoardTransform.RotateAround(boardCenterBefore, Vector3.forward, -180f);
        s_BoardTransform.SetPositionAndRotation(boardStartPosition, boardStartRotation);
        RecalculateBoardCenter();

        var animatedObjects = new List<FlipObjectData>();
        var seenTransforms = new HashSet<Transform>();

        foreach (PuckController puck in UnityEngine.Object.FindObjectsOfType<PuckController>())
        {
            if (puck == null)
            {
                continue;
            }

            Transform transform = puck.transform;
            if (transform == null || !seenTransforms.Add(transform))
            {
                continue;
            }

            Vector3 startPosition = transform.position;
            var data = new FlipObjectData
            {
                Transform = transform,
                StartPosition = startPosition,
                OffsetFromCenter = startPosition - boardCenterBefore,
                StartRotation = transform.rotation,
                EndRotation = Quaternion.identity,
                EndPosition = boardCenterBefore + rotationDelta * (startPosition - boardCenterBefore) + totalOffset,
                Rigidbody = transform.GetComponent<Rigidbody2D>()
            };
            animatedObjects.Add(data);
        }

        foreach (Piece piece in UnityEngine.Object.FindObjectsOfType<Piece>())
        {
            if (piece == null)
            {
                continue;
            }

            Transform transform = piece.transform;
            if (transform == null || !seenTransforms.Add(transform))
            {
                continue;
            }

            Vector3 startPosition = transform.position;
            var data = new FlipObjectData
            {
                Transform = transform,
                StartPosition = startPosition,
                OffsetFromCenter = startPosition - boardCenterBefore,
                StartRotation = transform.rotation,
                EndRotation = Quaternion.identity,
                EndPosition = boardCenterBefore + rotationDelta * (startPosition - boardCenterBefore) + totalOffset,
                Rigidbody = transform.GetComponent<Rigidbody2D>()
            };
            animatedObjects.Add(data);
        }

        Vector3 liftDirection = Vector3.back;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 cameraLift = -cam.transform.forward;
            if (cameraLift.sqrMagnitude > Mathf.Epsilon)
            {
                liftDirection = cameraLift.normalized;
            }
        }

        var animationData = new BoardFlipAnimationData
        {
            BoardTransform = s_BoardTransform,
            BoardCenterBefore = boardCenterBefore,
            BoardStartPosition = boardStartPosition,
            BoardEndPosition = boardEndPosition,
            BoardStartRotation = boardStartRotation,
            BoardEndRotation = boardEndRotation,
            TotalOffset = totalOffset,
            Objects = animatedObjects,
            Duration = FLIP_DURATION,
            LiftHeight = FLIP_LIFT_HEIGHT,
            LiftDirection = liftDirection
        };

        s_IsFlipped = targetFlippedState;
        EventsManager.OnBoardFlipState.Invoke(s_IsFlipped);
        s_Animator.BeginAnimation(animationData);

        while (s_Animator.IsAnimating)
        {
            yield return null;
        }
    }

    public static void FlipCamera()
    {
        if (s_BoardTransform == null)
        {
            return;
        }

        s_IsFlipped = !s_IsFlipped;
        EventsManager.OnBoardFlipState.Invoke(s_IsFlipped);

        // Keep camera rotation centred on the actual board centre.
        RecalculateBoardCenter();
        Vector3 boardCenter = GetBoardCenter();
        Camera cam = Camera.main;
        if (cam != null)
        {

            // Preserve the camera's original offset from the board centre so the board
            // remains in the exact same screen position after the flip.
            Vector3 offset = cam.transform.position - boardCenter;

            cam.transform.position = new Vector3(boardCenter.x, boardCenter.y, cam.transform.position.z);
            cam.transform.RotateAround(boardCenter, Vector3.forward, 180f);

            cam.transform.position = new Vector3(boardCenter.x + offset.x, boardCenter.y + offset.y, cam.transform.position.z);
        }

        // Ensure pucks remain upright relative to the player's view when the

        // camera is flipped. Rotating them by 180° each time we flip the camera
        // preserves their original orientation without relying on a hardcoded
        // absolute rotation.
        foreach (PuckController puck in UnityEngine.Object.FindObjectsOfType<PuckController>())
        {
            puck.transform.Rotate(0f, 0f, 180f, Space.Self);

        }

        // Pieces are implemented as flat sprites on pucks. When the camera flips
        // to show the opposite player's perspective we need to rotate these
        // sprites as well, otherwise the chess piece images appear upside down.
        // Rotating each Piece by 180° keeps the artwork facing "up" relative to
        // the camera just like we do for the pucks themselves.
        foreach (Piece piece in UnityEngine.Object.FindObjectsOfType<Piece>())
        {
            piece.transform.Rotate(0f, 0f, 180f, Space.Self);
        }
    }

    private static void EnsureAnimator()
    {
        if (s_Animator != null)
        {
            return;
        }

        GameObject animatorObject = new GameObject("BoardFlipAnimator");
        animatorObject.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(animatorObject);
        s_Animator = animatorObject.AddComponent<BoardFlipAnimationRunner>();
    }

    private class BoardFlipAnimationRunner : MonoBehaviour
    {
        public bool IsAnimating { get; private set; }

        public void BeginAnimation(BoardFlipAnimationData data)
        {
            if (IsAnimating)
            {
                return;
            }

            StartCoroutine(Animate(data));
        }

        private IEnumerator Animate(BoardFlipAnimationData data)
        {
            IsAnimating = true;

            foreach (FlipObjectData obj in data.Objects)
            {
                if (obj.Rigidbody != null)
                {
                    obj.WasSimulated = obj.Rigidbody.simulated;
                    obj.Rigidbody.simulated = false;
                    obj.Rigidbody.velocity = Vector2.zero;
                    obj.Rigidbody.angularVelocity = 0f;
                }
            }

            float elapsed = 0f;
            Vector3 normalizedLift = data.LiftDirection.sqrMagnitude > Mathf.Epsilon
                ? data.LiftDirection.normalized
                : Vector3.back;

            while (elapsed < data.Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / data.Duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Quaternion rotation = Quaternion.AngleAxis(180f * smoothT, Vector3.forward);
                Vector3 translation = data.TotalOffset * smoothT;
                float liftAmount = Mathf.Sin(Mathf.PI * smoothT) * data.LiftHeight;
                Vector3 liftOffset = normalizedLift * liftAmount;

                Vector3 boardRotatedOffset = rotation * (data.BoardStartPosition - data.BoardCenterBefore);
                data.BoardTransform.position = data.BoardCenterBefore + boardRotatedOffset + translation + liftOffset;
                data.BoardTransform.rotation = rotation * data.BoardStartRotation;

                foreach (FlipObjectData obj in data.Objects)
                {
                    Vector3 rotated = rotation * obj.OffsetFromCenter;
                    obj.Transform.position = data.BoardCenterBefore + rotated + translation + liftOffset;
                    obj.Transform.rotation = Quaternion.Slerp(obj.StartRotation, obj.EndRotation, smoothT);
                }

                yield return null;
            }

            data.BoardTransform.position = data.BoardEndPosition;
            data.BoardTransform.rotation = data.BoardEndRotation;

            foreach (FlipObjectData obj in data.Objects)
            {
                obj.Transform.position = obj.EndPosition;
                obj.Transform.rotation = obj.EndRotation;
                if (obj.Rigidbody != null)
                {
                    obj.Rigidbody.velocity = Vector2.zero;
                    obj.Rigidbody.angularVelocity = 0f;
                    obj.Rigidbody.simulated = obj.WasSimulated;
                }
            }

            RecalculateBoardCenter();

            IsAnimating = false;
        }
    }

    private class BoardFlipAnimationData
    {
        public Transform BoardTransform;
        public Vector3 BoardCenterBefore;
        public Vector3 BoardStartPosition;
        public Vector3 BoardEndPosition;
        public Quaternion BoardStartRotation;
        public Quaternion BoardEndRotation;
        public Vector3 TotalOffset;
        public List<FlipObjectData> Objects;
        public float Duration;
        public float LiftHeight;
        public Vector3 LiftDirection;
    }

    private class FlipObjectData
    {
        public Transform Transform;
        public Vector3 StartPosition;
        public Vector3 OffsetFromCenter;
        public Quaternion StartRotation;
        public Quaternion EndRotation;
        public Vector3 EndPosition;
        public Rigidbody2D Rigidbody;
        public bool WasSimulated;
    }
}
