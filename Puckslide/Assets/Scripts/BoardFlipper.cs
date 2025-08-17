using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BoardFlipper
{
    // Fired once the board transform and size have been configured.
    public static event Action OnBoardSet;

    private static bool s_IsFlipped = false;
    private static Transform s_BoardTransform;

    private static int s_GridSize;
    private static float s_TileSize;
    private static Vector3 s_BoardCenter;

    private static Vector3 s_FlipOffset = Vector3.zero;


    public static void SetBoard(Transform board, int gridSize, float tileSize)
    {
        s_BoardTransform = board;

        s_GridSize = gridSize;
        s_TileSize = tileSize;

        RecalculateBoardCenter();
        // Inform any listeners that the board is ready for use.
        OnBoardSet?.Invoke();
    }

    public static void SetFlipOffset(Vector3 offset)
    {
        s_FlipOffset = offset;
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

    public static void Flip()
    {
        if (s_BoardTransform == null)
        {
            return;
        }

        s_IsFlipped = !s_IsFlipped;

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
        Vector3 totalOffset = boardOffset + (s_IsFlipped ? s_FlipOffset : -s_FlipOffset);
        s_BoardTransform.position += totalOffset;
        // Update the cached centre again now that the board has been moved.
        RecalculateBoardCenter();

        foreach (PuckController puck in Object.FindObjectsOfType<PuckController>())
        {
            if (!puck.transform.IsChildOf(s_BoardTransform))
            {

                Vector3 offset = puck.transform.position - boardCenterBefore;
                Vector3 newPos = new Vector3(boardCenterBefore.x - offset.x,
                    boardCenterBefore.y - offset.y,

                    puck.transform.position.z);
                // Apply the board translation so independent pucks stay aligned.
                newPos += totalOffset;
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
                Vector3 newPos = new Vector3(boardCenterBefore.x - offset.x,
                    boardCenterBefore.y - offset.y,

                    piece.transform.position.z);
                // Apply the board translation so independent pieces stay aligned.
                newPos += totalOffset;
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

        // Update the cached centre again after moving the board and pieces.
        RecalculateBoardCenter();
    }

    public static void FlipCamera()
    {
        if (s_BoardTransform == null)
        {
            return;
        }

        s_IsFlipped = !s_IsFlipped;

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
        foreach (PuckController puck in Object.FindObjectsOfType<PuckController>())
        {
            puck.transform.Rotate(0f, 0f, 180f, Space.Self);

        }

        // Pieces are implemented as flat sprites on pucks. When the camera flips
        // to show the opposite player's perspective we need to rotate these
        // sprites as well, otherwise the chess piece images appear upside down.
        // Rotating each Piece by 180° keeps the artwork facing "up" relative to
        // the camera just like we do for the pucks themselves.
        foreach (Piece piece in Object.FindObjectsOfType<Piece>())
        {
            piece.transform.Rotate(0f, 0f, 180f, Space.Self);
        }
    }
}
