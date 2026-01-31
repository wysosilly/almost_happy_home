using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 화면. Start 클릭 시 가구 선택 UI로 이동.
/// </summary>
public class GameStartUI : MonoBehaviour
{
    public GameObject gameStartPanel;
    public FurnitureSelectionManager selectionManager;

    void Start()
    {
        if (selectionManager == null) selectionManager = FindObjectOfType<FurnitureSelectionManager>();
        if (gameStartPanel == null) CreateGameStartUI();
        ShowGameStart();
    }

    void CreateGameStartUI()
    {
        var canvasObj = new GameObject("GameStartCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        gameStartPanel = new GameObject("GameStartPanel");
        gameStartPanel.transform.SetParent(canvasObj.transform, false);
        var panelRect = gameStartPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        var panelImg = gameStartPanel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(gameStartPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.6f);
        titleRect.anchorMax = new Vector2(0.5f, 0.6f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(500, 80);
        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "Furniture Game";
        titleText.fontSize = 56;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) titleText.font = font;
        var btnObj = new GameObject("StartButton");
        btnObj.transform.SetParent(gameStartPanel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.35f);
        btnRect.anchorMax = new Vector2(0.5f, 0.35f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(220, 60);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.35f, 0.2f, 1f);
        var btn = btnObj.AddComponent<Button>();
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Start";
        btnText.fontSize = 32;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        if (font != null) btnText.font = font;
        btn.onClick.AddListener(OnStartClicked);
    }

    void OnStartClicked()
    {
        if (gameStartPanel != null) gameStartPanel.SetActive(false);
        if (selectionManager != null) selectionManager.ShowSelectionAgain();
    }

    public void ShowGameStart()
    {
        if (gameStartPanel != null) gameStartPanel.SetActive(true);
    }
}
