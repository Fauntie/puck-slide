using UnityEngine;

public static class BoardFlipper
{
    private static bool s_IsFlipped = false;
    private static Transform s_BoardTransform;
    private static Vector3 s_BoardCenter;

    public static void SetBoard(Transform board, int gridSize, float tileSize)
    {
        s_BoardTransform = board;
        s_BoardCenter = board.position + new Vector3(gridSize * tileSize / 2f, gridSize * tileSize / 2f, 0f);
    }

    public static void Flip()
    {
        if (s_BoardTransform == null)
        {
            return;
        }

        s_IsFlipped = !s_IsFlipped;
        float angle = s_IsFlipped ? 180f : 0f;
        s_BoardTransform.rotation = Quaternion.Euler(0f, 0f, angle);

        foreach (PuckController puck in Object.FindObjectsOfType<PuckController>())
        {
            if (!puck.transform.IsChildOf(s_BoardTransform))
            {
                puck.transform.RotateAround(s_BoardCenter, Vector3.forward, 180f);
            }
            puck.transform.rotation = Quaternion.identity;
        }

        foreach (Piece piece in Object.FindObjectsOfType<Piece>())
        {
            if (!piece.transform.IsChildOf(s_BoardTransform))
            {
                piece.transform.RotateAround(s_BoardCenter, Vector3.forward, 180f);
            }
            piece.transform.rotation = Quaternion.identity;
        }
    }
}
