using UnityEngine;
using System.Collections.Generic;

public class StageGenerator
{
    public static StageData GenerateStage(int stageIndex, StageData existingData = null)
    {
        // 난이도 곡선 설계:
        // 스테이지가 올라갈수록 방이 조금씩 커지고, 요구 행복도가 기하급수적으로 상승합니다.

        StageData stage;
        int size;

        if (existingData != null)
        {
            // 사용자가 미리 설정한 데이터가 있다면 기반으로 사용
            stage = existingData;
            size = stage.gridWidth;
            // 기존 라운드 정보 초기화 (동적 생성을 위해)
            stage.rounds = new List<RoundConfig>();
        }
        else
        {
            // 데이터가 없다면 완전 자동 생성
            size = 8 + (stageIndex / 2);
            size = Mathf.Min(size, 15);
            stage = StageData.CreateDynamicStage($"Room {stageIndex + 1}", size, size);
        }

        // 2. 라운드 구성 (스테이지당 3라운드)
        int roundCount = 3;
        for (int i = 0; i < roundCount; i++)
        {
            RoundConfig round = new RoundConfig();
            
            // 라운드당 턴 수: 기본 4턴, 라운드가 지날수록 조금씩 증가
            round.turnsInRound = 4 + i;
            
            // 요구 행복도 공식: (스테이지 난이도 보정) + (라운드 진행도 보정)
            // 스테이지 1: 15, 30, 50
            // 스테이지 2: 40, 65, 95 ...
            int stageBase = stageIndex * 25;
            int roundBonus = (i + 1) * 15 + (i * i * 5);
            round.requiredHappy = stageBase + roundBonus;
            
            stage.rounds.Add(round);
        }

        // 3. 초기 장애물/가구 배치 (기존 데이터가 없을 때만 추가 생성)
        if (existingData == null)
        {
            int obstacleCount = Mathf.Min(stageIndex, 5); 
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector2Int randomPos = new Vector2Int(
                    Random.Range(0, size - 2), 
                    Random.Range(0, size - 2)
                );
                stage.dynamicObstaclePositions.Add(randomPos);
            }
        }

        return stage;
    }
}
