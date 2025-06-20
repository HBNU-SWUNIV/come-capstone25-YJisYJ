using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class distanceUI : MonoBehaviour
{
    public Transform drone;
    public Transform[] goals;
    public TextMeshProUGUI dronePosText;
    public TextMeshProUGUI[] goalPosTexts;
    public TextMeshProUGUI homeCollisionText;
    public TextMeshProUGUI isTouchedObstacleText;

    private Drone droneScript;

    // 누적 카운트
    private int homeArrivedCount = 0;
    private int obstacleCollisionCount = 0;

    void Start()
    {
        droneScript = drone.GetComponent<Drone>();
    }

    void Update()
    {
        // 드론 위치 표시
        string droneText = $"Drone: ({drone.position.x:F1}, {drone.position.y:F1}, {drone.position.z:F1})";
        dronePosText.text = droneText;

        // 각 목표 위치, 거리, 도달 표시
        for (int i = 0; i < goals.Length; i++)
        {
            float dist = Vector3.Distance(drone.position, goals[i].position);
            string goalText = $"Goal{i + 1}: ({goals[i].position.x:F1}, {goals[i].position.y:F1}, {goals[i].position.z:F1}), distance: {dist:F2}";
            if (droneScript.check != null && droneScript.check.Length > i && droneScript.check[i] == 1)
            {
                goalText += "   Collision";
            }
            goalPosTexts[i].text = goalText;
        }

        // Home 도달 누적 카운트 표시
        homeCollisionText.text = $"homeCollision: {droneScript.homeArrivedCount}";

        // 장애물 충돌 누적 카운트 표시
        isTouchedObstacleText.text = $"obstacleCollision: {droneScript.obstacleCollisionCount}";

    }
}
