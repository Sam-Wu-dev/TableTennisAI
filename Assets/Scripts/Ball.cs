using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    /// <summary>
    /// Agents hitting this ball.
    /// </summary>
    [Tooltip("Agents hitting this ball.")]
    public TableTennisAgent[] Agents = new TableTennisAgent[2];
    private TableTennisAgent _lastHitter;

    /// <summary>
    /// Runs when the ball hits the Collider.
    /// </summary>
    /// <param name="collision">Arguments that contain information about the colliding parties</param>
    private void OnCollisionEnter(Collision collision)
    {
        // fall to the floor
        if (collision.collider.CompareTag("floor"))
        {
            Agents[0].BallDropped();
            Agents[1].BallDropped();
        }

        // Hit the racket
        // Introducing Conditional operator "?" to avoid NullExpection, I got an InvalidOperationExpection error.                                                                
        if (collision.collider.transform.parent != null && collision.collider.transform.parent.CompareTag("racket"))
        {
            Debug.Log("racket hit");
            // Call the BallHit() of the Agent that hit the ball.
            var agent = collision.collider.transform.parent.GetComponent<TableTennisAgent>();
            _lastHitter = agent;
            agent.BallHit();
        }

        // bounces on the table
        if (collision.collider.CompareTag("table"))
        {
            Debug.Log("valid table bounce");
            _lastHitter?.BallBounced(collision.collider);
        }
    }
}
