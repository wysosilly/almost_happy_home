using UnityEngine;
using System.Collections.Generic;

public enum FurnitureType { Normal, Storage, Wall }
public enum FurnitureCategory { None, Food, Clothes, Book, Toy }

public class Furniture : MonoBehaviour
{
    public Vector2Int gridPos;
    
    [Header("Wall Settings")]
    public float height = 1f; // 가구의 높이 (바닥 가구는 점유 높이, 벽 가구는 설치 높이)
    public float wallThickness = 0.05f; // 벽에 붙었을 때의 두께
    public Vector2 wallSize = new Vector2(1f, 1f); // 벽 가구의 그리드 점유 크기 (월드 1x1 단위)
    [HideInInspector] public bool isAttachedToWall = false;
    [HideInInspector] public Vector3 originalScale;
    [HideInInspector] public Vector2 wallGridPos; // 벽면 내의 0.5 단위 좌표
    
    [Header("Size / Shape")]
    [Tooltip("커스텀 모양 사용 (ㄱ자, ㄴ자, T자 등)")]
    public bool useCustomShape = false;
    
    [Tooltip("커스텀 모양: gridPos 기준 상대 좌표 리스트\n예) ㄱ자: (0,0), (1,0), (1,1), (1,2)")]
    public List<Vector2Int> customShapeCells = new List<Vector2Int>();
    
    [Tooltip("사각형 모양일 때 가구가 차지하는 그리드 칸 수 (가로 x 세로)")]
    public Vector2Int size = new Vector2Int(1, 1);
    
    [Tooltip("체크하면 shape에 맞게 비주얼 자동 생성")]
    public bool autoScaleVisual = true;
    
    [Tooltip("비주얼 높이 (Y축)")]
    public float visualHeight = 1f;
    
    [Tooltip("커스텀 모양 비주얼 색상")]
    public Color shapeColor = Color.white;

    [Header("3D Model (선택)")]
    [Tooltip("할당하면 큐브 대신 이 모델(FBX/프리팹) 형태로 표시. 비우면 그리드 큐브 또는 커스텀 셰이프 사용")]
    public GameObject modelPrefab;

    [Header("Name Label")]
    public string displayName;
    [Tooltip("가구 위쪽으로 이름 텍스트 높이 (로컬 Y)")]
    public float nameLabelHeight = 1.5f;
    
    Transform _nameLabelTransform;
    TextMesh _nameTextMesh;

    [Header("Pivot")]
    [Tooltip("체크=피벗이 셀 중심 (Unity Quad/Plane 기본). 해제=피벗이 좌하단 코너")]
    public bool pivotAtCenter = true;

    [Header("Rotation")]
    [Tooltip("0=0°, 1=90°, 2=180°, 3=270° (드래그 중 R키/우클릭으로 회전)")]
    public int rotationSteps = 0;

    [Header("Happy")]
    [Tooltip("턴 종료 시 이 가구가 제공하는 Happy 수치")]
    public int happyValue = 1;
    [Tooltip("병합 강화 단계. 병합할 때마다 증가하며 이름 뒤에 +N으로 표시")]
    public int mergeLevel = 0;

    [Header("Enhancements")]
    public int bonusActionPoints = 0;
    public int permanentBonusAP = 0; // 매 턴 추가될 영구 보너스 AP
    private List<IFurnitureEnhancement> _appliedEnhancements = new List<IFurnitureEnhancement>();

    public void AddEnhancement(IFurnitureEnhancement enhancement)
    {
        _appliedEnhancements.Add(enhancement);
        enhancement.Apply(this);
        UpdateNameLabel(); // 수치 변경 시 라벨 업데이트
    }

    [Header("Storage Settings")]
    public FurnitureType type = FurnitureType.Normal;
    public FurnitureCategory category; 
    public List<FurnitureCategory> acceptableCategories = new List<FurnitureCategory>();
    public int storageCapacity = 2;
    public List<Furniture> storedItems = new List<Furniture>();

    /// <summary>병합 시 같은 가구인지 판별하는 키. 같은 키면 드롭 시 병합 가능.</summary>
    public string GetMergeKey()
    {
        string baseName = string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
        return baseName.Replace("(Clone)", "").Trim();
    }

    private List<GameObject> _generatedVisuals = new List<GameObject>();
    
