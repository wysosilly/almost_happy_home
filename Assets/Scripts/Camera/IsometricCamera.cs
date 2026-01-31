using UnityEngine;

/// <summary>
/// 카메라에 붙여서 사용. Inspector에서 컴포넌트 우클릭 → "아이소메트릭 적용" 선택
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsometricCamera : MonoBehaviour
{
    [ContextMenu("아이소메트릭 적용")]
    public void ApplyIsometric()
    {
        Camera cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        transform.position = new Vector3(0, 12, 12);
        transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
    }
}
