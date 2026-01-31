using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 가구 선택 화면. furniturePrefabs에서 랜덤 3개를 보여주고 하나를 선택하면 게임 시작.
/// </summary>
public class FurnitureSelectionManager : MonoBehaviour
{
    public GridGameManager gridGameManager;
    public GameObject selectionPanel;
    public RectTransform cardContainer;
    public GameObject cardPrefab;
    [Tooltip("폰트 (비우면 Unity 기본 폰트 사용)")]
    public TMP_FontAsset customFont;

    private List<GameObject> _cardUIs = new List<GameObject>();

    void Start()
    {
        if (gridGameManager == null) gridGameManager = FindObjectOfType<GridGameManager>();
        if (gridGameManager == null) { Debug.LogError("GridGameManager를 찾을 수 없습니다."); return; }
        if (gridGameManager.furniturePrefabs.Count < 3) { StartGameWithRandom(); return; }
        if (selectionPanel == null) CreateSelectionUI();
        var gameStart = FindObjectOfType<GameStartUI>();
        if (gameStart != null) { selectionPanel.SetActive(false); return; }
        ShowSelectionCards();
    }

    void CreateSelectionUI()
    {
        var canvasObj = new GameObject("FurnitureSelectionCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        selectionPanel = new GameObject("SelectionPanel");
        selectionPanel.transform.SetParent(canvasObj.transform, false);
        var panelRect = selectionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        var panelImg = selectionPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.98f);
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(selectionPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.85f);
        titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(400, 60);
        var titleText = titleObj.AddComponent<Text>();
        var titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (titleFont != null) titleText.font = titleFont;
        titleText.text = "Select Furniture";
        titleText.fontSize = 28;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        var containerObj = new GameObject("CardContainer");
        containerObj.transform.SetParent(selectionPanel.transform, false);
        cardContainer = containerObj.AddComponent<RectTransform>();
        cardContainer.anchorMin = new Vector2(0, 0.1f);
        cardContainer.anchorMax = new Vector2(1, 0.75f);
        cardContainer.offsetMin = new Vector2(20, 20);
        cardContainer.offsetMax = new Vector2(-20, -20);
    }

    void ShowSelectionCards()
    {
        var prefabs = gridGameManager.furniturePrefabs;
        var indices = GetRandomIndices(prefabs.Count, 3);
        for (int i = 0; i < 3; i++)
        {
            GameObject prefab = prefabs[indices[i]];
            var card = CreateCard(prefab, i);
            _cardUIs.Add(card);
        }
    }

    List<int> GetRandomIndices(int maxCount, int need)
    {
        var list = new List<int>();
        for (int i = 0; i < maxCount; i++) list.Add(i);
        while (list.Count > need) list.RemoveAt(Random.Range(0, list.Count));
        return list;
    }

    GameObject CreateCard(GameObject furniturePrefab, int index)
    {
        GameObject card = cardPrefab != null ? Instantiate(cardPrefab, cardContainer) : CreateDefaultCard();
        var furniture = furniturePrefab.GetComponent<Furniture>();
        string name = furniture != null && !string.IsNullOrEmpty(furniture.displayName) ? furniture.displayName : furniturePrefab.name;
        var tmp = card.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) {
            tmp.gameObject.SetActive(true);
            var font = GetFont();
            if (font != null) tmp.font = font;
            tmp.text = name;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.ForceMeshUpdate();
        } else {
            var legacyText = card.GetComponentInChildren<Text>();
            if (legacyText != null) { legacyText.text = name; legacyText.color = Color.white; }
        }
        var btn = card.GetComponent<Button>();
        if (btn == null) btn = card.AddComponent<Button>();
        var fp = furniturePrefab;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardSelected(fp));
        var rect = card.GetComponent<RectTransform>();
        if (rect != null && cardContainer != null) {
            rect.anchorMin = new Vector2(index / 3f + 0.05f, 0.2f);
            rect.anchorMax = new Vector2((index + 1) / 3f - 0.05f, 0.8f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        return card;
    }

    GameObject CreateDefaultCard()
    {
        var card = new GameObject("FurnitureCard");
        card.transform.SetParent(cardContainer, false);
        var rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 250);
        var img = card.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(card.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        var legacyText = textObj.AddComponent<Text>();
        legacyText.text = "";
        var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtinFont != null) legacyText.font = builtinFont;
        legacyText.fontSize = 22;
        legacyText.alignment = TextAnchor.MiddleCenter;
        legacyText.color = Color.white;
        legacyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        legacyText.verticalOverflow = VerticalWrapMode.Overflow;
        card.AddComponent<Button>();
        return card;
    }

    void OnCardSelected(GameObject furniturePrefab)
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        foreach (var c in _cardUIs) if (c != null) Destroy(c);
        _cardUIs.Clear();
        gridGameManager.BeginGame(furniturePrefab);
    }

    public void ShowSelectionAgain()
    {
        if (gridGameManager == null || gridGameManager.furniturePrefabs.Count < 3) { StartGameWithRandom(); return; }
        foreach (var c in _cardUIs) if (c != null) Destroy(c);
        _cardUIs.Clear();
        if (selectionPanel != null) selectionPanel.SetActive(true);
        ShowSelectionCards();
    }

    TMP_FontAsset GetFont()
    {
        if (customFont != null) return customFont;
        return TMP_Settings.defaultFontAsset;
    }

    void StartGameWithRandom()
    {
        GameObject prefab = null;
        if (gridGameManager != null && gridGameManager.furniturePrefabs.Count > 0)
            prefab = gridGameManager.furniturePrefabs[Random.Range(0, gridGameManager.furniturePrefabs.Count)];
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (gridGameManager != null) gridGameManager.BeginGame(prefab);
    }
}
