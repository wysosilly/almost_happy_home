using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoundConfig
{
    [Tooltip("이 라운드에서 Happy 체크를 할 턴 수 (상대적 턴 수)")]
    public int turnsInRound = 5;

    [Tooltip("이 라운드 종료 시 요구하는 Happy 총합")]
    public int requiredHappy = 10;
}

[System.Serializable]
public class FurnitureSpawnInfo
{
    public GameObject prefab;
    public Vector2Int gridPos;
    public int rotationSteps;
}

[System.Serializable]
public class UpcomingDelivery
{
    public Vector2Int gridPos;
    public Vector2Int size;
    public List<Vector2Int> shapeCells;
    public int turnsLeft;
    public GameObject prefab;
}
