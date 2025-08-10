using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase2Manager : MonoBehaviour
{
    private void OnEnable()
    {
        // Clear any lingering pucks from the board. Pieces will be spawned
        // from the last recorded layout by the BoardController, so avoid
        // destroying them here.
        EventsManager.OnDeletePucks.Invoke(true);
    }
}
