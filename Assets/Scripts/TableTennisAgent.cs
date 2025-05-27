using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

public class TableTennisAgent : Agent
{
    public Transform Ball;
    public Transform Table;
    public Collider TableCollider_1;
    public Collider TableCollider_2;
    public Collider moveArea_1;
    public Collider moveArea_2;
    public Transform ahchor1;
    public Transform ahchor2;

    private Collider opponentArea;
    private Collider moveArea;
    private Transform anchor;
    private Transform Racket;
    private Vector3 defaultRacketPos;
    private Quaternion defaultRacketRot;
    private Vector3 beforeRacketPos;
    private Rigidbody racketRb;
    private Rigidbody ballRb;

    private bool isServing;
    private bool isHitable;
    private int bounceCount;

    public override void Initialize()
    {
        Racket = this.transform;
        defaultRacketPos = Racket.position;
        defaultRacketRot = Racket.rotation;
        beforeRacketPos = defaultRacketPos;

        racketRb = GetComponent<Rigidbody>();
        ballRb = Ball.GetComponent<Rigidbody>();

        opponentArea = Vector3.Distance(Racket.position, TableCollider_1.transform.position) < Vector3.Distance(Racket.position, TableCollider_2.transform.position) ? TableCollider_2 : TableCollider_1;
        moveArea = Vector3.Distance(Racket.position, moveArea_1.transform.position) < Vector3.Distance(Racket.position, moveArea_2.transform.position) ? moveArea_1 : moveArea_2;
        anchor = Vector3.Distance(Racket.position, ahchor1.transform.position) < Vector3.Distance(Racket.position, ahchor2.transform.position) ? ahchor1 : ahchor2;
    }

    public override void OnEpisodeBegin()
    {
        // 隨機選擇這一局是發球還是接球
        isServing = Random.value < 0.5f;

        var b = moveArea.bounds;
        float x = Random.Range(b.min.x, b.max.x);
        Racket.position = new Vector3(x, defaultRacketPos.y, defaultRacketPos.z);
        Racket.rotation = defaultRacketRot;

        if (isServing)
        {
            Ball.position = Racket.position + Racket.forward * 0.2f + Vector3.up * 0.8f;
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.AddForce(Vector3.up * 0.1f);
        }
        else
        {
            Ball.position = opponentArea.transform.position + Vector3.up * 0.8f;
            Vector3 towardMe = (Racket.position - Ball.position).normalized;
            ballRb.linearVelocity = towardMe * 4f;
            ballRb.angularVelocity = Vector3.zero;
        }

        isHitable = true;
        bounceCount = 0;
        beforeRacketPos = Racket.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1) Ball position relative to the chosen anchor
        Vector3 localBallPos = anchor.InverseTransformPoint(Ball.position);
        sensor.AddObservation(localBallPos);

        // 2) Racket position relative to the same anchor
        Vector3 localRacketPos = anchor.InverseTransformPoint(Racket.position);
        sensor.AddObservation(localRacketPos);

        // 3) Ball velocity in anchor‐local axes
        Vector3 localBallVel = anchor.InverseTransformDirection(ballRb.linearVelocity);
        sensor.AddObservation(localBallVel);

        // 4) Ball rotational speed (angular velocity) in anchor‐local space
        Vector3 localBallAngVel = anchor.InverseTransformDirection(ballRb.angularVelocity);
        sensor.AddObservation(localBallAngVel);

        // 5) Racket rotation in anchor‐local space (as normalized Euler angles)
        Quaternion relRot = Quaternion.Inverse(anchor.rotation) * Racket.rotation;
        Vector3 localRacketEuler = relRot.eulerAngles / 360f;
        sensor.AddObservation(localRacketEuler);

        // bool isServing;
        // sensor.AddObservation(isServing);
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        //AddReward(1f / MaxStep);
        racketRb.transform.Translate(new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * Time.deltaTime);

        Collider[] colliders = Physics.OverlapSphere(racketRb.position, 0.002f);
        if (!colliders.Contains(moveArea))
        {
            AddReward(-0.02f);
            racketRb.transform.Translate(new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * Time.deltaTime * -1);
        }

        Racket.Rotate(new Vector3(1, 0, 0), Mathf.Clamp(actions.ContinuousActions[3] * 20, 0, 360));
        Racket.Rotate(new Vector3(0, 1, 0), Mathf.Clamp(actions.ContinuousActions[4] * 20, 0, 360));
        Racket.Rotate(new Vector3(0, 0, 1), Mathf.Clamp(actions.ContinuousActions[5] * 20, 0, 360));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        continuousActionsOut[1] = Input.GetKey(KeyCode.E) ? 1f : Input.GetKey(KeyCode.C) ? -1f : 0f;
        continuousActionsOut[2] = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;

        continuousActionsOut[3] = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;
        continuousActionsOut[4] = Input.GetKey(KeyCode.LeftArrow) ? 1f : Input.GetKey(KeyCode.RightArrow) ? -1f : 0f;
        continuousActionsOut[5] = 0f;
    }

    public void BallDropped()
    {
        Debug.Log("dropped");
        AddReward(-0.5f);
        EndEpisode();
    }

    public void BallHit()
    {
        if (!isHitable) EndEpisode();
        AddReward(10f);
        isHitable = false;
    }

    public void BallBounced(Collider collidedZone)
    {
        bounceCount++;
        //Debug.Log($"Bounced {bounceCount}");
        //Debug.Log($"collidedZone.name {collidedZone.name}");
        //Debug.Log($"opponentArea {opponentArea.name}");
        if (bounceCount == 1 && !isServing)
        {
            if (collidedZone == opponentArea)
            {
                AddReward(-0.3f);
                EndEpisode();
                return;
            }
            else
            {
                AddReward(20f);
                Debug.Log("0.3f");
            }
        }
        else if (bounceCount == 2)
        {
            if (collidedZone == opponentArea)
            {
                AddReward(20f);
            }
            else
            {
                AddReward(-0.5f);
            }
            EndEpisode();
        }
    }

    void Update()
    {
        if (racketRb.position == beforeRacketPos)
        {
            Debug.DrawRay(racketRb.position, new Vector3(0, 0.5f, 0), Color.green);
            AddReward(-0.05f);
        }
        else
        {
            beforeRacketPos = racketRb.position;
        }
    }
}