    void Start() {
        originalScale = transform.localScale;
        ApplyVisualScale();
        SetupNameLabel();
    }
    
    void SetupNameLabel() {
        string nameText = GetFormattedName();
        Vector3 labelLocalPos = GetNameLabelLocalPosition();
        var existingCanvas = GetComponentInChildren<Canvas>(true);
        if (existingCanvas != null) existingCanvas.gameObject.SetActive(false);
        GameObject labelObj = new GameObject("NameLabel3D");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = labelLocalPos;
        labelObj.transform.localRotation = Quaternion.identity;
        labelObj.transform.localScale = Vector3.one;
        _nameTextMesh = labelObj.AddComponent<TextMesh>();
        _nameTextMesh.text = nameText;
        _nameTextMesh.fontSize = 18; // 24 -> 18
        _nameTextMesh.anchor = TextAnchor.MiddleCenter;
        _nameTextMesh.alignment = TextAlignment.Center;
        _nameTextMesh.color = Color.white;
        _nameTextMesh.characterSize = 0.2f; // 0.3 -> 0.2
        _nameTextMesh.lineSpacing = 1f;
        _nameLabelTransform = labelObj.transform;
    }

    string GetFormattedName() {
        string baseName = string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
        if (mergeLevel > 0) baseName += " +" + mergeLevel;
        string info = $"(H:{happyValue}";
        if (permanentBonusAP > 0) info += $", AP:+{permanentBonusAP}";
        info += ")";
        return $"{baseName}\n<size=12>{info}</size>"; // 18 -> 12
    }

    public void UpdateNameLabel() {
        if (_nameTextMesh != null) {
            _nameTextMesh.text = GetFormattedName();
        }
    }
    
    Vector3 GetNameLabelLocalPosition() {
        if (useCustomShape && customShapeCells.Count > 0) {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var c in customShapeCells) {
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.y > maxY) maxY = c.y;
            }
            float centerX = (minX + maxX + 1f) * 0.5f;
            float centerZ = (minY + maxY + 1f) * 0.5f;
            return new Vector3(centerX, nameLabelHeight, centerZ);
        }
        if (pivotAtCenter) return new Vector3(0f, nameLabelHeight, 0f);
        var s = EffectiveSize;
        return new Vector3(s.x * 0.5f, nameLabelHeight, s.y * 0.5f);
    }
    
    public void ApplyVisualScale() {
        if (isAttachedToWall) return;
        // 3D Model(선택)에 할당한 가구만 모델 표시. 할당 안 한 가구는 Model 자식 무시/제거 후 큐브만 표시
        if (modelPrefab != null) {
            Transform modelRoot = transform.Find("Model");
            if (modelRoot == null) modelRoot = transform.Find("FurnitureModel");
            if (modelRoot == null) modelRoot = CreateModelFromPrefab();
            if (modelRoot != null) {
                transform.localScale = Vector3.one;
                var r = GetComponent<Renderer>();
                if (r != null) r.enabled = false;
                ApplyVisualRotation();
                return;
            }
        }
        // modelPrefab 없음 → 기존 Model 자식(선반/마젠타 등) 제거하고 큐브만 표시
        Transform oldModel = transform.Find("Model");
        if (oldModel == null) oldModel = transform.Find("FurnitureModel");
        if (oldModel != null) {
            if (Application.isPlaying) Destroy(oldModel.gameObject);
            else DestroyImmediate(oldModel.gameObject);
        }
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = true;
        if (!autoScaleVisual) return;
        var s = EffectiveSize;
        transform.localScale = new Vector3(s.x, visualHeight, s.y);
        ApplyVisualRotation();
    }
    
    /// <summary>modelPrefab을 인스턴스화해 Model 자식으로 넣고, 그리드 크기(size, visualHeight)에 맞게 스케일/위치 보정</summary>
    Transform CreateModelFromPrefab() {
        GameObject go = Instantiate(modelPrefab);
        go.name = "Model";
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        Bounds b = ComputeBounds(go);
        if (b.size.x < 0.001f) b.size = new Vector3(1f, 1f, 1f);
        Vector2Int s = EffectiveSize;
        if (s.x < 1) s.x = 1;
        if (s.y < 1) s.y = 1;
        float h = visualHeight > 0.01f ? visualHeight : 1f;
        go.transform.localPosition = -b.center;
        go.transform.localScale = new Vector3(s.x / b.size.x, h / b.size.y, s.y / b.size.z);
        return go.transform;
    }

    static Bounds ComputeBounds(GameObject root) {
        Bounds b = new Bounds(root.transform.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>()) {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return first ? new Bounds(root.transform.position, Vector3.one) : b;
    }

    void GenerateCustomShapeVisual() {
        foreach (var v in _generatedVisuals) {
            if (v != null) {
                if (Application.isPlaying) Destroy(v);
                else DestroyImmediate(v);
            }
        }
        _generatedVisuals.Clear();
        transform.localScale = Vector3.one;
        if (!useCustomShape || customShapeCells.Count == 0)
            ApplyVisualRotation();
        else
            transform.rotation = Quaternion.identity;
        var cellsToUse = (useCustomShape && customShapeCells.Count > 0) ? GetRotatedShapeCells() : customShapeCells;
        foreach (var cell in cellsToUse) {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"ShapeCell_{cell.x}_{cell.y}";
            cube.transform.SetParent(transform);
            cube.transform.localPosition = new Vector3(cell.x + 0.5f, visualHeight * 0.5f, cell.y + 0.5f);
            cube.transform.localScale = new Vector3(0.95f, visualHeight, 0.95f);
            var rend = cube.GetComponent<Renderer>();
            if (rend != null) {
                rend.material = new Material(Shader.Find("Standard"));
                rend.material.color = shapeColor;
            }
            _generatedVisuals.Add(cube);
        }
    }
    
#if UNITY_EDITOR
    void OnValidate() {
        if (autoScaleVisual) {
            if (useCustomShape && customShapeCells.Count > 0)
                transform.localScale = Vector3.one;
            else {
                var s = EffectiveSize;
                transform.localScale = new Vector3(s.x, visualHeight, s.y);
            }
        }
    }
    void OnDrawGizmosSelected() {
        if (useCustomShape && customShapeCells.Count > 0) {
            foreach (var cell in customShapeCells) {
                Vector3 cellCenter = new Vector3(gridPos.x + cell.x + 0.5f, 0.1f, gridPos.y + cell.y + 0.5f);
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(cellCenter, new Vector3(0.9f, 0.1f, 0.9f));
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(cellCenter, new Vector3(1f, 0.1f, 1f));
            }
        } else {
            var s = EffectiveSize;
            Vector3 center = new Vector3(gridPos.x + s.x * 0.5f, 0.1f, gridPos.y + s.y * 0.5f);
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(center, new Vector3(s.x, 0.1f, s.y));
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, new Vector3(s.x, 0.1f, s.y));
        }
    }
