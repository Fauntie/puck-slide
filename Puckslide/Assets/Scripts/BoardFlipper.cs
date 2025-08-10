using UnityEngine;

public static class BoardFlipper
{
    private static bool s_IsFlipped = false;

    public static void Flip()
    {
        if (Camera.main == null)
        {
            return;
        }

        s_IsFlipped = !s_IsFlipped;
        float angle = s_IsFlipped ? 180f : 0f;
        Camera.main.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
