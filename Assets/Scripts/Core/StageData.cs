using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStage", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName = "새로운 방";
    public int gridWidth = 10;
    public int gridHeight = 10;
    
    [Tooltip("이 스테이지를 구성하는 라운드들")]
    public List<RoundConfig> rounds = new List<RoundConfig>();

    [Tooltip("스테이지 시작 시 기본으로 주어지는 가구들 (선택 사항)")]
    public List<FurnitureSpawnInfo> initialFurnitures = new List<FurnitureSpawnInfo>();

    [HideInInspector]
    public List<Vector2Int> dynamicObstaclePositions = new List<Vector2Int>();

    // 런타임 생성을 위한 정적 메서드
    public static StageData CreateDynamicStage(string name, int width, int height)
    {
        StageData stage = ScriptableObject.CreateInstance<StageData>();
        stage.stageName = name;
        stage.gridWidth = width;
        stage.gridHeight = height;
        stage.rounds = new List<RoundConfig>();
        stage.initialFurnitures = new List<FurnitureSpawnInfo>();
        stage.dynamicObstaclePositions = new List<Vector2Int>();
        return stage;
    }
}
