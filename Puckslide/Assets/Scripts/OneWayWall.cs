using System;
using System.Collections.Generic;
using UnityEngine;

public class OneWayWall : MonoBehaviour
{
    private int puckLayer;       // Original layer for pucks
    private int ignoreWallLayer; // Layer where pucks pass through the wall
    private int oneWayWallLayer; // Layer for the one-way wall

    private readonly List<Rigidbody2D> m_Pucks = new List<Rigidbody2D>();

    private void OnEnable()
    {
        EventsManager.OnPuckSpawned.AddListener(RegisterPuck);
        EventsManager.OnPuckDespawned.AddListener(UnregisterPuck);
    }

    private void OnDisable()
    {
        EventsManager.OnPuckSpawned.RemoveListener(RegisterPuck);
        EventsManager.OnPuckDespawned.RemoveListener(UnregisterPuck);
        m_Pucks.Clear();
    }

    private void RegisterPuck(Rigidbody2D rb)
    {
        if (rb != null && !m_Pucks.Contains(rb))
        {
            m_Pucks.Add(rb);
        }
    }

    private void UnregisterPuck(Rigidbody2D rb)
    {
        m_Pucks.Remove(rb);
    }

    private void Start()
    {
        puckLayer = LayerMask.NameToLayer("Puck");
        ignoreWallLayer = LayerMask.NameToLayer("IgnoreWall");
        oneWayWallLayer = LayerMask.NameToLayer("OneWayWall");

        // Ensure layer collisions are initially enabled
        Physics2D.IgnoreLayerCollision(puckLayer, oneWayWallLayer, false);
        Physics2D.IgnoreLayerCollision(ignoreWallLayer, oneWayWallLayer, true);

        // Cache any pucks already in the scene
        GameObject[] pucks = GameObject.FindGameObjectsWithTag("Puck");
        foreach (GameObject puck in pucks)
        {
            Rigidbody2D rb = puck.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                m_Pucks.Add(rb);
            }
        }
    }

    private void FixedUpdate()
    {
        for (int i = m_Pucks.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rb = m_Pucks[i];
            if (rb == null)
            {
                m_Pucks.RemoveAt(i);
                continue;
            }

            if (rb.velocity.y > 0)
            {
                // Moving upward: switch layer to IgnoreWall to pass through
                rb.gameObject.layer = ignoreWallLayer;
            }
            else
            {
                // Moving downward: switch layer back to Puck to enable bouncing
                rb.gameObject.layer = puckLayer;
            }
        }
    }
}
