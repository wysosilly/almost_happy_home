using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class UpcomingDelivery
{
    public Vector2Int gridPos;
    public Vector2Int size;
    public List<Vector2Int> shapeCells;
    public int turnsLeft;
    public GameObject prefab;
}

[System.Serializable]
public class StageConfig
{
    [Tooltip("이 턴 수에 도달하면 Happy 체크")]
    public int turnCheck = 5;

    [Tooltip("이 스테이지에서 요구하는 Happy 총합 (미달 시 게임 오버)")]
    public int requiredHappy = 10;
}
