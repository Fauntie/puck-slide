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

        s_BoardCenter = board.position;
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
                puck.transform.position = boardCenter - offset;
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
                piece.transform.position = boardCenter - offset;
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
