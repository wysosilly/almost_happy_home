using UnityEngine;
using System.Collections.Generic;
using TMPro;

public partial class GridGameManager : MonoBehaviour
{
    [Header("Grid Settings")]
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
    [Tooltip("10개 스테이지. 각 스테이지에서 turnCheck 턴에 도달 시 requiredHappy 미달이면 게임 오버")]
    public List<StageConfig> stages = new List<StageConfig>();

    [Header("Debug")]
    [SerializeField] bool debugStorage = false;
    
    private Furniture selectedFurniture;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Vector2Int originalGridPos;
    private List<GameObject> activeIndicators = new List<GameObject>();
    private List<GameObject> _furnitureOverlays = new List<GameObject>();
    private List<GameObject> _dropPreviewIndicators = new List<GameObject>();
    private bool _gridInitialized;
    private FurnitureSelectionManager _selectionManager;
    private int _currentTurn;
    private int _currentStageIndex;
    private bool _gameOver;
    private GameObject _gameOverUI;
    private GameObject _stageClearUI;
    private bool _waitingForNextStage;

    [Header("Flow")]
    [Tooltip("비우면 FurnitureSelectionManager가 먼저 표시됨")]
    public bool skipSelection;
    [Tooltip("있으면 게임 오버 Retry 시 게임 시작 UI로 이동")]
    public GameStartUI gameStartUI;

    void Awake() {
        if (!skipSelection && gameStartUI == null) {
            var go = new GameObject("GameStartUI");
            gameStartUI = go.AddComponent<GameStartUI>();
        }
    }

    void Start() {
        EnsureStages();
        if (skipSelection) BeginGame(null);
    }

    public void BeginGame(GameObject selectedFurniturePrefab) {
        if (!_gridInitialized) {
            RegisterSceneFurniture();
            SetupScene();
            CreateVisualGrid();
            _gridInitialized = true;
            _currentTurn = 0;
            _currentStageIndex = 0;
            _gameOver = false;
        }
        if (_gameOver) return;
        if (_selectionManager == null) _selectionManager = FindObjectOfType<FurnitureSelectionManager>();
        AddSelectedDelivery(selectedFurniturePrefab);
        UpdateIndicators();
    }

    void AddSelectedDelivery(GameObject prefab) {
        if (prefab == null) return;
        var f = prefab.GetComponent<Furniture>();
        if (f == null) return;
        var sz = f.EffectiveSize;
        int maxX = Mathf.Max(0, gridWidth - sz.x);
        int maxY = Mathf.Max(0, gridHeight - sz.y);
        if (maxX < 0 || maxY < 0) return;
        Vector2Int pos = new Vector2Int(Random.Range(0, maxX + 1), Random.Range(0, maxY + 1));
        List<Vector2Int> shapeCells = null;
        if (f.useCustomShape && f.customShapeCells.Count > 0)
            shapeCells = new List<Vector2Int>(f.customShapeCells);
        upcomingDeliveries.Add(new UpcomingDelivery {
            gridPos = pos, size = sz, shapeCells = shapeCells, turnsLeft = 1, prefab = prefab
        });
    }

    void RegisterSceneFurniture() {
        var sceneFurniture = FindObjectsOfType<Furniture>();
        foreach (var f in sceneFurniture) {
            if (allFurnitures.Contains(f)) continue;
            if (f.transform.parent != null && f.transform.parent.GetComponent<Furniture>() != null)
                continue;
            var fs = f.EffectiveSize;
            float ox = f.pivotAtCenter ? fs.x * 0.5f : 0;
            float oy = f.pivotAtCenter ? fs.y * 0.5f : 0;
            f.gridPos = new Vector2Int(
                Mathf.FloorToInt(f.transform.position.x - ox),
                Mathf.FloorToInt(f.transform.position.z - oy));
            allFurnitures.Add(f);
        }
    }

    void Update() {
        if (_gameOver || _waitingForNextStage) return;
        HandleMouseInput();
        UpdateDropPreview();
        if (apText != null) apText.text = $"Action Points: {actionPoints}  Happy: {happyTotal}  Turn: {_currentTurn}  Stage: {_currentStageIndex + 1}/10";
        if (Input.GetKeyDown(KeyCode.Space)) EndTurn();
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
        Camera.main.orthographicSize = Mathf.Max(gridWidth, gridHeight) / 2f + 2f;
        var camCtrl = Camera.main.GetComponent<IsometricCameraController>();
        if (camCtrl == null) camCtrl = Camera.main.gameObject.AddComponent<IsometricCameraController>();
        camCtrl.Init(gridCenter, camPos);
    }

