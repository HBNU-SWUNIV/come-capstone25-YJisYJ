using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PA_DronePack;
using System;

// 드론 에이전트 클래스 (ML-Agents를 상속)
public class Drone : Agent
{
    // 드론 컨트롤러 스크립트 참조
    private PA_DroneController dcoScript;
    // 드론이 속한 에이리어
    public Area area;
    // 목표 지점 오브젝트 배열
    public GameObject[] goals;
    // 장애물 오브젝트 배열
    public GameObject[] obstacle;

    float preDist; // 이전 목표와의 거리
    float[] min; // 각 장애물과의 최소 거리
    public Transform agentTrans; // 드론 Transform
    public Transform[] goalTrans; // 목표 Transform 배열
    public Transform[] ObstacleTrans; // 장애물 Transform 배열
    public int[] check; // 각 목표 도달 여부 체크
    public Rigidbody agent_Rigidbody; // 드론 Rigidbody
    int[] order; // 목표 방문 순서
    int GoalSequence = 0; // 현재 목표 인덱스
    int count = 0; // 도달한 목표 개수

    public GameObject home; // 홈 오브젝트
    public Transform homeTrans; // 홈 Transform
    private bool goHome = false; // 모든 목표를 돌면 home 모드

    private Renderer goalRenderer;
    public bool homeCollision = false;
    public bool AllGoalCollision = true;

    // 에이전트 초기화 함수
    public override void Initialize()
    {
        dcoScript = gameObject.GetComponent<PA_DroneController>();
        agentTrans = gameObject.transform;
        goalTrans = new Transform[goals.Length];
        ObstacleTrans = new Transform[obstacle.Length];
        check = new int[goals.Length];
        order = new int[goals.Length];
        for (int i = 0; i < goals.Length; i++)
        {
            goalTrans[i] = goals[i].transform;
            check[i] = 0;
        }
        for (int i = 0; i < obstacle.Length; i++)
        {
            ObstacleTrans[i] = obstacle[i].transform;
        }
        agent_Rigidbody = gameObject.GetComponent<Rigidbody>();
        Academy.Instance.AgentPreStep += WaitTimeInference; // 결정 주기 설정
        homeTrans = home.transform;
    }

    // 환경 관측값 수집 (드론-목표/홈 상대 위치, 속도 등)
    public override void CollectObservations(VectorSensor sensor)
    {
        if (!goHome)
        {
            // Goal로 이동할 때: 드론과 현재 목표의 상대 위치
            sensor.AddObservation(agentTrans.position - goalTrans[GoalSequence].position);
        }
        else
        {
            // Home 모드일 때: 드론과 home의 상대 위치
            sensor.AddObservation(agentTrans.position - homeTrans.position);
        }
        sensor.AddObservation(agent_Rigidbody.velocity); // 드론 속도
        sensor.AddObservation(agent_Rigidbody.angularVelocity); // 드론 각속도
    }

    private bool isTouchedGoal = false; // 목표 도달 여부
    public bool isTouchedObstacle = false; // 장애물 충돌 여부

