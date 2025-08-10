using UnityEngine;

public static class BoardFlipper
{
    private static bool s_IsFlipped = false;
    private static Transform s_BoardTransform;

    private static int s_GridSize;
    private static float s_TileSize;

    public static void SetBoard(Transform board, int gridSize, float tileSize)
    {
        s_BoardTransform = board;

        s_GridSize = gridSize;
        s_TileSize = tileSize;
    }

    private static Vector3 GetBoardCenter()
    {
        if (s_BoardTransform == null)
        {
            return Vector3.zero;
        }

        // In Phase 2 the board's pivot isn't guaranteed to sit at the
        // bottom‑left corner of the grid.  Computing the centre assuming a
        // particular pivot causes the pieces to be mirrored to the wrong
        // location when the board is flipped.  Instead we calculate the
        // bounds of all renderers on the board and use the bounds centre as
        // the rotation point.  This works regardless of the board's pivot or
        // orientation.
        Renderer[] renderers = s_BoardTransform.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        foreach (Renderer r in renderers)
        {
            // Ignore renderers that belong to pieces or pucks. Including them
            // skews the bounds toward whichever side of the board currently
            // has more pieces, causing the board to shift in view when
            // rotated.
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
            return bounds.center;
        }

        // Fallback to the old calculation if no renderers are present.
        float halfSize = (s_GridSize - 1) * s_TileSize * 0.5f;
        return s_BoardTransform.TransformPoint(new Vector3(halfSize, halfSize, 0f));
    }

    public static void Flip()
    {
        if (s_BoardTransform == null)
        {
            return;
        }

        s_IsFlipped = !s_IsFlipped;

        // Rotate the board around its current centre.
        Vector3 boardCenterBefore = GetBoardCenter();
        s_BoardTransform.RotateAround(boardCenterBefore, Vector3.forward, 180f);

        // After rotation the board's bounds can shift if it has asymmetric
        // renderers (scoreboards, decorations, etc.).  Re‑center the board by
        // translating it so the centre matches its pre‑rotation position.
        Vector3 boardCenterAfter = GetBoardCenter();
        Vector3 boardOffset = boardCenterBefore - boardCenterAfter;
        s_BoardTransform.position += boardOffset;

        foreach (PuckController puck in Object.FindObjectsOfType<PuckController>())
        {
            if (!puck.transform.IsChildOf(s_BoardTransform))
            {
                Vector3 offset = puck.transform.position - boardCenterBefore;
                // Mirror the puck across the board centre on the X/Y plane but keep its original Z.
                Vector3 newPos = new Vector3(boardCenterBefore.x - offset.x,
                    boardCenterBefore.y - offset.y,
                    puck.transform.position.z);
                // Apply the board translation so independent pucks stay aligned.
                newPos += boardOffset;
                puck.transform.position = newPos;
            }

            puck.transform.rotation = Quaternion.identity;

            Rigidbody2D rb = puck.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        foreach (Piece piece in Object.FindObjectsOfType<Piece>())
        {
            if (!piece.transform.IsChildOf(s_BoardTransform))
            {
                Vector3 offset = piece.transform.position - boardCenterBefore;
                // Mirror the piece across the board centre on the X/Y plane but keep its original Z.
                Vector3 newPos = new Vector3(boardCenterBefore.x - offset.x,
                    boardCenterBefore.y - offset.y,
                    piece.transform.position.z);
                // Apply the board translation so independent pieces stay aligned.
                newPos += boardOffset;
                piece.transform.position = newPos;
            }

            piece.transform.rotation = Quaternion.identity;

            Rigidbody2D rb = piece.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    public static void FlipCamera()
    {
        if (s_BoardTransform == null)
        {
            return;
        }

        s_IsFlipped = !s_IsFlipped;

        Vector3 boardCenter = GetBoardCenter();
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.RotateAround(boardCenter, Vector3.forward, 180f);
        }
    }
}
