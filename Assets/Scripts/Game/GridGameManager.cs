using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public partial class GridGameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public HashSet<Vector2Int> validCells = new HashSet<Vector2Int>();
    public int gridWidth = 10;
    public int gridHeight = 10;
    public GameObject floorPlane;
    public Color gridColor = Color.gray; 
    public float lineWidth = 0.05f;      

    [Header("Prefabs")]
    public List<GameObject> furniturePrefabs = new List<GameObject>();
    public GameObject indicatorPrefab;
    [Tooltip("인디케이터 프리팹 기본 크기 (Unity Plane=10, Cube/Quad=1)")]
    public float indicatorBaseSize = 10f;
    [Tooltip("가구 위에 있어도 인디케이터가 보이도록 항상 위에 렌더 (드래그 중만)")]
    public bool indicatorAlwaysOnTop = true;
    [Tooltip("비드래그 시 겹친 가구 위 오버레이 투명도")]
    [Range(0.2f, 0.9f)]
    public float furnitureOverlayOpacity = 0.6f;
    public TextMeshProUGUI apText;

    [Header("State")]
    public int actionPoints = 3;
    public List<UpcomingDelivery> upcomingDeliveries = new List<UpcomingDelivery>();
    public List<Furniture> allFurnitures = new List<Furniture>();

    [Header("Happy")]
    public int happyTotal = 0;
    [Tooltip("Storage에 맞는 카테고리 아이템을 넣었을 때 추가 Happy (예: 냉장고+음식 시너지)")]
    public int synergyBonusPerMatch = 2;

    [Header("Stages / Game Over")]
    public List<StageData> stageDataList = new List<StageData>();
    private int _currentRoundIndex;
    private int _currentStageIndex;
    private int _turnsInCurrentRound;

    [Header("Debug")]
    [SerializeField] bool debugStorage = false;
    
    private Furniture selectedFurniture;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Vector2Int originalGridPos;
    private List<GameObject> activeIndicators = new List<GameObject>();
    private List<GameObject> _furnitureOverlays = new List<GameObject>();
    private List<GameObject> _dropPreviewIndicators = new List<GameObject>();
    private List<GameObject> _wallPreviewIndicators = new List<GameObject>();
    private bool _gridInitialized;
    private FurnitureSelectionManager _selectionManager;
    private int _currentTurn;
    private bool _gameOver;
    private GameObject _gameOverUI;
    private GameObject _stageClearUI;
    private bool _waitingForNextStage;
    private FurnitureEnhancementUI _enhancementUI;
    private List<GameObject> _activeWalls = new List<GameObject>();
    private List<(Vector2Int cell, int side)> _activeWallCells = new List<(Vector2Int, int)>();
    private List<GameObject> _wallGridLines = new List<GameObject>();
    private List<GameObject> _wallOccupancyOverlays = new List<GameObject>();
    private List<GameObject> _wallFurnitureLabels = new List<GameObject>();
    // 벽 가이드를 그리드와 정확히 맞추기 위해 마지막 벽 히트 정보 저장 (벽 회전 사용)
    private Vector3 _lastWallHitPoint;
    private Vector3 _lastWallHitNormal;
    private Quaternion _lastWallHitRotation;
    private List<GameObject> _expansionButtons = new List<GameObject>();
    private HashSet<Vector2Int> _expansionCandidates = new HashSet<Vector2Int>();
    private bool _isSelectingExpansion = false;
    private System.Action _onExpansionComplete;
    
    // 가구 배치 위치 선택 모드
    private bool _isSelectingDropPosition = false;
    private GameObject _pendingDeliveryPrefab;
    private int _pendingRotationSteps = 0;
    private GameObject _dropPreviewIndicator;
    private Vector2Int _lastPreviewGridPos = new Vector2Int(-999, -999);

    [Header("Flow")]
    [Tooltip("비우면 FurnitureSelectionManager가 먼저 표시됨")]
    public bool skipSelection;
    [Tooltip("있으면 게임 오버 Retry 시 게임 시작 UI로 이동")]
    public GameStartUI gameStartUI;

    void Awake() {
        // 해상도 고정 (1920x1080, 전체화면 여부는 에디터 설정에 따름)
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        
        if (!skipSelection && gameStartUI == null) {
            var go = new GameObject("GameStartUI");
            gameStartUI = go.AddComponent<GameStartUI>();
        }
    }

    void Start() {
        SetupHUD(); // HUD 위치 조정 추가
        if (skipSelection) BeginGame(null);
    }

    void SetupHUD() {
        if (apText == null) return;

        // 1. Canvas 설정
        Canvas canvas = apText.canvas;
        if (canvas != null) {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0f;
            if (canvas.GetComponent<CanvasFitInScreen>() == null)
                canvas.gameObject.AddComponent<CanvasFitInScreen>();
        }

        // 2. apText 위치 조정 (좌측 상단 고정)
        RectTransform textRect = apText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(0, 1);
        textRect.pivot = new Vector2(0, 1);
        textRect.anchoredPosition = new Vector2(20, -20); // 여백 20

        // 3. "END TURN" 버튼 찾아서 위치 조정 (좌측 하단 고정)
        Button endTurnBtn = canvas.GetComponentInChildren<Button>(true);
        if (endTurnBtn != null) {
            RectTransform btnRect = endTurnBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0);
            btnRect.anchorMax = new Vector2(0, 0);
            btnRect.pivot = new Vector2(0, 0);
            btnRect.anchoredPosition = new Vector2(20, 20); // 여백 20
        }
    }

    public void BeginGame(GameObject selectedFurniturePrefab) {
        if (!_gridInitialized) {
            _currentStageIndex = 0;
            _currentRoundIndex = 0;
            _turnsInCurrentRound = 0;
            _currentTurn = 0;
            happyTotal = 0; // 행복도 초기화 추가
            _gameOver = false;
            
            LoadStage(_currentStageIndex);
            _gridInitialized = true;
        }
        if (_gameOver) return;
        _waitingForNextStage = false; // 플로우가 꼬이지 않도록 초기화
        if (_selectionManager == null) _selectionManager = FindObjectOfType<FurnitureSelectionManager>();
        AddSelectedDelivery(selectedFurniturePrefab);
        UpdateIndicators();
    }

    void LoadStage(int stageIndex) {
        StageData data;
        
        // 리스트에 미리 정의된 데이터가 있으면 사용하고, 없으면 동적으로 생성합니다.
        if (stageIndex < stageDataList.Count) {
            data = StageGenerator.GenerateStage(stageIndex, stageDataList[stageIndex]);
        } else {
            data = StageGenerator.GenerateStage(stageIndex);
            Debug.Log($"[StageGenerator] 동적 스테이지 생성: {data.stageName} ({data.gridWidth}x{data.gridHeight})");
        }

        gridWidth = data.gridWidth;
        gridHeight = data.gridHeight;

        // 초기 그리드 셀 설정: 0.5 단위(반쪽 셀)로 저장
        validCells.Clear();
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                validCells.Add(new Vector2Int(x * 2, y * 2));
                validCells.Add(new Vector2Int(x * 2 + 1, y * 2));
                validCells.Add(new Vector2Int(x * 2, y * 2 + 1));
                validCells.Add(new Vector2Int(x * 2 + 1, y * 2 + 1));
            }
        }

        // 기존 그리드 비주얼 및 가구 정리
        ClearCurrentStage();

        SetupScene();
        CreateVisualGrid();
        UpdateWalls();

        // 초기 가구 배치 (스테이지 gridPos는 1단위 → 반쪽 셀 인덱스로 변환)
        foreach (var info in data.initialFurnitures) {
            if (info.prefab == null) continue;
            GameObject go = Instantiate(info.prefab);
            Furniture f = go.GetComponent<Furniture>();
            f.gridPos = new Vector2Int(info.gridPos.x * 2, info.gridPos.y * 2);
            f.rotationSteps = info.rotationSteps;
            f.ApplyVisualScale();
            f.UpdateVisualPosition();
            allFurnitures.Add(f);
        }

        _currentRoundIndex = 0;
        _turnsInCurrentRound = 0;
        _waitingForNextStage = false; // 스테이지 로드 시 플로우 잠금 해제
    }

    void ClearCurrentStage() {
        // 가구 제거
        foreach (var f in allFurnitures) {
            if (f != null) Destroy(f.gameObject);
        }
        allFurnitures.Clear();

        // 그리드 비주얼 제거
        GameObject oldGrid = GameObject.Find("VisualGrid");
        if (oldGrid != null) Destroy(oldGrid);

        upcomingDeliveries.Clear();
    }

    void AddSelectedDelivery(GameObject prefab) {
        if (prefab == null) return;
        var f = prefab.GetComponent<Furniture>();
        if (f == null) return;
        
        // 위치 선택 모드 시작
        _pendingDeliveryPrefab = prefab;
        StartDropPositionSelection(f);
    }
    
    void StartDropPositionSelection(Furniture furnitureRef) {
        _isSelectingDropPosition = true;
        _pendingRotationSteps = 0;
        _lastPreviewGridPos = new Vector2Int(-999, -999);
        ClearDropPreviewIndicator();
        CreateDropPreviewIndicator(furnitureRef);
    }
    
    void ClearDropPreviewIndicator() {
        if (_dropPreviewIndicator != null) {
            Destroy(_dropPreviewIndicator);
            _dropPreviewIndicator = null;
        }
    }
    
    void CreateDropPreviewIndicator(Furniture furnitureRef) {
        if (indicatorPrefab == null) return;
        
        _dropPreviewIndicator = Instantiate(indicatorPrefab);
        _dropPreviewIndicator.name = "DropPreviewIndicator";
        
        // 콜라이더 비활성화
        var allColliders = _dropPreviewIndicator.GetComponentsInChildren<Collider>(true);
        foreach (var c in allColliders) if (c != null) c.enabled = false;
        
        // 초기 위치는 화면 밖에
        _dropPreviewIndicator.transform.position = new Vector3(-1000, 0, -1000);
    }
    
    void HandleDropPositionSelection() {
        if (!_isSelectingDropPosition || _pendingDeliveryPrefab == null) return;
        
        var furnitureRef = _pendingDeliveryPrefab.GetComponent<Furniture>();
        if (furnitureRef == null) return;
        
        // R키 또는 우클릭으로 회전
        if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1)) {
            _pendingRotationSteps = (_pendingRotationSteps + 1) % 4;
            _lastPreviewGridPos = new Vector2Int(-999, -999); // 강제 업데이트
        }
        
        // 마우스 위치에 따라 프리뷰 업데이트
        UpdateDropPreviewPosition(furnitureRef);
        
        if (Input.GetMouseButtonDown(0)) {
            Vector3 mPos = GetMouseWorldPos();
            // 0.5 단위 스냅
            int halfX = Mathf.FloorToInt(mPos.x * 2f);
            int halfY = Mathf.FloorToInt(mPos.z * 2f);
            
            var sz = GetRotatedSize(furnitureRef, _pendingRotationSteps);
            Vector2Int halfCellPos = new Vector2Int(halfX, halfY);
            
            // 유효한 위치인지 확인
            if (!IsDropPositionValidWithRotation(halfCellPos, sz)) return;
            
            // 즉시 가구 배치 (halfCellPos 전달)
            PlaceFurnitureImmediately(halfX, halfY, _pendingRotationSteps);
            
            // 위치 선택 모드 종료
            _isSelectingDropPosition = false;
            _pendingDeliveryPrefab = null;
            _pendingRotationSteps = 0;
            ClearDropPreviewIndicator();
            UpdateIndicators();
        }
    }
    
    void UpdateDropPreviewPosition(Furniture furnitureRef) {
        if (_dropPreviewIndicator == null) return;
        
        Vector3 mPos = GetMouseWorldPos();
        // 0.5 단위 스냅
        int halfX = Mathf.FloorToInt(mPos.x * 2f);
        int halfY = Mathf.FloorToInt(mPos.z * 2f);
        
        Vector2Int currentGridPos = new Vector2Int(halfX, halfY);
        
        // 같은 그리드 위치면 업데이트 스킵 (회전 시에는 강제 업데이트됨)
        if (currentGridPos == _lastPreviewGridPos) return;
        _lastPreviewGridPos = currentGridPos;
        
        var sz = GetRotatedSize(furnitureRef, _pendingRotationSteps);
        Vector2Int halfCellPos = new Vector2Int(halfX, halfY);
        
        // 위치 및 크기 설정 (0.5 단위 기준)
        float scaleFactor = 1f / Mathf.Max(indicatorBaseSize, 0.01f);
        float worldX = halfX * 0.5f;
        float worldZ = halfY * 0.5f;
        float centerX = worldX + sz.x * 0.5f;
        float centerZ = worldZ + sz.y * 0.5f;
        
        _dropPreviewIndicator.transform.position = new Vector3(centerX, 0.03f, centerZ);
        _dropPreviewIndicator.transform.localScale = new Vector3(scaleFactor * sz.x, 1f, scaleFactor * sz.y);
        
        // 유효한 위치인지에 따라 색상 변경
        bool isValid = IsDropPositionValidWithRotation(halfCellPos, sz);
        var rend = _dropPreviewIndicator.GetComponentInChildren<Renderer>();
        if (rend != null) {
            if (rend.material == null) {
                rend.material = new Material(Shader.Find("Sprites/Default"));
            }
            rend.material.color = isValid 
                ? new Color(0.2f, 0.8f, 0.2f, 0.5f)  // 녹색 (배치 가능)
                : new Color(0.8f, 0.2f, 0.2f, 0.5f); // 빨간색 (배치 불가)
        }
    }
    
    Vector2Int GetRotatedSize(Furniture f, int rotSteps) {
        var baseSize = f.size;
        if (f.useCustomShape && f.customShapeCells.Count > 0) {
            // 커스텀 셰이프의 경우 회전된 바운딩 박스 계산
            var cells = GetRotatedShapeCellsStatic(f.customShapeCells, rotSteps);
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
        var s = new Vector2Int(baseSize.x > 0 ? baseSize.x : 1, baseSize.y > 0 ? baseSize.y : 1);
        if (rotSteps == 1 || rotSteps == 3) return new Vector2Int(s.y, s.x);
        return s;
    }
    
    static List<Vector2Int> GetRotatedShapeCellsStatic(List<Vector2Int> originalCells, int rotSteps) {
        if (originalCells == null || originalCells.Count == 0) return originalCells;
        var result = new List<Vector2Int>(originalCells);
        for (int step = 0; step < rotSteps; step++) {
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
    
    
    bool IsDropPositionValidWithRotation(Vector2Int halfCellPos, Vector2Int size) {
        int gx = halfCellPos.x / 2;
        int gy = halfCellPos.y / 2;
        
        if (gx + size.x > gridWidth || gy + size.y > gridHeight) return false;
        if (gx < 0 || gy < 0) return false;
        
        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                int hx = halfCellPos.x + x * 2;
                int hy = halfCellPos.y + y * 2;
                
                if (!validCells.Contains(new Vector2Int(hx, hy))) return false;
                if (!validCells.Contains(new Vector2Int(hx + 1, hy))) return false;
                if (!validCells.Contains(new Vector2Int(hx, hy + 1))) return false;
                if (!validCells.Contains(new Vector2Int(hx + 1, hy + 1))) return false;
            }
        }
        
        for (int x = 0; x < size.x * 2; x++) {
            for (int y = 0; y < size.y * 2; y++) {
                Vector2Int cell = new Vector2Int(halfCellPos.x + x, halfCellPos.y + y);
                foreach (var f in allFurnitures) {
                    if (f == null || !f.gameObject.activeInHierarchy) continue;
                    if (f.GetOccupiedCells().Contains(cell)) return false;
                }
            }
        }
        
        return true;
    }
    
    void PlaceFurnitureImmediately(int halfX, int halfY, int rotSteps) {
        if (_pendingDeliveryPrefab == null) return;
        
        GameObject go = Instantiate(_pendingDeliveryPrefab);
        Furniture f = go.GetComponent<Furniture>();
        f.gridPos = new Vector2Int(halfX, halfY);
        f.rotationSteps = rotSteps;
        f.ApplyVisualScale();
        allFurnitures.Add(f);
        
        Vector3 target = f.GetWorldPosition();
        go.transform.position = target + Vector3.up * 10f;
        StartCoroutine(FallRoutine(go.transform, target));
    }

    void RegisterSceneFurniture() {
        var sceneFurniture = FindObjectsOfType<Furniture>();
        foreach (var f in sceneFurniture) {
            if (allFurnitures.Contains(f)) continue;
            if (f.transform.parent != null && f.transform.parent.GetComponent<Furniture>() != null)
                continue;
            var fs = f.EffectiveSize;
            float cx = f.transform.position.x, cz = f.transform.position.z;
            int hx = Mathf.RoundToInt(cx * 2f), hz = Mathf.RoundToInt(cz * 2f);
            if (f.pivotAtCenter)
                f.gridPos = new Vector2Int(hx - fs.x, hz - fs.y);
            else
                f.gridPos = new Vector2Int(hx, hz);
            allFurnitures.Add(f);
        }
    }

    void Update() {
        if (_gameOver || _waitingForNextStage) return;
        
        // HUD 업데이트 (항상 실행)
        UpdateHUD();
        
        // 가구 배치 위치 선택 모드 처리
        if (_isSelectingDropPosition) {
            HandleDropPositionSelection();
            return;
        }
        
        HandleMouseInput();
        UpdateDropPreview();
        if (Camera.main != null) {
            foreach (var go in _wallFurnitureLabels)
                if (go != null) go.transform.LookAt(go.transform.position + Camera.main.transform.forward);
        }
        if (Input.GetKeyDown(KeyCode.Space)) EndTurn();
    }
    
    void UpdateHUD() {
        if (apText == null) return;
        
        apText.fontSize = 24;
        StageData currentStage = (_currentStageIndex < stageDataList.Count) ? stageDataList[_currentStageIndex] : null;
        string stageName = currentStage != null ? currentStage.stageName : $"Room {_currentStageIndex + 1}";
        int totalRounds = currentStage != null ? currentStage.rounds.Count : 3;
        
        string modeText = "";
        if (_isSelectingDropPosition) {
            modeText = "\n<color=#00FF00>Click to place furniture (R: Rotate)</color>";
        }
        
        apText.text = $"Action Points: {actionPoints}  Happy: {happyTotal}\n" +
                      $"Stage: {stageName} ({_currentStageIndex + 1})  Round: {_currentRoundIndex + 1}/{totalRounds}  Turn: {_currentTurn}" +
                      modeText;
    }

    void SetupScene() {
        if (floorPlane != null) {
            floorPlane.transform.position = new Vector3(gridWidth / 2f, -0.01f, gridHeight / 2f);
            floorPlane.transform.localScale = new Vector3(gridWidth / 10f, 1, gridHeight / 10f);
        }
        Vector3 gridCenter = new Vector3(gridWidth / 2f, 0f, gridHeight / 2f);
        float distance = Mathf.Max(gridWidth, gridHeight) * 1.5f;
        float d = distance * 0.85f;
        Vector3 camPos = gridCenter + new Vector3(d, distance * 0.85f, d);
        Camera.main.transform.position = camPos;
        Camera.main.transform.LookAt(gridCenter);
        Camera.main.orthographic = true;
        // Full HD(1920x1080) 기준: 그리드 전체가 화면에 잡히도록 (이소메트릭 뷰에서 여유 패딩)
        float gridMax = Mathf.Max(gridWidth, gridHeight);
        Camera.main.orthographicSize = gridMax / 2f + 1.5f;
        var camCtrl = Camera.main.GetComponent<IsometricCameraController>();
        if (camCtrl == null) camCtrl = Camera.main.gameObject.AddComponent<IsometricCameraController>();
        camCtrl.Init(gridCenter, camPos);
    }

    const float FloorGridStep = 0.5f;

    void CreateVisualGrid() {
        GameObject gridParent = new GameObject("VisualGrid");
        if (validCells.Count == 0) return;

        float y = 0.01f;
        // 반쪽 셀 기준: (hx, hz) → 월드 x = hx*0.5 ~ (hx+1)*0.5, z = hz*0.5 ~ (hz+1)*0.5
        // 세로선( x 고정 ): x 좌표별로, 그 x를 쓰는 셀들의 z 범위만 연속 구간으로 그림
        var xCoords = new HashSet<float>();
        foreach (var c in validCells) {
            xCoords.Add(c.x * FloorGridStep);
            xCoords.Add((c.x + 1) * FloorGridStep);
        }
        foreach (float x in xCoords) {
            int hxL = Mathf.FloorToInt(x / FloorGridStep);
            int hxR = hxL - 1;
            var hzSet = new HashSet<int>();
            foreach (var c in validCells) {
                if (c.x == hxL || c.x == hxR) hzSet.Add(c.y);
            }
            foreach (var (z0, z1) in GetContiguousRanges(hzSet))
                CreateLine(new Vector3(x, y, z0 * FloorGridStep), new Vector3(x, y, (z1 + 1) * FloorGridStep), gridParent.transform);
        }
        // 가로선( z 고정 ): z 좌표별로, 그 z를 쓰는 셀들의 x 범위만 연속 구간으로 그림
        var zCoords = new HashSet<float>();
        foreach (var c in validCells) {
            zCoords.Add(c.y * FloorGridStep);
            zCoords.Add((c.y + 1) * FloorGridStep);
        }
        foreach (float z in zCoords) {
            int hzL = Mathf.FloorToInt(z / FloorGridStep);
            int hzR = hzL - 1;
            var hxSet = new HashSet<int>();
            foreach (var c in validCells) {
                if (c.y == hzL || c.y == hzR) hxSet.Add(c.x);
            }
            foreach (var (x0, x1) in GetContiguousRanges(hxSet))
                CreateLine(new Vector3(x0 * FloorGridStep, y, z), new Vector3((x1 + 1) * FloorGridStep, y, z), gridParent.transform);
        }
    }

    static IEnumerable<(int min, int max)> GetContiguousRanges(HashSet<int> indices) {
        if (indices.Count == 0) yield break;
        var list = new List<int>(indices);
        list.Sort();
        int start = list[0], end = list[0];
        for (int i = 1; i < list.Count; i++) {
            if (list[i] == end + 1) end = list[i];
            else {
                yield return (start, end);
                start = end = list[i];
            }
        }
        yield return (start, end);
    }

    void CreateLine(Vector3 start, Vector3 end, Transform parent) {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(parent);
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = gridColor;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    void HandleMouseInput() {
        // 확장 선택 모드일 때는 마우스 위치를 그리드 좌표로 변환하여 처리
        if (_isSelectingExpansion) {
            if (Input.GetMouseButtonDown(0)) {
                // 바닥(y=0) 위 마우스 위치 → 1x1 월드 셀 하나만 계산 (0.5 단위 그리드 기준으로 스냅)
                Vector3 mPos = GetMouseWorldPos();
                int wx = Mathf.FloorToInt(mPos.x);
                int wy = Mathf.FloorToInt(mPos.z);
                Vector2Int clickedCell = new Vector2Int(wx, wy);
                if (!_expansionCandidates.Contains(clickedCell)) {
                    return;
                }
                _isSelectingExpansion = false;
                ClearExpansionButtons();
                AddGridCellWorld(clickedCell); // 반쪽 셀 4개(0.5×2, 0.5×2) = 1x1 한 칸만 추가
                _onExpansionComplete?.Invoke();
            }
            return;
        }

        if (actionPoints <= 0) return;
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // 모든 레이어에 대해 레이캐스트 (기본은 가구 선택)
            if (Physics.Raycast(ray, out RaycastHit hit, 100f)) {
                Furniture f = hit.collider.GetComponentInParent<Furniture>();
                if (f != null) {
                    selectedFurniture = f;
                    isDragging = true;
                    originalGridPos = f.gridPos;
                    dragOffset = f.transform.position - GetMouseWorldPos();
                    UpdateIndicators();
                }
            }
        }
        if (isDragging && selectedFurniture != null) {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1))
                selectedFurniture.Rotate90();

            // 벽 레이캐스트 체크 (모든 레이어 대상, 태그로 판별)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool hitWall = false;
            
            // RaycastAll을 사용하여 가구 뒤에 있는 벽도 감지할 수 있도록 함
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            foreach (var h in hits) {
                if (h.collider.transform.IsChildOf(selectedFurniture.transform)) continue;
                if (h.collider.CompareTag("Wall") || h.collider.gameObject.name == "Wall") {
                    // Type이 Wall일 때만 벽에 붙음 (WALL 태그 가구가 아님)
                    if (selectedFurniture.type == FurnitureType.Wall) {
                        HandleWallDrag(selectedFurniture, h);
                        hitWall = true;
                    }
                    break;
                }
            }

            if (!hitWall) {
                // 바닥 드래그 모드로 복구
                if (selectedFurniture.isAttachedToWall) {
                    selectedFurniture.isAttachedToWall = false;
                    selectedFurniture.transform.localScale = selectedFurniture.originalScale;
                }
                Vector3 mPos = GetMouseWorldPos() + dragOffset;
                selectedFurniture.transform.position = new Vector3(mPos.x, 1.2f, mPos.z);
                selectedFurniture.transform.rotation = Quaternion.Euler(0f, selectedFurniture.rotationSteps * 90f, 0f);
            }
        }
        if (Input.GetMouseButtonUp(0) && isDragging) {
            isDragging = false;
            var item = selectedFurniture;
            selectedFurniture = null;

            if (item.isAttachedToWall) {
                // 벽 가구 배치 확정 시 바닥 가구 충돌 체크
                if (IsWallPlacementValid(item)) {
                    actionPoints--;
                } else {
                    // 배치 실패 시 바닥으로 복구
                    item.isAttachedToWall = false;
                    item.transform.localScale = item.originalScale;
                    StartCoroutine(ShakeRoutine(item, originalGridPos));
                }
                UpdateWallOccupancyColors();
                UpdateIndicators();
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits) {
                Furniture storage = hit.collider.GetComponentInParent<Furniture>();
                if (storage == null || storage == item) continue;
                bool categoryOk = storage.acceptableCategories.Count == 0 || storage.acceptableCategories.Contains(item.category);
                if (debugStorage) Debug.Log($"[Drop] Hit: {hit.collider.name}, Type: {storage.type}, CatOK: {categoryOk}, Cap: {storage.storedItems.Count}/{storage.storageCapacity}");
                if (storage.type == FurnitureType.Storage && categoryOk && storage.storedItems.Count < storage.storageCapacity) {
                    if (debugStorage) Debug.Log($"[Store] {item.name} → {storage.name}");
                    StoreFurniture(item, storage);
                    actionPoints--;
                    UpdateIndicators();
                    return;
                }
                // 같은 가구끼리, 같은 강화 단계끼리만 병합 (+0은 +0끼리, +1은 +1끼리만)
                if (item.GetMergeKey() == storage.GetMergeKey() && item.mergeLevel == storage.mergeLevel) {
                    MergeFurniture(item, storage);
                    actionPoints--;
                    UpdateIndicators();
                    return;
                }
            }

            Vector3 dropPos = GetMouseWorldPos() + dragOffset;
            var itemSize = item.EffectiveSize;
            int snapX = Mathf.RoundToInt(dropPos.x * 2f), snapZ = Mathf.RoundToInt(dropPos.z * 2f);
            Vector2Int targetPos = item.pivotAtCenter
                ? new Vector2Int(snapX - itemSize.x, snapZ - itemSize.y)
                : new Vector2Int(snapX, snapZ);
            TryMove(item, targetPos);
            UpdateIndicators();
        }
    }

    void TryMove(Furniture f, Vector2Int target) {
        Furniture storage = GetStorageAtArea(target, f.EffectiveSize, f);
        bool moveFailed = true;

        if (storage != null) {
            StoreFurniture(f, storage);
            actionPoints--;
            moveFailed = false;
        } else if (IsPlacementValid(f, target)) { // IsPlacementValid를 사용하도록 수정
            f.Move(target);
            actionPoints--;
            moveFailed = false;
        }

        if (moveFailed) StartCoroutine(ShakeRoutine(f, originalGridPos));
    }

    /// <summary>같은 가구를 드롭했을 때: source 제거, target 위치에 남기고 target의 Happy x2, 강화 단계 +1.</summary>
    void MergeFurniture(Furniture source, Furniture target)
    {
        allFurnitures.Remove(source);
        target.mergeLevel += 1;
        Destroy(source.gameObject);
        target.happyValue *= 2;
        target.UpdateNameLabel();
    }

    void ProcessDelivery(UpcomingDelivery d) {
        HashSet<Vector2Int> deliveryCells = new HashSet<Vector2Int>();
        void AddHalfCells(int gx, int gy) {
            int hx = gx * 2, hy = gy * 2;
            deliveryCells.Add(new Vector2Int(hx, hy));
            deliveryCells.Add(new Vector2Int(hx + 1, hy));
            deliveryCells.Add(new Vector2Int(hx, hy + 1));
            deliveryCells.Add(new Vector2Int(hx + 1, hy + 1));
        }
        if (d.shapeCells != null && d.shapeCells.Count > 0) {
            foreach (var cell in d.shapeCells)
                AddHalfCells(d.gridPos.x + cell.x, d.gridPos.y + cell.y);
        } else {
            for (int x = 0; x < d.size.x; x++)
                for (int y = 0; y < d.size.y; y++)
                    AddHalfCells(d.gridPos.x + x, d.gridPos.y + y);
        }
        
        for (int i = allFurnitures.Count - 1; i >= 0; i--) {
            foreach (var cell in allFurnitures[i].GetOccupiedCells()) {
                if (deliveryCells.Contains(cell)) { 
                    Destroy(allFurnitures[i].gameObject); 
                    allFurnitures.RemoveAt(i); 
                    break; 
                }
            }
        }
        
        GameObject prefab = d.prefab != null ? d.prefab : (furniturePrefabs.Count > 0 ? furniturePrefabs[Random.Range(0, furniturePrefabs.Count)] : null);
        if (prefab != null) {
            GameObject go = Instantiate(prefab);
            Furniture f = go.GetComponent<Furniture>();
            f.gridPos = new Vector2Int(d.gridPos.x * 2, d.gridPos.y * 2);
            f.ApplyVisualScale();
            allFurnitures.Add(f);
            Vector3 target = f.GetWorldPosition();
            go.transform.position = target + Vector3.up * 10f;
            StartCoroutine(FallRoutine(go.transform, target));
        }
    }

    public void EndTurn() {
        if (_gameOver || _waitingForNextStage) return;
        StartCoroutine(EndTurnWithPopupDelay());
    }

    const float HappyPopupDuration = 1.2f;
    const float DelayAfterPopupBeforeUI = 1f;

    System.Collections.IEnumerator EndTurnWithPopupDelay() {
        var breakdown = GetPlacedFurnitureHappyBreakdown();
        int sum = 0;
        foreach (var (f, amount) in breakdown) {
            sum += amount;
            if (amount > 0) ShowHappyPopup(f, amount);
        }
        happyTotal += sum;
        yield return new WaitForSeconds(HappyPopupDuration + DelayAfterPopupBeforeUI);
        for (int i = upcomingDeliveries.Count - 1; i >= 0; i--) {
            upcomingDeliveries[i].turnsLeft--;
            if (upcomingDeliveries[i].turnsLeft <= 0) { ProcessDelivery(upcomingDeliveries[i]); upcomingDeliveries.RemoveAt(i); }
        }
        _currentTurn++;
        _turnsInCurrentRound++;
        StartFurnitureEnhancementPhase();
    }

    public void StartExpansionSelection(System.Action onComplete) {
        _isSelectingExpansion = true;
        _onExpansionComplete = onComplete;
        _expansionCandidates.Clear();
        
        // 확장 후보: 비어 있는 1x1 월드 셀 중, 이미 채워진 월드 셀과 인접한 것
        HashSet<Vector2Int> occupiedWorldCells = new HashSet<Vector2Int>();
        foreach (var h in validCells) {
            int wx = Mathf.FloorToInt(h.x / 2f), wy = Mathf.FloorToInt(h.y / 2f);
            occupiedWorldCells.Add(new Vector2Int(wx, wy));
        }
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var w in occupiedWorldCells) {
            foreach (var dir in dirs) {
                Vector2Int next = w + dir;
                if (!occupiedWorldCells.Contains(next)) _expansionCandidates.Add(next);
            }
        }

        // 후보지에 시각적 표시 (1x1 월드 셀 중심) + 클릭 시 해당 칸만 추가
        float scaleFactor = 1f / Mathf.Max(indicatorBaseSize, 0.01f);
        foreach (var cand in _expansionCandidates) {
            GameObject visual = Instantiate(indicatorPrefab);
            visual.transform.position = new Vector3(cand.x + 0.5f, 0.02f, cand.y + 0.5f);
            visual.transform.localScale = new Vector3(scaleFactor, 1f, scaleFactor);
            visual.name = "ExpansionMarker_" + cand.x + "_" + cand.y;

            var rend = visual.GetComponentInChildren<Renderer>();
            if (rend != null) {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = new Color(0f, 1f, 0f, 0.4f);
            }

            // 프리팹의 큰 콜라이더는 비활성화 후 1x1 BoxCollider만 사용 (즉시 적용으로 줄 전체 클릭 방지)
            var allColliders = visual.GetComponentsInChildren<Collider>(true);
            foreach (var c in allColliders) if (c != null) c.enabled = false;

            var btn = visual.GetComponent<ExpansionButton>();
            if (btn == null) btn = visual.AddComponent<ExpansionButton>();
            btn.cellPos = cand;
            btn.onClicked = null;
            btn.skipAddColliderInStart = true;

            var box = visual.AddComponent<BoxCollider>();
            float localSize = 1f / Mathf.Max(scaleFactor, 0.01f);
            box.size = new Vector3(localSize, 0.1f, localSize);
            box.center = Vector3.zero;
            box.isTrigger = true;

            _expansionButtons.Add(visual);
        }
    }

    void AddGridCell(Vector2Int pos) {
        validCells.Add(pos);
        _isSelectingExpansion = false;
        GameObject oldGrid = GameObject.Find("VisualGrid");
        if (oldGrid != null) Destroy(oldGrid);
        CreateVisualGrid();
        UpdateWalls();
        Debug.Log($"[GridGameManager] 셀 추가 완료: {pos}");
    }

    /// <summary>클릭한 1x1 월드 셀 한 칸만 추가. 반쪽 셀(0.5,0.5) 4개 = 1x1 그리드 한 칸. 그 외 추가 없음.</summary>
    void AddGridCellWorld(Vector2Int worldCell) {
        int wx = worldCell.x, wy = worldCell.y;
        Vector2Int h00 = new Vector2Int(wx * 2, wy * 2), h10 = new Vector2Int(wx * 2 + 1, wy * 2);
        Vector2Int h01 = new Vector2Int(wx * 2, wy * 2 + 1), h11 = new Vector2Int(wx * 2 + 1, wy * 2 + 1);
        if (validCells.Contains(h00) && validCells.Contains(h10) && validCells.Contains(h01) && validCells.Contains(h11))
            return;
        validCells.Add(h00);
        validCells.Add(h10);
        validCells.Add(h01);
        validCells.Add(h11);
        GameObject oldGrid = GameObject.Find("VisualGrid");
        if (oldGrid != null) Destroy(oldGrid);
        CreateVisualGrid();
        UpdateWalls();
    }

    void UpdateWalls() {
        foreach (var w in _activeWalls) if (w != null) Destroy(w);
        _activeWalls.Clear();
        _activeWallCells.Clear();
        foreach (var line in _wallGridLines) if (line != null) Destroy(line);
        _wallGridLines.Clear();
        foreach (var o in _wallOccupancyOverlays) if (o != null) Destroy(o);
        _wallOccupancyOverlays.Clear();

        if (validCells.Count == 0) return;

        GameObject wallParent = GameObject.Find("Walls");
        if (wallParent == null) wallParent = new GameObject("Walls");

        // 1. UL·UR(먼 쪽)만 벽, DR·DL(가까운 쪽)은 벽 표현 안 함
        var candidates = new HashSet<(Vector2Int cell, int side)>();
        foreach (var cell in validCells) {
            if (!validCells.Contains(cell + Vector2Int.left)) candidates.Add((cell, 0));   // UL
            if (!validCells.Contains(cell + Vector2Int.down)) candidates.Add((cell, 1));  // UR
            // DL(2)·DR(3) 쪽 벽은 생성하지 않음
        }

        // 2. 연결 요소별로 수집 — UL·UR(먼 그리드) 전부 포함하는 연결 요소를 사용 (공간 확장해도 먼 쪽 벽 이어짐)
        var components = new List<HashSet<(Vector2Int cell, int side)>>();
        var assigned = new HashSet<(Vector2Int cell, int side)>();
        var queue = new Queue<(Vector2Int cell, int side)>();

        foreach (var start in candidates) {
            if (assigned.Contains(start)) continue;
            var comp = new HashSet<(Vector2Int cell, int side)>();
            queue.Enqueue(start);
            while (queue.Count > 0) {
                var curr = queue.Dequeue();
                if (assigned.Contains(curr)) continue;
                assigned.Add(curr);
                comp.Add(curr);
                foreach (var next in candidates) {
                    if (assigned.Contains(next)) continue;
                    if (IsWallConnected(curr, next)) queue.Enqueue(next);
                }
            }
            if (comp.Count > 0) components.Add(comp);
        }

        // UR·DR 만나는 지점까지 벽 이어지게: UL·UR(먼 그리드)를 가장 많이 포함하는 연결 요소 선택 (전부 포함하는 게 없으면 최대 포함분으로)
        HashSet<(Vector2Int cell, int side)> connectedWalls = new HashSet<(Vector2Int cell, int side)>();
        if (components.Count > 0) {
            var ulUrCandidates = new HashSet<(Vector2Int cell, int side)>();
            foreach (var seg in candidates)
                if (seg.side == 0 || seg.side == 1) ulUrCandidates.Add(seg);
            int maxUlUr = -1;
            foreach (var c in components) {
                int count = 0;
                foreach (var seg in ulUrCandidates) if (c.Contains(seg)) count++;
                if (count > maxUlUr || (count == maxUlUr && (connectedWalls.Count == 0 || c.Count > connectedWalls.Count))) {
                    maxUlUr = count;
                    connectedWalls = c;
                }
            }
            if (connectedWalls.Count == 0) {
                var best = components[0];
                foreach (var c in components) if (c.Count > best.Count) best = c;
                connectedWalls = best;
            }
        }

        // 3. 같은 직선상의 벽을 스트립으로 병합 (코너에서 겹침/뚫림 방지)
        var strips = BuildWallStrips(connectedWalls);
        const float h = 0.5f;
        const float minStripLen = 0.55f; // 단일 세그먼트(코너)도 인접 벽과 겹쳐 보이도록 최소 길이
        foreach (var strip in strips) {
            if (strip.Count == 0) continue;
            int side = strip[0].side;
            int hxMin = strip[0].cell.x, hxMax = strip[0].cell.x, hzMin = strip[0].cell.y, hzMax = strip[0].cell.y;
            foreach (var w in strip) {
                int cx = w.cell.x, cy = w.cell.y;
                if (cx < hxMin) hxMin = cx; if (cx > hxMax) hxMax = cx;
                if (cy < hzMin) hzMin = cy; if (cy > hzMax) hzMax = cy;
            }
            float len = 0f;
            Vector3 center;
            Quaternion rot;
            if (side == 0 || side == 3) {
                len = (hzMax - hzMin + 1) * h;
                float cx = (side == 0) ? (hxMin * h - 0.05f) : ((hxMin + 1) * h + 0.05f);
                if (strip.Count > 1) {
                    int lineX = strip[0].side == 0 ? strip[0].cell.x : strip[0].cell.x + 1;
                    cx = lineX * h - 0.05f;
                }
                center = new Vector3(cx, 0f, (hzMin + hzMax + 1) * h * 0.5f);
                rot = Quaternion.identity;
            } else if (side == 1 || side == 2) {
                len = (hxMax - hxMin + 1) * h;
                float cz = (side == 1) ? (hzMin * h - 0.05f) : ((hzMin + 1) * h + 0.05f);
                if (strip.Count > 1) {
                    int lineZ = strip[0].side == 1 ? strip[0].cell.y : strip[0].cell.y + 1;
                    cz = lineZ * h - 0.05f;
                }
                center = new Vector3((hxMin + hxMax + 1) * h * 0.5f, 0f, cz);
                rot = Quaternion.Euler(0f, 90f, 0f);
            } else {
                len = (hzMax - hzMin + 1) * h;
                center = new Vector3((hxMin + 1) * h + 0.05f, 0f, (hzMin + hzMax + 1) * h * 0.5f);
                rot = Quaternion.identity;
            }
            if (len < minStripLen) len = minStripLen; // 예외: 너무 짧은 스트립(코너 1칸)도 최소 길이로 매끄럽게
            _activeWalls.Add(CreateWall(center, rot, wallParent.transform, len));
            foreach (var w in strip) _activeWallCells.Add(w);
        }

        CreateWallGrid(wallParent.transform);
        UpdateWallOccupancyColors();
    }

    static readonly Color WallColorDefault = new Color(0.4f, 0.7f, 0.7f);
    static readonly Color WallColorOccupied = new Color(0.25f, 0.45f, 0.45f);
    const float WallCellSize = 0.5f;

    void UpdateWallOccupancyColors()
    {
        const float planeTolerance = 0.2f;
        const float halfH = 1.25f;
        const float halfL = 0.55f;

        // 1. 벽 기본 색 복원
        for (int i = 0; i < _activeWalls.Count; i++)
        {
            var wallGo = _activeWalls[i];
            if (wallGo == null) continue;
            var rend = wallGo.GetComponent<Renderer>();
            if (rend != null && rend.material != null) rend.material.color = WallColorDefault;
        }

        // 2. 기존 점유 오버레이 제거
        foreach (var o in _wallOccupancyOverlays) if (o != null) Destroy(o);
        _wallOccupancyOverlays.Clear();

        Transform overlayRoot = null;
        GameObject wallsGo = GameObject.Find("Walls");
        if (wallsGo != null)
        {
            var existing = wallsGo.transform.Find("WallOccupancyOverlays");
            if (existing != null) Destroy(existing.gameObject);
            var root = new GameObject("WallOccupancyOverlays");
            root.transform.SetParent(wallsGo.transform, false);
            overlayRoot = root.transform;
        }

        if (overlayRoot == null) return;

        // 3. 각 벽을 0.5 단위 셀로 나누고, 가구가 점유한 셀만 오버레이로 표시
        for (int i = 0; i < _activeWalls.Count; i++)
        {
            GameObject wallGo = _activeWalls[i];
            if (wallGo == null || wallGo.name != "Wall") continue;

            Transform T = wallGo.transform;
            Vector3 C = T.position;
            Vector3 R = T.right;
            Vector3 U = T.up;
            Vector3 F = T.forward;

            // 벽 그리드와 동일: Y(높이) -1.25~1.25 step 0.5 → 셀 중심 -1,-0.5,0,0.5,1 / Z(가로) -0.55~0.55 step 0.5 → 셀 중심 -0.3, 0.2
            int numCellsU = Mathf.Max(1, Mathf.FloorToInt(halfH * 2f / WallCellSize));
            int numCellsF = Mathf.Max(1, Mathf.FloorToInt(halfL * 2f / WallCellSize));

            for (int iy = 0; iy < numCellsU; iy++)
            {
                float uOff = -halfH + WallCellSize * (0.5f + iy);
                if (uOff < -halfH || uOff > halfH) continue;
                for (int iz = 0; iz < numCellsF; iz++)
                {
                    float fOff = -halfL + WallCellSize * (0.5f + iz);
                    if (fOff < -halfL || fOff > halfL) continue;

                    Vector3 cellCenter = C + U * uOff + F * fOff;

                    bool occupied = false;
                    foreach (var f in allFurnitures)
                    {
                        if (f == null || !f.isAttachedToWall) continue;
                        Vector3 P = f.transform.position;
                        float distToPlane = Mathf.Abs(Vector3.Dot(P - C, R));
                        if (distToPlane > planeTolerance) continue;
                        // 이 벽 세그먼트 위에 있는 가구만 (U·F 범위 안)
                        if (Mathf.Abs(Vector3.Dot(P - C, U)) > halfH || Mathf.Abs(Vector3.Dot(P - C, F)) > halfL) continue;

                        float halfWy = Mathf.Max(0.25f, f.wallSize.y * 0.5f);
                        float halfWx = Mathf.Max(0.25f, f.wallSize.x * 0.5f);
                        float du = Mathf.Abs(Vector3.Dot(cellCenter - P, U));
                        float df = Mathf.Abs(Vector3.Dot(cellCenter - P, F));
                        if (du <= halfWy + WallCellSize * 0.5f && df <= halfWx + WallCellSize * 0.5f)
                        {
                            occupied = true;
                            break;
                        }
                    }

                    if (!occupied) continue;

                    // 벽면에 밀착: 벽 두께 0.1 → 면이 중심에서 ±0.05, Quad 법선이 방 안쪽을 향하도록 회전
                    Vector3 gridCenter = new Vector3(gridWidth / 2f, 0f, gridHeight / 2f);
                    float towardRoom = Vector3.Dot(gridCenter - C, R);
                    const float wallHalfThickness = 0.05f;
                    float push = towardRoom > 0f ? wallHalfThickness : -wallHalfThickness;
                    Vector3 faceNormal = towardRoom > 0f ? R : -R; // 방 안쪽을 향하는 법선
                    Quaternion rot = Quaternion.LookRotation(faceNormal, U);

                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "WallOccupiedCell";
                    quad.transform.SetParent(overlayRoot);
                    quad.transform.position = cellCenter + R * push;
                    quad.transform.rotation = rot;
                    quad.transform.localScale = new Vector3(WallCellSize, WallCellSize, 1f);
                    var col = quad.GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                    var rend = quad.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material = new Material(Shader.Find("Sprites/Default"));
                        rend.material.color = new Color(WallColorOccupied.r, WallColorOccupied.g, WallColorOccupied.b, 0.85f);
                    }
                    _wallOccupancyOverlays.Add(quad);
                }
            }
        }
    }

    void CreateWallGrid(Transform parent) {
        foreach (var line in _wallGridLines) if (line != null) Destroy(line);
        _wallGridLines.Clear();

        Transform existing = parent.Find("WallGridRoot");
        if (existing != null) Destroy(existing.gameObject);

        GameObject gridRoot = new GameObject("WallGridRoot");
        gridRoot.transform.SetParent(parent);

        foreach (var wallGo in _activeWalls) {
            if (wallGo == null || wallGo.name != "Wall") continue;
            Transform w = wallGo.transform;
            Vector3 center = w.position;
            // 벽면 방향: 로컬 Y=높이(위), 로컬 Z=가로(벽 길이). 월드에서 벽의 up/right 사용
            Vector3 wallUp = w.TransformDirection(0, 1, 0);   // 높이 방향
            Vector3 wallAlong = w.TransformDirection(0, 0, 1); // 벽 가로 방향
            Vector3 scale = w.lossyScale;
            float halfH = scale.y * 0.5f; // 1.25
            float halfL = scale.z * 0.5f; // 0.55

            // UL/UR 구분 없이 양면 모두 그리드 (카메라가 보는 쪽에 항상 보이도록)
            Vector3 outNorm = w.TransformDirection(1, 0, 0);
            float push = scale.x * 0.51f;
            Vector3[] faceCenters = { center + outNorm * push, center - outNorm * push };

            const float gridStep = 0.5f;
            foreach (Vector3 faceCenter in faceCenters) {
                // 가로선
                for (float yOff = -halfH; yOff <= halfH + 0.01f; yOff += gridStep) {
                    Vector3 p = faceCenter + wallUp * yOff;
                    _wallGridLines.Add(CreateWallGridLineWorld(gridRoot.transform, p - wallAlong * halfL, p + wallAlong * halfL));
                }
                // 세로선
                for (float lOff = -halfL; lOff <= halfL + 0.01f; lOff += gridStep) {
                    Vector3 p = faceCenter + wallAlong * lOff;
                    _wallGridLines.Add(CreateWallGridLineWorld(gridRoot.transform, p - wallUp * halfH, p + wallUp * halfH));
                }
            }
        }
    }

    GameObject CreateWallGridLineWorld(Transform parent, Vector3 worldStart, Vector3 worldEnd) {
        GameObject line = new GameObject("WallGridLine");
        line.transform.SetParent(parent);
        line.transform.localPosition = Vector3.zero;
        line.transform.localRotation = Quaternion.identity;
        line.transform.localScale = Vector3.one;
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(gridColor.r, gridColor.g, gridColor.b, 0.6f);
        lr.startWidth = lr.endWidth = Mathf.Max(0.03f, lineWidth * 1.5f);
        lr.positionCount = 2;
        lr.SetPosition(0, worldStart);
        lr.SetPosition(1, worldEnd);
        return line;
    }

    // 두 벽 세그먼트가 정점을 공유하는지 확인
    bool IsWallConnected((Vector2Int cell, int side) w1, (Vector2Int cell, int side) w2) {
        Vector2Int[] v1 = GetSideVertices(w1.cell, w1.side);
        Vector2Int[] v2 = GetSideVertices(w2.cell, w2.side);

        foreach (var p1 in v1) {
            foreach (var p2 in v2) {
                if (p1 == p2) return true;
            }
        }
        return false;
    }

    Vector2Int[] GetSideVertices(Vector2Int c, int side) {
        if (side == 0) return new Vector2Int[] { c, c + Vector2Int.up };
        if (side == 1) return new Vector2Int[] { c, c + Vector2Int.right };
        if (side == 2) return new Vector2Int[] { c + Vector2Int.up, c + new Vector2Int(1, 1) };
        if (side == 3) return new Vector2Int[] { c + Vector2Int.right, c + new Vector2Int(1, 1) };
        return new Vector2Int[0];
    }

    // 같은 물리 직선 상의 이웃 (세로: side 0·3 동일 x선 병합, 가로: side 1·2 동일 z선 병합)
    static void GetCollinearNeighbors(Vector2Int cell, int side, HashSet<(Vector2Int, int)> set, List<(Vector2Int cell, int side)> outNeighbors) {
        outNeighbors.Clear();
        if (side == 0 || side == 3) {
            var a = (cell + Vector2Int.down, side);
            var b = (cell + Vector2Int.up, side);
            if (set.Contains(a)) outNeighbors.Add(a);
            if (set.Contains(b)) outNeighbors.Add(b);
            // 같은 세로선(x=const): side 0 (cell.x=x) ↔ side 3 (cell.x=x-1) → (hx-1,hz) side 3 / (hx+1,hz) side 0
            if (side == 0) {
                var c = (cell + new Vector2Int(-1, 0), 3);
                var d = (cell + new Vector2Int(-1, 1), 3);
                if (set.Contains(c)) outNeighbors.Add(c);
                if (set.Contains(d)) outNeighbors.Add(d);
            } else {
                var c = (cell + new Vector2Int(1, 0), 0);
                var d = (cell + new Vector2Int(1, 1), 0);
                if (set.Contains(c)) outNeighbors.Add(c);
                if (set.Contains(d)) outNeighbors.Add(d);
            }
        } else {
            var a = (cell + Vector2Int.left, side);
            var b = (cell + Vector2Int.right, side);
            if (set.Contains(a)) outNeighbors.Add(a);
            if (set.Contains(b)) outNeighbors.Add(b);
            // 같은 가로선(z=hz): side 1 (cell.y=hz) ↔ side 2 (cell.y=hz-1) → (hx±1,hz) side 1 / (hx±1,hz-1) side 2
            if (side == 1) {
                var c = (cell + new Vector2Int(1, -1), 2);
                var d = (cell + new Vector2Int(-1, -1), 2);
                if (set.Contains(c)) outNeighbors.Add(c);
                if (set.Contains(d)) outNeighbors.Add(d);
            } else if (side == 2) {
                var c = (cell + new Vector2Int(1, 1), 1);
                var d = (cell + new Vector2Int(-1, 1), 1);
                if (set.Contains(c)) outNeighbors.Add(c);
                if (set.Contains(d)) outNeighbors.Add(d);
            }
        }
    }

    static List<List<(Vector2Int cell, int side)>> BuildWallStrips(HashSet<(Vector2Int cell, int side)> connectedWalls) {
        var strips = new List<List<(Vector2Int cell, int side)>>();
        var remaining = new HashSet<(Vector2Int cell, int side)>(connectedWalls);
        var neighborBuf = new List<(Vector2Int cell, int side)>();
        while (remaining.Count > 0) {
            var strip = new List<(Vector2Int cell, int side)>();
            var q = new Queue<(Vector2Int cell, int side)>();
            var first = default((Vector2Int, int));
            foreach (var x in remaining) { first = x; break; }
            q.Enqueue(first);
            while (q.Count > 0) {
                var curr = q.Dequeue();
                if (!remaining.Remove(curr)) continue;
                strip.Add(curr);
                GetCollinearNeighbors(curr.cell, curr.side, remaining, neighborBuf);
                foreach (var n in neighborBuf) q.Enqueue(n);
            }
            if (strip.Count > 0) strips.Add(strip);
        }
        return strips;
    }

    GameObject CreateWall(Vector3 pos, Quaternion rot, Transform parent, float length = 1.1f) {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.position = pos + Vector3.up * 1.25f;
        wall.transform.rotation = rot;
        wall.transform.localScale = new Vector3(0.1f, 2.5f, Mathf.Max(0.1f, length)); 
        
        var rend = wall.GetComponent<Renderer>();
        if (rend != null) {
            // URP와 빌트인 모두 호환되는 쉐이더 찾기
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            rend.material = new Material(shader);
            rend.material.color = new Color(0.4f, 0.7f, 0.7f);
        }
        
        // 벽 가구 배치: 레이캐스트에 잡히도록 Default 레이어 유지, 태그만 설정
        wall.tag = "Wall";
        var col = wall.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

        return wall;
    }

    void ClearExpansionButtons() {
        foreach (var b in _expansionButtons) if (b != null) Destroy(b);
        _expansionButtons.Clear();
        _isSelectingExpansion = false;
    }

    void StartFurnitureEnhancementPhase() {
        if (allFurnitures.Count == 0) {
            CheckRoundAndContinue();
            return;
        }

        if (_enhancementUI == null) _enhancementUI = FurnitureEnhancementUI.Create();

        // 랜덤하게 가구 하나 선택
        Furniture target = allFurnitures[UnityEngine.Random.Range(0, allFurnitures.Count)];
        
        // 강화 옵션 생성
        List<IFurnitureEnhancement> options = new List<IFurnitureEnhancement> {
            new HappyEnhancement(),
            new ActionPointEnhancement(),
            new GridExpansionEnhancement()
        };

        _enhancementUI.Show(target, options, () => {
            CheckRoundAndContinue();
        });
    }

    void CheckRoundAndContinue() {
        if (stageDataList == null) return;

        StageData currentStage;
        if (_currentStageIndex < stageDataList.Count) {
            // 리스트에 있는 데이터를 기반으로 동적 생성 (사용자 배치 유지)
            currentStage = StageGenerator.GenerateStage(_currentStageIndex, stageDataList[_currentStageIndex]);
        } else {
            // 데이터가 없으면 완전 자동 생성
            currentStage = StageGenerator.GenerateStage(_currentStageIndex);
        }

        if (_currentRoundIndex < currentStage.rounds.Count) {
            RoundConfig currentRound = currentStage.rounds[_currentRoundIndex];
            
            if (_turnsInCurrentRound >= currentRound.turnsInRound) {
                if (happyTotal < currentRound.requiredHappy) {
                    ShowGameOver(currentRound.requiredHappy);
                } else {
                    // 라운드 클리어
                    if (_currentRoundIndex + 1 < currentStage.rounds.Count) {
                        ShowRoundClear(_currentRoundIndex + 1);
                    } else {
                        // 스테이지 클리어
                        ShowStageClear(_currentStageIndex + 1);
                    }
                }
                return;
            }
        }
        FinishEndTurn();
    }

    void FinishEndTurn() {
        // 보너스 AP 계산 (영구 보너스 합산)
        int bonusAP = 0;
        foreach(var f in allFurnitures) {
            bonusAP += f.permanentBonusAP;
        }

        actionPoints = 3 + bonusAP;
        UpdateIndicators();
        if (_selectionManager == null) _selectionManager = FindObjectOfType<FurnitureSelectionManager>();
        if (_selectionManager != null) _selectionManager.ShowSelectionAgain();
    }

    public void GenerateRandomDelivery() {
        if (furniturePrefabs.Count == 0) return;
        GameObject refPrefab = furniturePrefabs[Random.Range(0, furniturePrefabs.Count)];
        Furniture f = refPrefab.GetComponent<Furniture>();
        Vector2Int sz = f.EffectiveSize;
        List<Vector2Int> shapeCells = null;
        if (f.useCustomShape && f.customShapeCells.Count > 0)
            shapeCells = new List<Vector2Int>(f.customShapeCells);
        int maxGx = Mathf.Max(0, gridWidth - sz.x);
        int maxGy = Mathf.Max(0, gridHeight - sz.y);
        upcomingDeliveries.Add(new UpcomingDelivery {
            gridPos = new Vector2Int(Random.Range(0, maxGx + 1), Random.Range(0, maxGy + 1)),
            size = sz, shapeCells = shapeCells, turnsLeft = Random.Range(1, 4), prefab = refPrefab
        });
    }

    System.Collections.IEnumerator ShakeRoutine(Furniture f, Vector2Int origin) {
        float t = 0;
        f.gridPos = origin;
        Vector3 start = f.GetWorldPosition();
        while (t < 0.3f) { t += Time.deltaTime; f.transform.position = start + new Vector3(Mathf.Sin(t * 40f) * 0.15f, 0, 0); yield return null; }
        f.UpdateVisualPosition();
    }

    System.Collections.IEnumerator FallRoutine(Transform t, Vector3 end) {
        float et = 0; Vector3 sp = t.position;
        while (et < 0.5f) { et += Time.deltaTime; t.position = Vector3.Lerp(sp, end, et / 0.5f); yield return null; }
        t.position = end;
    }

    Vector3 GetMouseWorldPos() {
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (new Plane(Vector3.up, Vector3.zero).Raycast(r, out float d)) return r.GetPoint(d);
        return Vector3.zero;
    }

    bool IsOccupied(Vector2Int p, Vector2Int s, Furniture self) {
        int hw = s.x * 2, hh = s.y * 2;
        foreach (var f in allFurnitures) {
            if (f == self || f == null || !f.gameObject.activeInHierarchy) continue;
            foreach (var c in f.GetOccupiedCells())
                for (int x = 0; x < hw; x++)
                    for (int y = 0; y < hh; y++)
                        if (c == new Vector2Int(p.x + x, p.y + y)) return true;
        }
        return false;
    }

    bool IsPlacementValid(Furniture f, Vector2Int target) {
        var cells = f.GetOccupiedCellsAt(target);
        foreach (var c in cells) {
            // 해당 셀이 유효한 그리드 영역 내에 있는지 확인
            if (!validCells.Contains(c)) return false;
            
            foreach (var other in allFurnitures) {
                if (other == f || other == null || !other.gameObject.activeInHierarchy) continue;
                if (other.GetOccupiedCells().Contains(c)) return false;
            }
        }
        return true;
    }

    Furniture GetFurnitureAt(Vector2Int p, Furniture exclude = null) {
        foreach (var f in allFurnitures) {
            if (f == exclude || f == null || !f.gameObject.activeInHierarchy) continue;
            foreach (var c in f.GetOccupiedCells()) if (c == p) return f;
        }
        return null;
    }

    Furniture GetStorageAtArea(Vector2Int target, Vector2Int itemSize, Furniture itemToStore) {
        var cells = new List<Vector2Int>();
        int hw = itemSize.x * 2, hh = itemSize.y * 2;
        for (int x = 0; x < hw; x++)
            for (int y = 0; y < hh; y++)
                cells.Add(new Vector2Int(target.x + x, target.y + y));
        foreach (var cell in cells) {
            var f = GetFurnitureAt(cell, itemToStore);
            if (f != null && f != itemToStore && f.type == FurnitureType.Storage &&
                (f.acceptableCategories.Count == 0 || f.acceptableCategories.Contains(itemToStore.category)) &&
                f.storedItems.Count < f.storageCapacity)
                return f;
        }
        return null;
    }

    void StoreFurniture(Furniture i, Furniture s) {
        s.storedItems.Add(i); allFurnitures.Remove(i);
        i.transform.SetParent(s.transform); i.gameObject.SetActive(false);
        if (i.isAttachedToWall) i.isAttachedToWall = false;
        UpdateWallOccupancyColors();
    }

    int GetPlacedFurnitureHappySum() {
        int sum = 0;
        foreach (var (_, amount) in GetPlacedFurnitureHappyBreakdown())
            sum += amount;
        return sum;
    }

    /// <summary>가구별 Happy 기여도. 턴 종료 시 팝업 표시용.</summary>
    List<(Furniture f, int amount)> GetPlacedFurnitureHappyBreakdown() {
        var list = new List<(Furniture, int)>();
        foreach (var f in allFurnitures) {
            if (f == null || !f.gameObject.activeInHierarchy) continue;
            int amount = f.happyValue;
            if (f.type == FurnitureType.Storage && f.storedItems != null) {
                foreach (var item in f.storedItems) {
                    if (item == null) continue;
                    amount += item.happyValue;
                    if (item.category != FurnitureCategory.None &&
                        f.acceptableCategories != null && f.acceptableCategories.Contains(item.category))
                        amount += synergyBonusPerMatch;
                }
            }
            list.Add((f, amount));
        }
        return list;
    }

    /// <summary>가구 위에 Happy 수치 팝업을 띄우고, 위로 떠오르며 사라지는 연출.</summary>
    void ShowHappyPopup(Furniture furniture, int amount) {
        var go = new GameObject("HappyPopup");
        go.transform.position = furniture.transform.position + Vector3.up * (furniture.nameLabelHeight + 0.5f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = "+" + amount;
        tm.fontSize = 24;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.9f, 0.3f);
        tm.characterSize = 0.25f;
        StartCoroutine(FloatHappyPopupRoutine(go, go.transform.position));
    }

    System.Collections.IEnumerator FloatHappyPopupRoutine(GameObject popupObj, Vector3 startPos) {
        float duration = HappyPopupDuration;
        float elapsed = 0f;
        var tm = popupObj.GetComponent<TextMesh>();
        if (tm == null) yield break;
        var cam = Camera.main;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            popupObj.transform.position = startPos + Vector3.up * (t * 1.5f);
            if (cam != null) {
                Vector3 toCam = cam.transform.position - popupObj.transform.position;
                popupObj.transform.rotation = Quaternion.LookRotation(-toCam);
            }
            var c = tm.color;
            c.a = 1f - t;
            tm.color = c;
            yield return null;
        }
        if (popupObj != null) Destroy(popupObj);
    }
}
