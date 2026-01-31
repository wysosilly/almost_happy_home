using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public partial class GridGameManager
{
    void EnsureStages()
    {
        if (stages.Count > 0) return;
        for (int i = 0; i < 10; i++)
        {
            stages.Add(new StageConfig
            {
                turnCheck = 5 + i * 2,
                requiredHappy = 10 + i * 10
            });
        }
    }

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
        canvas.pixelPerfect = false;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.25f, 0.05f, 0.05f, 0.98f);
        panelImg.raycastTarget = true;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.6f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(400, 60);
        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "Game Over";
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        if (font != null) titleText.font = font;

        var detailObj = new GameObject("Detail");
        detailObj.transform.SetParent(panel.transform, false);
        var detailRect = detailObj.AddComponent<RectTransform>();
        detailRect.anchorMin = detailRect.anchorMax = new Vector2(0.5f, 0.45f);
        detailRect.pivot = new Vector2(0.5f, 0.5f);
        detailRect.sizeDelta = new Vector2(500, 80);
        var detailText = detailObj.AddComponent<Text>();
        detailText.text = $"Required Happy: {requiredHappy}\nYour Happy: {happyTotal}";
        detailText.fontSize = 24;
        detailText.color = Color.white;
        detailText.alignment = TextAnchor.MiddleCenter;
        if (font != null) detailText.font = font;

        var btnObj = new GameObject("RetryButton");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0.25f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(160, 50);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.2f, 0.2f, 1f);
        var btn = btnObj.AddComponent<Button>();
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Retry";
        btnText.fontSize = 28;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        if (font != null) btnText.font = font;
        btn.onClick.AddListener(OnRetryGameOver);

        return canvasObj;
    }

    void OnRetryGameOver()
    {
        if (_gameOverUI != null)
        {
            Destroy(_gameOverUI);
            _gameOverUI = null;
        }
        _gameOver = false;
        _currentTurn = 0;
        _currentStageIndex = 0;
        happyTotal = 0;
        if (gameStartUI != null)
        {
            gameStartUI.ShowGameStart();
        }
        else if (_selectionManager != null)
        {
            _selectionManager.ShowSelectionAgain();
        }
    }

    void ShowStageClear(int clearedStage)
    {
        _waitingForNextStage = true;
        if (_selectionManager != null && _selectionManager.selectionPanel != null)
            _selectionManager.selectionPanel.SetActive(false);
        if (_stageClearUI != null) return;
        _stageClearUI = CreateStageClearUI(clearedStage);
        if (_stageClearUI != null) _stageClearUI.SetActive(true);
    }

    GameObject CreateStageClearUI(int clearedStage)
    {
        var canvasObj = new GameObject("StageClearCanvas");
        canvasObj.SetActive(true);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvas.pixelPerfect = false;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 280);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.3f, 0.15f, 0.95f);
        panelImg.raycastTarget = true;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(350, 50);
        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "Stage Clear!";
        titleText.fontSize = 40;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        if (font != null) titleText.font = font;

        var detailObj = new GameObject("Detail");
        detailObj.transform.SetParent(panel.transform, false);
        var detailRect = detailObj.AddComponent<RectTransform>();
        detailRect.anchorMin = detailRect.anchorMax = new Vector2(0.5f, 0.5f);
        detailRect.pivot = new Vector2(0.5f, 0.5f);
        detailRect.sizeDelta = new Vector2(350, 60);
        var detailText = detailObj.AddComponent<Text>();
        detailText.text = $"Stage {clearedStage} Complete!\nTotal Happy: {happyTotal}";
        detailText.fontSize = 22;
        detailText.color = Color.white;
        detailText.alignment = TextAnchor.MiddleCenter;
        if (font != null) detailText.font = font;

        var btnObj = new GameObject("NextStageButton");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0.25f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(180, 50);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.3f, 1f);
        var btn = btnObj.AddComponent<Button>();
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Next Stage";
        btnText.fontSize = 26;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        if (font != null) btnText.font = font;
        btn.onClick.AddListener(OnNextStage);

        return canvasObj;
    }

    void OnNextStage()
    {
        if (_stageClearUI != null)
        {
            Destroy(_stageClearUI);
            _stageClearUI = null;
        }
        _waitingForNextStage = false;
        _currentStageIndex++;
        actionPoints = 3;
        UpdateIndicators();
        if (_selectionManager == null) _selectionManager = FindObjectOfType<FurnitureSelectionManager>();
        if (_selectionManager != null) _selectionManager.ShowSelectionAgain();
    }
}