    // 트리거 충돌 처리 (목표, 홈, 장애물)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            isTouchedGoal = true;
        }
        // else if (other.CompareTag("Home"))
        // {
        //     Debug.Log("Home 도달, 에피소드 종료");
        //     SetReward(2.0f); // 홈 도달 보상
        //     EndEpisode(); // 에피소드 종료
        // }
        else if (other.CompareTag("Obstacle"))
        {
            isTouchedObstacle = true;
        }
    }

    public int homeArrivedCount = 0;
    public int obstacleCollisionCount = 0;

    // 에이전트가 행동을 받았을 때 호출
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        for (int i = 0; i < obstacle.Length; i++)
        {
            float obsDist = Vector3.Distance(ObstacleTrans[i].position, agentTrans.position);
            if (obsDist <= 1.5f)
                AddReward(-0.1f*obsDist);   // 더 강하게
        }


        // 장애물 충돌 시 에피소드 종료
        if (isTouchedObstacle)
        {
            SetReward(-15f);
            Debug.Log("장애물에 충돌했으면 에피소드 종료");
            obstacleCollisionCount++;
            EndEpisode();
            isTouchedObstacle = false;
            return;
        }

        AddReward(-0.01f); // 시간 패널티

        var actions = actionBuffers.ContinuousActions;
        float moveX = Mathf.Clamp(actions[0], -1, 1f); // 전진/후진
        float moveY = Mathf.Clamp(actions[1], -1, 1f); // 좌/우
        float moveZ = Mathf.Clamp(actions[2], -1, 1f); // 상/하

        dcoScript.DriveInput(moveX); // 드론 전진/후진 입력
        dcoScript.StrafeInput(moveY); // 드론 좌/우 입력
        dcoScript.LiftInput(moveZ); // 드론 상/하 입력

        // Goal 또는 Home 이동 로직
        if (goHome){
            float homeDist = Vector3.Distance(agentTrans.position, homeTrans.position);
            if (homeDist <= 1.0f)
            {
                homeCollision = true;
                SetReward(10.0f);
                homeArrivedCount++;
                Debug.Log("Home 도달, 에피소드 종료 (distance=" + homeDist + ")");
                EndEpisode();
            }
            homeCollision = false;
            return;
        }
        homeCollision = false;

        float distance = Vector3.Magnitude(goalTrans[GoalSequence].position - agentTrans.position); // 현재 목표와의 거리
        for (int i = 0; i < obstacle.Length; i++)
        {
            float obsDist = Vector3.Magnitude(ObstacleTrans[i].position - agentTrans.position); // 장애물과의 거리
            if (obsDist <= 0.05f)
            {
                SetReward(-1f); // 장애물 근접 패널티
            }
            else if (obsDist <= 1.5f && distance > obsDist)
            {
                // 장애물에 가까워지면 보상 감소
                if (min[i] > obsDist)
                    min[i] = obsDist;
                else
                    AddReward(obsDist - min[i]);
            }
        }

        // if (isTouchedGoal&&distance<=1.5f)
        if (distance<=1.5f)
        {
            count++;
            SetReward(2.0f); // 목표 도달 보상
            check[GoalSequence] = 1; // 목표 도달 체크
            isTouchedGoal = false;
            Debug.Log("Goal 도착 " + count + ", distance: " + distance);

            // === [여기서 색상 변경] ===
            Renderer goalRenderer = goals[GoalSequence].GetComponent<Renderer>();
            if (goalRenderer != null)
                goalRenderer.material.color = Color.green;
            // =========================

            // 다음 목표 찾기 (가장 가까운 미방문 목표)
            float closeGoal = 100f;
            for (int i = 0; i < goals.Length; i++)
            {
                if (check[i] != 1 && Vector3.Magnitude(goalTrans[GoalSequence].position - goalTrans[i].position) < closeGoal)
                {
                    closeGoal = Vector3.Magnitude(goalTrans[GoalSequence].position - goalTrans[i].position);
                    GoalSequence = i;
                }
            }
            int c = 0;
            for (; c < goals.Length; c++)
                if (check[c] != 1) break;

            if (c == goals.Length)
            {
                AllGoalCollision = true;
                SetReward(1.0f); // 모든 목표 도달 시 추가 보상
                goHome = true; // 홈 모드로 전환
                Debug.Log("모든 목표 도달, Home 이동 시작");
            }
            else
            {
                preDist = Vector3.Magnitude(goalTrans[GoalSequence].position - agentTrans.position); // 다음 목표와의 거리 갱신
            }
        }
        else if (distance > 15f)
        {
            SetReward(-1f); // 목표와 너무 멀어지면 패널티
            Debug.Log("목표 지점과 너무 멀어지면 에피소드 종료");
            EndEpisode();
        }
        else
        {
            float reward = preDist - distance; // 목표에 가까워질수록 보상
            AddReward(reward);
            preDist = distance;
        }
        // goHome==true일 때는 Home 도달을 OnTriggerEnter에서 처리
    }

    // 에피소드 시작 시 초기화
    public override void OnEpisodeBegin()
    {
        count = 0;
        GoalSequence = 0;
        goHome = false;
        area.AreaSetting(); // 에이리어 초기화

        float[] index = new float[goals.Length];
        min = new float[obstacle.Length];
        for (int i = 0; i < goals.Length; i++)
        {
            check[i] = 0;
            index[i] = Vector3.Magnitude(goalTrans[i].position - agentTrans.position); // 각 목표와의 거리

            // 목표 오브젝트 색상 초기화
            Renderer r = goals[i].GetComponent<Renderer>();
            if (r != null)
                r.material.color = Color.red;
        }
        Array.Sort(index); // 거리순 정렬
        for (int i = 0; i < goals.Length; i++)
        {
            for (int ch = 0; ch < goals.Length; ch++)
            {
                if (index[ch] == Vector3.Magnitude(goalTrans[i].position - agentTrans.position))
                {
                    order[ch] = i;
                    break;
                }
            }
        }
        GoalSequence = order[0]; // 가장 가까운 목표부터 시작
        for (int i = 0; i < obstacle.Length; i++)
        {
            min[i] = Vector3.Magnitude(ObstacleTrans[i].position - agentTrans.position); // 장애물 최소 거리 초기화
        }
        preDist = Vector3.Magnitude(goalTrans[GoalSequence].position - agentTrans.position); // 첫 목표와의 거리

        // 각 목표의 OnEpisodeBegin 호출 (상태 초기화)
        foreach (var g in goals)
        {
            Goal_Collision gc = g.GetComponent<Goal_Collision>();
            if (gc != null)
                gc.OnEpisodeBegin();
        }

        isTouchedGoal = false;
        homeCollision = false;
        AllGoalCollision = false;

    }

    // 휴리스틱(수동 조작) 입력 처리
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical"); // 전진/후진
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); // 좌/우
        continuousActionsOut[2] = Input.GetAxis("Mouse ScrollWheel"); // 상/하
    }

    public float DecisionWaitingTime = 5f; // 결정 대기 시간
    float m_currentTime = 0f; // 현재 대기 시간

    // 결정 주기 제어 함수
    public void WaitTimeInference(int action)
    {
        if (Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if (m_currentTime >= DecisionWaitingTime)
            {
                m_currentTime = 0f;
                RequestDecision();
            }
            else
            {
                m_currentTime += Time.fixedDeltaTime;
            }
        }
    }
}
