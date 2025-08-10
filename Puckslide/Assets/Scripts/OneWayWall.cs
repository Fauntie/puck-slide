using System;
using UnityEngine;

public class OneWayWall : MonoBehaviour
{
    // private int puckLayer;
    // private int oneWayWallLayer;
    //
    // private void Awake()
    // {
    //     puckLayer = LayerMask.NameToLayer("Puck");
    //     oneWayWallLayer = LayerMask.NameToLayer("OneWayWall");
    // }
    //
    // private void FixedUpdate()
    // {
    //     // Find all pucks in the scene
    //     GameObject[] pucks = GameObject.FindGameObjectsWithTag("Puck");
    //
    //     foreach (GameObject puck in pucks)
    //     {
    //         Rigidbody2D rb = puck.GetComponent<Rigidbody2D>();
    //
    //         if (rb != null)
    //         {
    //             // Allow puck to pass through wall when moving upward
    //             if (rb.velocity.y > 0)
    //             {
    //                 Physics2D.IgnoreLayerCollision(puckLayer, oneWayWallLayer, true);
    //             }
    //             // Enable collision when puck moves downward
    //             else if (rb.velocity.y <= 0)
    //             {
    //                 Physics2D.IgnoreLayerCollision(puckLayer, oneWayWallLayer, false);
    //             }
    //         }
    //     }
    // }
    
    private int puckLayer;       // Original layer for pucks
    private int ignoreWallLayer; // Layer where pucks pass through the wall
    private int oneWayWallLayer; // Layer for the one-way wall

    private void Start()
    {
        puckLayer = LayerMask.NameToLayer("Puck");
        ignoreWallLayer = LayerMask.NameToLayer("IgnoreWall");
        oneWayWallLayer = LayerMask.NameToLayer("OneWayWall");

        // Ensure layer collisions are initially enabled
        Physics2D.IgnoreLayerCollision(puckLayer, oneWayWallLayer, false);
        Physics2D.IgnoreLayerCollision(ignoreWallLayer, oneWayWallLayer, true);
    }

    private void FixedUpdate()
    {
        // Find all pucks in the scene
        GameObject[] pucks = GameObject.FindGameObjectsWithTag("Puck");

        foreach (GameObject puck in pucks)
        {
            Rigidbody2D rb = puck.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                if (rb.velocity.y > 0)
                {
                    // Moving upward: switch layer to IgnoreWall to pass through
                    puck.layer = ignoreWallLayer;
                }
                else if (rb.velocity.y <= 0)
                {
                    // Moving downward: switch layer back to Puck to enable bouncing
                    puck.layer = puckLayer;
                }
            }
        }
    }
}
