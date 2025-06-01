using System.Collections;
using UnityEngine;
using Unity.MLAgents;

public class Ball : MonoBehaviour
{
    [Tooltip("Agents hitting this ball.")]
    public TableTennisAgent[] Agents = new TableTennisAgent[2];
    private TableTennisAgent _lastHitter;

    private Rigidbody ballRigidbody;

    private float tableContactTimer = 0f;
    private const float minTableContactTime = 1f;
    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    private void EndEpisode()
    {
        foreach (var agent in Agents)
        {
            agent.EndEpisode();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("table"))
        {
            tableContactTimer += Time.deltaTime;
            if (tableContactTimer >= minTableContactTime)
            {
                EndEpisode();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("table"))
        {
            // left the table: reset everything
            tableContactTimer = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // fall to the floor
        if (collision.collider.CompareTag("floor"))
        {
            if (_lastHitter != null)
            {
                foreach (var agent in Agents)
                {
                    if (agent == _lastHitter)
                    {
                        agent.AddReward(+1.0f);  // 上一個擊球者得分
                    }
                    else
                    {
                        agent.AddReward(-1.0f);  // 沒接到球的人扣分
                    }
                }
            }
            else
            {
                // 沒人擊球就掉地 → 代表發球方失誤
                foreach (var agent in Agents)
                {
                    if (agent.isServing) 
                    {
                        agent.AddReward(-1.0f);  // 發球方被罰
                    }
                    else
                    {
                        agent.AddReward(+1.0f);  // 對手得分
                    }
                }
            }

            Agents[0].BallDropped();
            Agents[1].BallDropped();
            //Debug.Log(_lastHitter.ToString());
            // Agents[0].BallDropped();
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
            tableContactTimer = 0f;
            //Debug.Log("valid table bounce");
            bool legal = true;
            if (_lastHitter != null)
            {
                legal = _lastHitter.BallBounced(collision.collider);
            }
            Debug.Log(legal);
            if (!legal)
            {
                foreach (var agent in Agents)
                {
                    agent.EndEpisode();
                }
            }

        }
    }

    void FixedUpdate()
    {
        float gravityScale = 0.3f; // 小於 1 時讓球下落變慢
        Vector3 gravity = Physics.gravity * gravityScale;
        ballRigidbody.AddForce(gravity - Physics.gravity, ForceMode.Acceleration);
    }
}
