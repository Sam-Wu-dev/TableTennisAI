using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [Tooltip("Agents hitting this ball.")]
    public TableTennisAgent[] Agents = new TableTennisAgent[1];
    private TableTennisAgent _lastHitter;

    private Rigidbody ballRigidbody;

    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // fall to the floor
        if (collision.collider.CompareTag("floor"))
        {
            //Agents[0].BallDropped();
            //Agents[1].BallDropped();
            //Debug.Log(_lastHitter.ToString());
            Agents[0].BallDropped();
        }

        // Hit the racket
        if (collision.collider.transform.parent != null && collision.collider.transform.parent.CompareTag("racket"))
        {
            //Debug.Log("racket hit");
            var agent = collision.collider.transform.parent.GetComponent<TableTennisAgent>();
            _lastHitter = agent;
            agent.BallHit();
        }

        // bounces on the table
        if (collision.collider.CompareTag("table"))
        {
            //Debug.Log("valid table bounce");
            Agents[0]?.BallBounced(collision.collider);
        }
    }

    void FixedUpdate()
    {
        float gravityScale = 0.3f; // 小於 1 時讓球下落變慢
        Vector3 gravity = Physics.gravity * gravityScale;
        ballRigidbody.AddForce(gravity - Physics.gravity, ForceMode.Acceleration);
    }
}