#endif

    void LateUpdate() {
        if (_nameLabelTransform == null) return;
        Vector3 parentScale = transform.lossyScale;
        float invX = Mathf.Abs(parentScale.x) > 0.001f ? 1f / parentScale.x : 1f;
        float invY = Mathf.Abs(parentScale.y) > 0.001f ? 1f / parentScale.y : 1f;
        float invZ = Mathf.Abs(parentScale.z) > 0.001f ? 1f / parentScale.z : 1f;

        // 벽에 붙었을 때: 라벨을 항상 읽기 가능한 고정 월드 스케일로 (텍스트 깨짐 방지)
        if (isAttachedToWall) {
            float worldScale = 0.2f;
            invX = worldScale / Mathf.Max(0.01f, Mathf.Abs(parentScale.x));
            invY = worldScale / Mathf.Max(0.01f, Mathf.Abs(parentScale.y));
            invZ = worldScale / Mathf.Max(0.01f, Mathf.Abs(parentScale.z));
        }
        _nameLabelTransform.localScale = new Vector3(invX, invY, invZ);
        if (Camera.main != null)
            _nameLabelTransform.LookAt(_nameLabelTransform.position + Camera.main.transform.forward);
    }

    public void Move(Vector2Int newPos) {
        gridPos = newPos;
        UpdateVisualPosition();
    }

    public void Rotate90() {
        rotationSteps = (rotationSteps + 1) % 4;
        ApplyVisualRotation();
    }

    void ApplyVisualRotation() {
        transform.rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
    }

    public Vector2Int EffectiveSize {
        get {
            if (useCustomShape && customShapeCells.Count > 0) {
                var cells = GetRotatedShapeCells();
                int minX = int.MaxValue, maxX = int.MinValue;
                int minY = int.MaxValue, maxY = int.MinValue;
                foreach (var c in cells) {
                    if (c.x < minX) minX = c.x;
                    if (c.x > maxX) maxX = c.x;
                    if (c.y < minY) minY = c.y;
                    if (c.y > maxY) maxY = c.y;
                }
                return new Vector2Int(maxX - minX + 1, maxY - minY + 1);
            }
            var s = new Vector2Int(size.x > 0 ? size.x : 1, size.y > 0 ? size.y : 1);
            if (rotationSteps == 1 || rotationSteps == 3) return new Vector2Int(s.y, s.x);
            return s;
        }
    }

    List<Vector2Int> GetRotatedShapeCells() {
        if (!useCustomShape || customShapeCells.Count == 0) return customShapeCells;
        var result = new List<Vector2Int>(customShapeCells);
        for (int step = 0; step < rotationSteps; step++) {
            var next = new List<Vector2Int>();
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in result) {
                var r = new Vector2Int(c.y, -c.x);
                next.Add(r);
                if (r.x < minX) minX = r.x;
                if (r.y < minY) minY = r.y;
            }
            for (int i = 0; i < next.Count; i++)
                next[i] = new Vector2Int(next[i].x - minX, next[i].y - minY);
            result = next;
        }
        return result;
    }
    
    public Vector2Int ShapeMinOffset {
        get {
            if (useCustomShape && customShapeCells.Count > 0) {
                int minX = int.MaxValue, minY = int.MaxValue;
                foreach (var c in customShapeCells) {
                    if (c.x < minX) minX = c.x;
                    if (c.y < minY) minY = c.y;
                }
                return new Vector2Int(minX, minY);
            }
            return Vector2Int.zero;
        }
    }

    /// <summary>바닥 그리드 셀 크기 (0.5 = 반쪽 셀). gridPos는 이 단위의 인덱스(반쪽 셀)입니다.</summary>
    public const float FloorCellSize = 0.5f;

    public Vector3 GetWorldPosition() {
        if (useCustomShape && customShapeCells.Count > 0) {
            var cells = GetRotatedShapeCells();
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var c in cells) {
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.y > maxY) maxY = c.y;
            }
            float cx = (minX + maxX + 1f) * 0.5f;
            float cz = (minY + maxY + 1f) * 0.5f;
            // 바닥에 붙이기: 루트 Y=0 → 자식 큐브 localY=visualHeight*0.5 이면 바닥=0
            return new Vector3(gridPos.x * FloorCellSize + cx, 0f, gridPos.y * FloorCellSize + cz);
        }
        var s = EffectiveSize;
        float x = pivotAtCenter ? (gridPos.x + s.x) * FloorCellSize : gridPos.x * FloorCellSize;
        float z = pivotAtCenter ? (gridPos.y + s.y) * FloorCellSize : gridPos.y * FloorCellSize;
        // 바닥에 붙이기: 중심 Y = visualHeight/2
        return new Vector3(x, visualHeight * 0.5f, z);
    }

    public void UpdateVisualPosition() {
        transform.position = GetWorldPosition();
    }

    public List<Vector2Int> GetOccupiedCells() => GetOccupiedCellsAt(gridPos);

    /// <summary>점유하는 반쪽 셀(0.5 단위) 좌표 목록. gridPos는 반쪽 셀 인덱스입니다.</summary>
    public List<Vector2Int> GetOccupiedCellsAt(Vector2Int pos) {
        List<Vector2Int> cells = new List<Vector2Int>();
        if (useCustomShape && customShapeCells.Count > 0) {
            foreach (var offset in GetRotatedShapeCells()) {
                int hx = pos.x + offset.x * 2, hy = pos.y + offset.y * 2;
                cells.Add(new Vector2Int(hx, hy));
                cells.Add(new Vector2Int(hx + 1, hy));
                cells.Add(new Vector2Int(hx, hy + 1));
                cells.Add(new Vector2Int(hx + 1, hy + 1));
            }
        } else {
            var s = EffectiveSize;
            int hw = s.x * 2, hh = s.y * 2;
            for (int x = 0; x < hw; x++)
                for (int y = 0; y < hh; y++)
                    cells.Add(new Vector2Int(pos.x + x, pos.y + y));
        }
        return cells;
    }
}
