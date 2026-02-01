using UnityEngine;

public interface IFurnitureEnhancement
{
    string EnhancementName { get; }
    string Description { get; }
    void Apply(Furniture furniture);
}

[System.Serializable]
public class HappyEnhancement : IFurnitureEnhancement
{
    public string EnhancementName => "행복 증진";
    public string Description => "턴 종료 시 획득하는 행복 수치를 1 올립니다.";

    public void Apply(Furniture furniture)
    {
        furniture.happyValue += 1;
    }
}

[System.Serializable]
public class ActionPointEnhancement : IFurnitureEnhancement
{
    public string EnhancementName => "활동성 강화";
    public string Description => "매 턴 액션 포인트를 1 추가로 얻습니다.";

    public void Apply(Furniture furniture)
    {
        furniture.permanentBonusAP += 1;
    }
}

[System.Serializable]
public class GridExpansionEnhancement : IFurnitureEnhancement
{
    public string EnhancementName => "공간 확장";
    public string Description => "방의 외곽에 새로운 그리드 칸을 1칸 추가합니다.";

    public void Apply(Furniture furniture)
    {
        // 실제 적용은 GridGameManager의 콜백에서 처리되므로 여기서는 플래그만 설정하거나 
        // GridGameManager에 위임하는 방식이 필요합니다.
        // 하지만 현재 Apply 구조상 즉시 실행되므로, GameManager에 선택 모드 진입을 요청합니다.
    }
}
