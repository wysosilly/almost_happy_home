using UnityEngine;
using System;

public class ExpansionButton : MonoBehaviour
{
    public Vector2Int cellPos;
    public Action<Vector2Int> onClicked;
    /// <summary>true면 Start에서 콜라이더 추가 안 함 (GridGameManager가 1x1 콜라이더만 쓸 때)</summary>
    public bool skipAddColliderInStart;

    void Start() {
        if (skipAddColliderInStart) return;
        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null) {
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(10f, 2f, 10f);
        }
    }

    // 마우스 직접 클릭 감지
    void OnMouseDown() {
        OnClickedManually();
    }

    // GameManager에서 레이캐스트로 호출할 수 있는 메서드
    public void OnClickedManually() {
        onClicked?.Invoke(cellPos);
    }
}
