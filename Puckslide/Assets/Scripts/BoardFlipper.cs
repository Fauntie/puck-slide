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

        // The board used in Phase 2 is built from UI elements, so it doesn't
        // necessarily have any Renderer components and its pivot might not sit
        // at the bottom‑left corner.  To mirror pieces correctly for both
        // phases we try a few strategies to find the actual centre of the
        // board:

        // 1) If the transform is a RectTransform (UI) we can use its world
        //    corners to determine the centre regardless of pivot.
        RectTransform rect = s_BoardTransform as RectTransform;
        if (rect != null)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f; // bottom‑left + top‑right
        }

        // 2) Otherwise fall back to the bounds of any renderers on the board.
        Renderer[] renderers = s_BoardTransform.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds.center;
        }

        // 3) As a last resort, assume the board pivot is at the bottom‑left
        //    and use the grid dimensions to compute the centre (Phase 1
        //    behaviour).
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

        Vector3 boardCenter = GetBoardCenter();
        s_BoardTransform.RotateAround(boardCenter, Vector3.forward, 180f);

        foreach (PuckController puck in Object.FindObjectsOfType<PuckController>())
        {
            if (!puck.transform.IsChildOf(s_BoardTransform))
            {
                Vector3 offset = puck.transform.position - boardCenter;
                // Mirror the puck across the board center on the X/Y plane but keep its original Z
                Vector3 newPos = new Vector3(boardCenter.x - offset.x,
                    boardCenter.y - offset.y,
                    puck.transform.position.z);
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
                Vector3 offset = piece.transform.position - boardCenter;
                // Mirror the piece across the board center on the X/Y plane but keep its original Z
                Vector3 newPos = new Vector3(boardCenter.x - offset.x,
                    boardCenter.y - offset.y,
                    piece.transform.position.z);
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
}
