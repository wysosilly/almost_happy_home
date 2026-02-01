using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public partial class GridGameManager
{
    private GameObject _roundClearUI;

    void ShowGameOver(int requiredHappy)
    {
        _gameOver = true;
        if (_selectionManager != null && _selectionManager.selectionPanel != null)
            _selectionManager.selectionPanel.SetActive(false);
        if (_gameOverUI != null) return;
        _gameOverUI = CreateGameOverUI(requiredHappy);
        if (_gameOverUI != null) _gameOverUI.SetActive(true);
    }

    GameObject CreateGameOverUI(int requiredHappy)
    {
        var canvasObj = new GameObject("GameOverCanvas");
        canvasObj.SetActive(true);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0f;
        canvasObj.AddComponent<GraphicRaycaster>();
        if (canvasObj.GetComponent<CanvasFitInScreen>() == null)
            canvasObj.AddComponent<CanvasFitInScreen>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.25f, 0.05f, 0.05f, 0.98f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "Game Over";
        titleText.fontSize = 36; // 48 -> 36
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = font;
        titleObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        var detailObj = new GameObject("Detail");
        detailObj.transform.SetParent(panel.transform, false);
        var detailText = detailObj.AddComponent<Text>();
        detailText.text = $"Required Happy: {requiredHappy}\nYour Happy: {happyTotal}";
        detailText.fontSize = 18; // 24 -> 18
        detailText.color = Color.white;
        detailText.alignment = TextAnchor.MiddleCenter;
        detailText.font = font;
        detailObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        var btnObj = new GameObject("RetryButton");
        btnObj.transform.SetParent(panel.transform, false);
        btnObj.AddComponent<Image>().color = new Color(0.3f, 0.2f, 0.2f, 1f);
        var btn = btnObj.AddComponent<Button>();
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Retry";
        btnText.fontSize = 20; // 28 -> 20
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.font = font;
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 50);
        btnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        btn.onClick.AddListener(OnRetryGameOver);

        return canvasObj;
    }

    void OnRetryGameOver()
    {
        if (_gameOverUI != null) Destroy(_gameOverUI);
        _gameOver = false;
        _currentTurn = 0;
        _currentStageIndex = 0;
        happyTotal = 0;
        _gridInitialized = false; // Re-initialize
        BeginGame(null);
    }

    void ShowRoundClear(int clearedRound)
    {
        _waitingForNextStage = true;
        if (_selectionManager != null && _selectionManager.selectionPanel != null)
            _selectionManager.selectionPanel.SetActive(false);
        _roundClearUI = CreateInfoUI($"Round {clearedRound} Clear!", "Next Round", OnNextRound);
    }

    void ShowStageClear(int clearedStage)
    {
        _waitingForNextStage = true;
        if (_selectionManager != null && _selectionManager.selectionPanel != null)
            _selectionManager.selectionPanel.SetActive(false);
        _stageClearUI = CreateInfoUI($"Stage {clearedStage} Clear!", "Move to Next Room", OnNextStage);
    }

    GameObject CreateInfoUI(string title, string buttonText, UnityEngine.Events.UnityAction action)
    {
        var canvasObj = new GameObject("InfoCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var infoScaler = canvasObj.AddComponent<CanvasScaler>();
        infoScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        infoScaler.referenceResolution = new Vector2(1920, 1080);
        infoScaler.matchWidthOrHeight = 0f;
        canvasObj.AddComponent<GraphicRaycaster>();
        if (canvasObj.GetComponent<CanvasFitInScreen>() == null)
            canvasObj.AddComponent<CanvasFitInScreen>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        panel.AddComponent<Image>().color = new Color(0.1f, 0.2f, 0.15f, 0.95f);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 300);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var tText = titleObj.AddComponent<Text>();
        tText.text = title;
        tText.fontSize = 30; // 40 -> 30
        tText.color = Color.white;
        tText.alignment = TextAnchor.MiddleCenter;
        tText.font = font;
        titleObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

        var btnObj = new GameObject("Button");
        btnObj.transform.SetParent(panel.transform, false);
        btnObj.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 1f);
        var btn = btnObj.AddComponent<Button>();
        var bTextObj = new GameObject("Text");
        bTextObj.transform.SetParent(btnObj.transform, false);
        var bText = bTextObj.AddComponent<Text>();
        bText.text = buttonText;
        bText.fontSize = 18; // 24 -> 18
        bText.color = Color.white;
        bText.alignment = TextAnchor.MiddleCenter;
        bText.font = font;
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
        btnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        btn.onClick.AddListener(action);

        return canvasObj;
    }

    void OnNextRound()
    {
        if (_roundClearUI != null) Destroy(_roundClearUI);
        _waitingForNextStage = false;
        _currentRoundIndex++;
        _turnsInCurrentRound = 0;
        FinishEndTurn();
    }

    void OnNextStage()
    {
        if (_stageClearUI != null) Destroy(_stageClearUI);
        _waitingForNextStage = false;
        _currentStageIndex++;
        LoadStage(_currentStageIndex);
        FinishEndTurn();
    }
}
