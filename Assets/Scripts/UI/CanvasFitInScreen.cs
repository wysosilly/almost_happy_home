using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1920x1080 기준으로 UI가 Scale 1x에서도 잘리지 않도록
/// scaleFactor를 min(너비비율, 높이비율)로 제한합니다.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasFitInScreen : MonoBehaviour
{
    public Vector2 referenceResolution = new Vector2(1920, 1080);

    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    void LateUpdate()
    {
        if (_canvas == null) return;
        float scaleW = Screen.width / referenceResolution.x;
        float scaleH = Screen.height / referenceResolution.y;
        _canvas.scaleFactor = Mathf.Min(scaleW, scaleH);
    }
}
