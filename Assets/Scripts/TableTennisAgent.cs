using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;
using Unity.MLAgents.Policies;

public class TableTennisAgent : Agent
{
    public Transform Ball;
    public Transform Table;
    public Collider TableCollider_1;
    public Collider TableCollider_2;
    public Collider LastCollider;
    public Collider moveArea_1;
    public Collider moveArea_2;
    public Transform ahchor1;
    public Transform ahchor2;

    private int teamId;
    private static int serverTeamId = 0;
    private static int serveCount = 0;
    private const int maxServeCount = 2;    // 發球次數上限
    private Collider opponentArea;
    private Collider moveArea;
    private Transform anchor;
    private Transform Racket;
    private Vector3 defaultRacketPos;
    private Quaternion defaultRacketRot;
    private Vector3 beforeRacketPos;
    private Rigidbody racketRb;
    private Rigidbody ballRb;

    private static bool isFirstInEpisode = true; 
    public bool isServing;
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
        teamId = GetComponent<BehaviorParameters>().TeamId;

        opponentArea = Vector3.Distance(Racket.position, TableCollider_1.transform.position) < Vector3.Distance(Racket.position, TableCollider_2.transform.position) ? TableCollider_2 : TableCollider_1;
        moveArea = Vector3.Distance(Racket.position, moveArea_1.transform.position) < Vector3.Distance(Racket.position, moveArea_2.transform.position) ? moveArea_1 : moveArea_2;
        anchor = Vector3.Distance(Racket.position, ahchor1.transform.position) < Vector3.Distance(Racket.position, ahchor2.transform.position) ? ahchor1 : ahchor2;
    }

    public override void OnEpisodeBegin()
    {
        // 隨機選擇這一局是發球還是接球
        //isServing = Random.value < 0.5f;
        if (isFirstInEpisode && teamId == serverTeamId)
        {
            serveCount++;

            if (serveCount > maxServeCount)
            {
                serverTeamId = 1 - serverTeamId;
                serveCount = 0;
            }

            isFirstInEpisode = false;
        }

        isServing = (teamId == serverTeamId);
        LastCollider = null;
        //Debug.Log($"{teamId} serverCount : {serveCount} {isServing}");

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
        //else
        //{
        //    Ball.position = opponentArea.transform.position + Vector3.up * 0.8f;
        //    Vector3 towardMe = (Racket.position - Ball.position).normalized;
        //    ballRb.linearVelocity = towardMe * 4f;
        //    ballRb.angularVelocity = Vector3.zero;
        //}

        isHitable = true;
        bounceCount = 0;
        beforeRacketPos = Racket.position;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // 把所有obs都轉到anchor space上
        sensor.AddObservation(anchor.InverseTransformPoint(Ball.position));        // 3
        sensor.AddObservation(anchor.InverseTransformPoint(Racket.position));      // 3
        sensor.AddObservation(anchor.InverseTransformDirection(ballRb.linearVelocity));  // 3
        sensor.AddObservation(anchor.InverseTransformDirection(ballRb.angularVelocity)); //3

        // Ball和Racket的相對向量
        sensor.AddObservation(anchor.InverseTransformDirection(Ball.position - Racket.position)); //3

        // 方向
        Quaternion relRot = Quaternion.Inverse(anchor.rotation) * Racket.rotation;
        Vector3 e = relRot.eulerAngles * Mathf.Deg2Rad;
        sensor.AddObservation(Mathf.Sin(e.x)); sensor.AddObservation(Mathf.Cos(e.x));
        sensor.AddObservation(Mathf.Sin(e.y)); sensor.AddObservation(Mathf.Cos(e.y));
        sensor.AddObservation(Mathf.Sin(e.z)); sensor.AddObservation(Mathf.Cos(e.z)); // 6

        // 判斷發球 擊球 
        sensor.AddObservation(isServing ? 1f : 0f);
        sensor.AddObservation(isHitable ? 1f : 0f);
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(0.001f);
        //racketRb.transform.Translate(new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * Time.deltaTime);

        //Collider[] colliders = Physics.OverlapSphere(racketRb.position, 0.002f);
        //if (!colliders.Contains(moveArea))
        //{
        //    AddReward(-0.02f);
        //    racketRb.transform.Translate(new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]) * Time.deltaTime * -1);
        //}

        //Racket.Rotate(new Vector3(1, 0, 0), Mathf.Clamp(actions.ContinuousActions[3] * 20, 0, 360));
        //Racket.Rotate(new Vector3(0, 1, 0), Mathf.Clamp(actions.ContinuousActions[4] * 20, 0, 360));
        //Racket.Rotate(new Vector3(0, 0, 1), Mathf.Clamp(actions.ContinuousActions[5] * 20, 0, 360));

        // Extract movement and rotation actions
        Vector3 localMove = new Vector3(
            actions.ContinuousActions[0],
            actions.ContinuousActions[1],
            actions.ContinuousActions[2]
        );

        // Convert local movement direction into world space using the anchor's orientation
        Vector3 worldMove = anchor.TransformDirection(localMove.normalized) * localMove.magnitude * Time.deltaTime;

        // Try to move the racket
        racketRb.transform.position += worldMove;

        // Check if racket is still within the allowed move area
        Collider[] colliders = Physics.OverlapSphere(racketRb.position, 0.002f);
        if (!colliders.Contains(moveArea))
        {
            AddReward(-0.02f);
            // Revert movement if out of bounds
            racketRb.transform.position -= worldMove;
        }

        // Apply rotation (still in world space for now)
        float rotX = Mathf.Clamp(actions.ContinuousActions[3] * 20, 0, 360);
        float rotY = Mathf.Clamp(actions.ContinuousActions[4] * 20, 0, 360);
        float rotZ = Mathf.Clamp(actions.ContinuousActions[5] * 20, 0, 360);
        Racket.Rotate(new Vector3(1, 0, 0), rotX, Space.Self);
        Racket.Rotate(new Vector3(0, 1, 0), rotY, Space.Self);
        Racket.Rotate(new Vector3(0, 0, 1), rotZ, Space.Self);

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
        //Debug.Log("dropped");
        AddReward(-0.5f);
        isFirstInEpisode = true;
        EndEpisode();
    }

    public void BallHit()
    {
        //if (!isHitable) EndEpisode();
        AddReward(10f);
        //isHitable = false;
    }

    public bool BallBounced(Collider collidedZone, TableTennisAgent _previousHitter, TableTennisAgent _lastHitter)
    {
        bounceCount++;
        //Debug.Log($"{teamId} : Bounced {bounceCount}");
        //Debug.Log($"collidedZone.name {collidedZone.name}");
        //Debug.Log($"opponentArea {opponentArea.name}");
        if (isServing)  // 發球方
        {
            if (bounceCount == 1)
            {
                if (collidedZone == opponentArea)
                {
                    AddReward(-5f);
                    Debug.Log("-5f");
                    isFirstInEpisode = true;
                    return false;
                }
                else
                {
                    AddReward(5f);
                    Debug.Log("5f");
                    LastCollider = collidedZone;
                    return true;
                }
            }
            else if (bounceCount == 2)
            {
                if (collidedZone == opponentArea)
                {
                    AddReward(5f);
                    Debug.Log("5f");
                    LastCollider = collidedZone;
                    return true;
                }
                else
                {
                    AddReward(-5f);
                    Debug.Log("-5f");
                    isFirstInEpisode = true;
                    return false;
                }
            }
            else if (bounceCount >= 3) // 對方擊球後又打過去 或是發球後在對方區域連彈兩次 我方贏
            {
                if (collidedZone == opponentArea)
                {
                    if (_previousHitter == null) // 代表發球過去後 彈對面桌子第二下 我方贏
                    {
                        AddReward(20f);
                        Debug.Log("20f _previousHitter == null");
                        isFirstInEpisode = true;
                        return false;
                    }
                    else if (_previousHitter == this) // 我方打過去對面桌子
                    {
                        AddReward(20f);
                        Debug.Log("20f _previousHitter == this");
                        LastCollider = collidedZone;
                        return true;
                    }
                }
                else
                {
                    AddReward(-5f);
                    Debug.Log("-5f");
                    isFirstInEpisode = true;
                    return false;
                }
            }
        }
        else if (!isServing)    // 擊球方
        {
            if (bounceCount == 1)
            {
                // 應該落在對方桌上
                if (collidedZone == opponentArea)
                {
                    AddReward(20f);
                    Debug.Log("20f collidedZone == opponentArea");
                    LastCollider = collidedZone;
                    return true; // 回合繼續
                }
                else
                {
                    AddReward(-5f);
                    Debug.Log("-5f");
                    isFirstInEpisode = true;
                    return false;
                }
            }
            else if (bounceCount >= 2)
            {
                // 打第二次
                if (collidedZone == opponentArea)
                {
                    if (_lastHitter == this && LastCollider == opponentArea)
                    {
                        // 球落對面 且在對面桌子上彈兩下
                        AddReward(20f);
                        Debug.Log("20f");
                        isFirstInEpisode = true;
                        return false;
                    }
                    else
                    {
                        // 球落對面且上一個是對方打的 那就繼續
                        AddReward(20f);
                        Debug.Log("20f");
                        LastCollider = collidedZone;
                        return true;
                    }
                }
                else
                {
                    // 球落自己桌子
                    AddReward(-5f);
                    Debug.Log("-5f");
                    isFirstInEpisode = true;
                    return false;
                }
            }
        }
        return true;
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

    public void setisFirstInEpisode()
    {
        isFirstInEpisode = true;
    }
}