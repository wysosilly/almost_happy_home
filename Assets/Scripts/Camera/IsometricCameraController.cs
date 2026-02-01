using UnityEngine;

/// <summary>
/// WASD: 이동, Q/E: 그리드 중앙 기준 회전
/// GridGameManager.SetupScene()에서 Init() 호출 필요
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsometricCameraController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 8f;

    [Header("회전")]
    public float rotateSpeed = 60f;
    public float tiltAngle = 35f;
    [Tooltip("비워두면 Init으로 받은 값 사용")]
    public Transform orbitPivot;

    [Header("줌 (마우스 스크롤)")]
    public float scrollZoomSpeed = 2f;
    public float orthoSizeMin = 2f;
    public float orthoSizeMax = 12f;

    private float _currentYRotation;
    private float _orbitDistance;
    private float _orbitHeight;
    private Vector3 _orbitCenter;
    private bool _initialized;

    public void Init(Vector3 gridCenter, Vector3 cameraPosition)
    {
        _orbitCenter = gridCenter;
        Vector3 toCam = cameraPosition - gridCenter;
        toCam.y = 0;
        _orbitDistance = toCam.magnitude;
        _orbitHeight = cameraPosition.y - gridCenter.y;
        _currentYRotation = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized)
        {
            var gm = FindObjectOfType<GridGameManager>();
            if (gm != null)
            {
                Vector3 center = new Vector3(gm.gridWidth / 2f, 0f, gm.gridHeight / 2f);
                Init(center, transform.position);
            }
            else return;
        }

        if (orbitPivot != null) _orbitCenter = orbitPivot.position;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += GetForward();
        if (Input.GetKey(KeyCode.S)) move -= GetForward();
        if (Input.GetKey(KeyCode.D)) move += GetRight();
        if (Input.GetKey(KeyCode.A)) move -= GetRight();
        if (move.sqrMagnitude > 0.1f)
        {
            move = move.normalized * moveSpeed * Time.deltaTime;
            _orbitCenter += move;
            if (orbitPivot != null) orbitPivot.position = _orbitCenter;
        }

        if (Input.GetKey(KeyCode.E)) _currentYRotation += rotateSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) _currentYRotation -= rotateSpeed * Time.deltaTime;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
            {
                float newSize = cam.orthographicSize - scroll * scrollZoomSpeed;
                cam.orthographicSize = Mathf.Clamp(newSize, orthoSizeMin, orthoSizeMax);
            }
        }

        float rad = _currentYRotation * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad) * _orbitDistance, _orbitHeight, Mathf.Cos(rad) * _orbitDistance);
        transform.position = _orbitCenter + offset;
        transform.LookAt(_orbitCenter);
    }

    Vector3 GetForward()
    {
        Vector3 f = transform.forward;
        f.y = 0;
        return f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
    }

    Vector3 GetRight()
    {
        Vector3 r = transform.right;
        r.y = 0;
        return r.sqrMagnitude > 0.01f ? r.normalized : Vector3.right;
    }
}