    void CreateVisualGrid() {
        GameObject gridParent = new GameObject("VisualGrid");
        for (int i = 0; i <= gridWidth; i++) 
            CreateLine(new Vector3(i, 0.01f, 0), new Vector3(i, 0.01f, gridHeight), gridParent.transform);
        for (int i = 0; i <= gridHeight; i++) 
            CreateLine(new Vector3(0, 0.01f, i), new Vector3(gridWidth, 0.01f, i), gridParent.transform);
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
        if (actionPoints <= 0) return;
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
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
            Vector3 mPos = GetMouseWorldPos() + dragOffset;
            selectedFurniture.transform.position = new Vector3(mPos.x, 1.2f, mPos.z);
        }
        if (Input.GetMouseButtonUp(0) && isDragging) {
            isDragging = false;
            var item = selectedFurniture;
            selectedFurniture = null;

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
            }

            Vector3 dropPos = GetMouseWorldPos() + dragOffset;
            var itemSize = item.EffectiveSize;
            Vector2Int targetPos = item.pivotAtCenter
                ? new Vector2Int(Mathf.FloorToInt(dropPos.x - itemSize.x * 0.5f), Mathf.FloorToInt(dropPos.z - itemSize.y * 0.5f))
                : new Vector2Int(Mathf.FloorToInt(dropPos.x), Mathf.FloorToInt(dropPos.z));
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
        } else if (target.x >= 0 && target.x + f.EffectiveSize.x <= gridWidth && 
                   target.y >= 0 && target.y + f.EffectiveSize.y <= gridHeight && !IsOccupied(target, f.EffectiveSize, f)) {
            f.Move(target);
            actionPoints--;
            moveFailed = false;
        }

        if (moveFailed) StartCoroutine(ShakeRoutine(f, originalGridPos));
    }

    void ProcessDelivery(UpcomingDelivery d) {
        HashSet<Vector2Int> deliveryCells = new HashSet<Vector2Int>();
        if (d.shapeCells != null && d.shapeCells.Count > 0) {
            foreach (var cell in d.shapeCells)
                deliveryCells.Add(new Vector2Int(d.gridPos.x + cell.x, d.gridPos.y + cell.y));
        } else {
            for (int x = 0; x < d.size.x; x++)
                for (int y = 0; y < d.size.y; y++)
                    deliveryCells.Add(new Vector2Int(d.gridPos.x + x, d.gridPos.y + y));
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
            f.gridPos = d.gridPos;
            f.ApplyVisualScale();
            allFurnitures.Add(f);
            Vector3 target = f.GetWorldPosition();
            go.transform.position = target + Vector3.up * 10f;
            StartCoroutine(FallRoutine(go.transform, target));
        }
    }

    public void EndTurn() {
        if (_gameOver || _waitingForNextStage) return;
        EnsureStages();
        happyTotal += GetPlacedFurnitureHappySum();
        for (int i = upcomingDeliveries.Count - 1; i >= 0; i--) {
            upcomingDeliveries[i].turnsLeft--;
            if (upcomingDeliveries[i].turnsLeft <= 0) { ProcessDelivery(upcomingDeliveries[i]); upcomingDeliveries.RemoveAt(i); }
        }
        _currentTurn++;
        while (_currentStageIndex < stages.Count && _currentTurn >= stages[_currentStageIndex].turnCheck) {
            int required = stages[_currentStageIndex].requiredHappy;
            if (happyTotal < required) {
                ShowGameOver(required);
                return;
            } else {
                ShowStageClear(_currentStageIndex + 1);
                return;
            }
        }
        actionPoints = 3;
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
        upcomingDeliveries.Add(new UpcomingDelivery {
            gridPos = new Vector2Int(Random.Range(0, gridWidth - sz.x + 1), Random.Range(0, gridHeight - sz.y + 1)),
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
        foreach (var f in allFurnitures) {
            if (f == self || f == null || !f.gameObject.activeInHierarchy) continue;
            foreach (var c in f.GetOccupiedCells())
                for (int x=0; x<s.x; x++) for (int y=0; y<s.y; y++)
                    if (c == new Vector2Int(p.x + x, p.y + y)) return true;
        }
        return false;
    }

    bool IsPlacementValid(Furniture f, Vector2Int target) {
        var cells = f.GetOccupiedCellsAt(target);
        foreach (var c in cells) {
            if (c.x < 0 || c.x >= gridWidth || c.y < 0 || c.y >= gridHeight) return false;
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
        for (int x = 0; x < itemSize.x; x++)
            for (int y = 0; y < itemSize.y; y++)
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
    }

    int GetPlacedFurnitureHappySum() {
        int sum = 0;
        foreach (var f in allFurnitures) {
            if (f == null || !f.gameObject.activeInHierarchy) continue;
            sum += f.happyValue;
            if (f.type == FurnitureType.Storage && f.storedItems != null) {
                foreach (var item in f.storedItems) {
                    if (item == null) continue;
                    sum += item.happyValue;
                    if (item.category != FurnitureCategory.None &&
                        f.acceptableCategories != null && f.acceptableCategories.Contains(item.category))
                        sum += synergyBonusPerMatch;
                }
            }
        }
        return sum;
    }
}
