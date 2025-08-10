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
        return s_BoardTransform.position + new Vector3(s_GridSize * s_TileSize / 2f, s_GridSize * s_TileSize / 2f, 0f);

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

                puck.transform.RotateAround(boardCenter, Vector3.forward, 180f);

            }
            puck.transform.rotation = Quaternion.identity;
        }

        foreach (Piece piece in Object.FindObjectsOfType<Piece>())
        {
            if (!piece.transform.IsChildOf(s_BoardTransform))
            {

                piece.transform.RotateAround(boardCenter, Vector3.forward, 180f);

            }
            piece.transform.rotation = Quaternion.identity;
        }
    }
}
