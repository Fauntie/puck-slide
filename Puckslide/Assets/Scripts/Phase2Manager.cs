using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase2Manager : MonoBehaviour
{
    private void OnEnable()
    {
        EventsManager.OnDeletePucks.Invoke(true);
    }
}
