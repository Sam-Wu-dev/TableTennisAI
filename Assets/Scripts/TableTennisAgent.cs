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
    public TableTennisAgent Opponent;
    public Collider TableCollider_1;
    public Collider TableCollider_2;
    public Collider moveArea_1;
    public Collider moveArea_2;
    private bool isServing;

    private Collider MyOpponentArea;
    private Collider MyMoveArea;

    private Transform Racket;
    private Vector3 defaultRacketPos;
    private Quaternion defaultRacketRot;
    private Vector3 defaultBallPos;
    private Vector3 beforeRacketPos;
    private Rigidbody racketRb;
    private Rigidbody ballRb;
    private Collider opponentArea;
    private Collider moveArea;
    private bool isHitable;

    private int bounceCount = 0;

    public override void Initialize()
    {
        Debug.Log("initlaize called");
        Racket = this.transform;
        float posX = Racket.position.x;
        float posY = Racket.position.y;
        float posZ = Racket.position.z;
        defaultRacketPos = new Vector3(posX, posY, posZ);
        float rotX = Racket.rotation.x;
        float rotY = Racket.rotation.y;
        float rotZ = Racket.rotation.z;
        float rotW = Racket.rotation.w;
        defaultRacketRot = new Quaternion(rotX, rotY, rotZ, rotW);
        posX = Ball.position.x;
        posY = Ball.position.y;
        posZ = Ball.position.z;
        defaultBallPos = new Vector3(posX, posY, posZ);
        beforeRacketPos = defaultRacketPos;
        racketRb = GetComponent<Rigidbody>();
        ballRb = Ball.GetComponent<Rigidbody>();
        float oDistance_1 = Vector3.Distance(Racket.position, TableCollider_1.transform.position);
        float oDistance_2 = Vector3.Distance(Racket.position, TableCollider_2.transform.position);
        if (oDistance_1 < oDistance_2)
        {
            opponentArea = TableCollider_2;
        }
        if (oDistance_2 < oDistance_1)
        {
            opponentArea = TableCollider_1;
        }
        MyOpponentArea = opponentArea;
        float mDistance_1 = Vector3.Distance(Racket.position, moveArea_1.transform.position);
        float mDistance_2 = Vector3.Distance(Racket.position, moveArea_2.transform.position);
        if (mDistance_1 < mDistance_2)
        {
            moveArea = moveArea_1;
        }
        if (mDistance_2 < mDistance_1)
        {
            moveArea = moveArea_2;
        }
        MyMoveArea = moveArea;
    }

    public override void OnEpisodeBegin()
    {
        // 1) Decide who’s serving this episode
        isServing = Random.value < 0.5f;
        Opponent.isServing = !isServing;

        // 2) Randomize *this* racket’s start within its move‐area
        var b = moveArea.bounds;
        float x = Random.Range(b.min.x, b.max.x);
        Racket.position = new Vector3(x, defaultRacketPos.y, defaultRacketPos.z);
        Racket.rotation = defaultRacketRot;

        // 3) If *this* agent is the server, place the ball at its serve offset
        if (isServing)
        {
            Vector3 serveOffset = Racket.forward * 0.2f + Vector3.up * 0.8f;
            Ball.position = Racket.position + serveOffset;
            // clear any stray velocity
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
        }

        // 4) Reset hit/serve state for the rally
        isHitable = true;
        bounceCount = 0;
        beforeRacketPos = Racket.position;
    }



    public override void CollectObservations(VectorSensor sensor)
    {
        // 1) Vector from *your* racket to the ball (3 floats)
        Vector3 toBall = Ball.position - Racket.position;
        sensor.AddObservation(toBall);

        // 2) Ball’s current velocity (3 floats)
        sensor.AddObservation(ballRb.linearVelocity);

        // 3) Vector from the ball to *your* target zone (3 floats)
        //    (i.e. where you want to send the ball)
        Vector3 toGoal = opponentArea.transform.position - Ball.position;
        sensor.AddObservation(toGoal);

        // 4) Your racket’s orientation (we’ll normalize Euler angles to [0,1]) (3 floats)
        Vector3 normEuler = Racket.localEulerAngles / 360f;
        sensor.AddObservation(normEuler);

        bool isServePhase = isServing && bounceCount == 0;
        sensor.AddObservation(isServePhase ? 1f : 0f);
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(1f / MaxStep);
        racketRb.transform.Translate(
            new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * Time.deltaTime);
        Collider[] colliders = Physics.OverlapSphere(racketRb.position, 0.002f);
        if (!colliders.Contains(moveArea))
        {
            AddReward(-0.02f);
            racketRb.transform.Translate(
                new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * Time.deltaTime * -1);
        }
        Racket.Rotate(new Vector3(1, 0, 0), Mathf.Clamp(actions.ContinuousActions[3] * 20, 0, 360));
        Racket.Rotate(new Vector3(0, 1, 0), Mathf.Clamp(actions.ContinuousActions[4] * 20, 0, 360));
        Racket.Rotate(new Vector3(0, 0, 1), Mathf.Clamp(actions.ContinuousActions[5] * 20, 0, 360));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var DiscreteActionsOut = actionsOut.DiscreteActions;
        DiscreteActionsOut[0] = 10;
        if (Input.GetKey(KeyCode.W)) racketRb.position += transform.forward * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) racketRb.position += -transform.forward * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) racketRb.position += -transform.right * Time.deltaTime;
        else if (Input.GetKey(KeyCode.D)) racketRb.position += transform.right * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) racketRb.position += transform.up * Time.deltaTime;
        else if (Input.GetKey(KeyCode.C)) racketRb.position += -transform.up * Time.deltaTime;
        if (Input.GetKey(KeyCode.UpArrow)) Racket.Rotate(transform.forward, Time.deltaTime);
        else if (Input.GetKey(KeyCode.DownArrow)) Racket.Rotate(-transform.forward, Time.deltaTime);
        if (Input.GetKey(KeyCode.LeftArrow)) Racket.Rotate(transform.right, Time.deltaTime);
        else if (Input.GetKey(KeyCode.RightArrow)) Racket.Rotate(-transform.right, Time.deltaTime);
    }

    public void BallDropped()
    {
        AddReward(-0.5f);
        EndEpisode();
        Opponent.EndEpisode();
    }

    public void BallHit()
    {
        if (!isHitable) EndEpisode();
        AddReward(0.3f);
        isHitable = false;
    }

    public void BallBounced(Collider collidedZone)
    {
        if (isServing)
        {
            bounceCount++;

            // --- Serve-phase logic ---
            if (bounceCount == 1)
            {
                // must land on own side
                if (collidedZone == opponentArea)
                {
                    AddReward(-0.3f);
                    EndRound();
                }
                else
                {
                    AddReward(0.5f);
                }
                // after first bounce, we switch out of serve-phase
                isServing = false;
                bounceCount = 0;
                return;
            }
        }

        // --- Rally logic for both server (after bounce 1) and receiver ---
        if (collidedZone == opponentArea)
        {
            AddReward(0.15f);
            isHitable = true;
        }
        else
        {
            AddReward(-0.1f);
            EndRound();
        }
    }

    private void EndRound()
    {
        EndEpisode();
        Opponent.EndEpisode();
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
