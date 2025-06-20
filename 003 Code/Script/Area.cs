using UnityEngine;
using Unity.MLAgents;
// using System.Diagnostics;
using System;
using System.Security.Cryptography.X509Certificates;
 
public class Area : MonoBehaviour
{
    public GameObject DroneAgent; // 드론 에이전트 오브젝트
    public GameObject [] Goals;   // 목표 지점 오브젝트들
    public GameObject [] Obstacle;   // 장애물 오브젝트들

    Vector3 areaInitPos;          // Area 오브젝트의 초기 위치(환경의 초기 위치로 사용)
    Vector3 droneInitPos;         // 드론이 에피소드 시작 시 위치할 초기 위치(드론의 시작 위치)
    Quaternion droneInitRot;      // 드론이 에피소드 시작 시 회전할 초기 회전값(드론의 시작 회전)

    EnvironmentParameters m_ResetParams; // 학습에 필요한 환경 변수들

    public Transform AreaTrans;  // Area 오브젝트의 Transform, 위치/회전 정보 저장용
    public Transform DroneTrans; // Drone 오브젝트의 Transform, 위치/회전 정보 저장용
    public Transform [] GoalTrans;  // 목표 지점(Goal) 오브젝트들의 Transform
    public Transform [] ObstacleTrans; // 장애물 오브젝트들의 Transform

    private Rigidbody DroneAgent_Rigidbody; // Drone 오브젝트의 Rigidbody 컴포넌트 참조(물리 연산용)

    void Start() // Unity Start 함수, DroneSetting.cs가 활성화될 때 한 번만 실행
    {
        UnityEngine.Debug.Log(m_ResetParams);

        AreaTrans = gameObject.transform;   // 이 스크립트가 붙은 오브젝트(Area)의 Transform 참조
        DroneTrans = DroneAgent.transform;  // 드론 오브젝트의 Transform 참조
        GoalTrans = new Transform[Goals.Length];
        for (int i = 0; i < Goals.Length; i++)
        {
            GoalTrans[i] = Goals[i].transform;
        }
        
        ObstacleTrans = new Transform[Obstacle.Length];
        for (int i = 0; i < Obstacle.Length; i++)
        {
            ObstacleTrans[i] = Obstacle[i].transform;
        }
        

        areaInitPos = AreaTrans.position;     // Area의 초기 위치 저장
        droneInitPos = DroneTrans.position;   // 드론의 초기 위치 저장
        droneInitRot = DroneTrans.rotation;   // 드론의 초기 회전값 저장

        DroneAgent_Rigidbody = DroneAgent.GetComponent<Rigidbody>();  // 드론의 Rigidbody 컴포넌트 참조
    }

    public void AreaSetting()  // 환경을 재설정할 때 호출, 에피소드마다 상태 초기화
    {
        DroneAgent_Rigidbody.velocity = Vector3.zero; // 드론의 속도를 0으로 초기화
        DroneAgent_Rigidbody.angularVelocity = Vector3.zero;  // 드론의 각속도를 0으로 초기화

        DroneTrans.position = droneInitPos;     // 드론 위치를 초기 위치로 복원
        DroneTrans.rotation = droneInitRot;     // 드론 회전을 초기 회전값으로 복원

        for (int i = 0; i < Goals.Length; i++)
        {
            float randomX = UnityEngine.Random.Range(-5f, 5f);
            float randomZ = UnityEngine.Random.Range(-5f, 5f);
            float randomY = UnityEngine.Random.Range(0f, 5f);
            GoalTrans[i].position = areaInitPos + new Vector3(randomX, randomY, randomZ);
        }
        Debug.Log("목표 지점 위치 초기화");
        // for (int i = 0; i < Goals.Length; i++)
        // {
        //     // 목표 지점 위치를 Area 기준으로 무작위 배치
        //     GoalTrans[i].position = areaInitPos + new Vector3(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(7f, 7f));
        // }

        // float max = Vector3.Magnitude(GoalTrans[0].position- DroneTrans.position);
        // int maxIndex=0;
        // float min = Vector3.Magnitude(GoalTrans[0].position- DroneTrans.position);
        // int minIndex=0;
        // for (int i = 0; i < Goals.Length; i++)
        // { 
        //     if(max<Vector3.Magnitude(GoalTrans[i].position- DroneTrans.position))
        //     {
        //         max=Vector3.Magnitude(GoalTrans[i].position- DroneTrans.position);
        //         maxIndex=i;
        //     }    
        //     if(min>Vector3.Magnitude(GoalTrans[i].position- DroneTrans.position))
        //     {
        //         min=Vector3.Magnitude(GoalTrans[i].position- DroneTrans.position);
        //         minIndex=i;
        //     }  
        // }
        
        
        // float minX = Math.Min(GoalTrans[minIndex].position.x, GoalTrans[maxIndex].position.x); // X축 최소값
        // float maxX = Math.Max(GoalTrans[minIndex].position.x, GoalTrans[maxIndex].position.x);  // X축 최대값
        // float minY = Math.Min(GoalTrans[minIndex].position.y, GoalTrans[maxIndex].position.y); // Y축 최소값
        // float maxY = Math.Max(GoalTrans[minIndex].position.y, GoalTrans[maxIndex].position.y);  // Y축 최대값
        // float minZ = Math.Min(GoalTrans[minIndex].position.z, GoalTrans[maxIndex].position.z); // Z축 최소값
        // float maxZ = Math.Max(GoalTrans[minIndex].position.z, GoalTrans[maxIndex].position.z);  // Z축 최대값
        // }
        
    }
}