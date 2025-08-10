using UnityEngine;

public static class BoardFlipper
{
    private static bool s_IsFlipped = false;
    private static Transform s_BoardTransform;

    private static int s_GridSize;
    private static float s_TileSize;

    private static Vector3 s_BoardCenter;

    public static void SetBoard(Transform board, int gridSize, float tileSize)
    {
        s_BoardTransform = board;

        s_GridSize = gridSize;
        s_TileSize = tileSize;

        // Calculate the actual world-space center of the board. The board's
        // transform position represents the bottom-left corner of the grid, so
        // we offset by half of the board's width and height to get the center.
        float halfSize = (gridSize - 1) * tileSize * 0.5f;
        s_BoardCenter = board.position + new Vector3(halfSize, halfSize, 0f);
    }

    private static Vector3 GetBoardCenter()
    {
        return s_BoardCenter;

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
